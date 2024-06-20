using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Light.TransactionalOutbox.Core.Tests;

public sealed class OutboxProcessorTests : IAsyncLifetime
{
    public static readonly TheoryData<List<OutboxFailure>> OutboxFailures =
        new ()
        {
            new List<OutboxFailure> { OutboxFailure.ErrorAtLoadNextOutboxItems },
            new List<OutboxFailure> { OutboxFailure.ErrorAtPublishOutboxItem },
            new List<OutboxFailure>
            {
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.None,
                OutboxFailure.ErrorAtLoadNextOutboxItems
            },
            new List<OutboxFailure>
            {
                OutboxFailure.None,
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.None,
                OutboxFailure.ErrorAtPublishOutboxItem,
                OutboxFailure.None,
                OutboxFailure.ErrorAtRemoveOutboxItems,
                OutboxFailure.ErrorAtSaveChanges,
                OutboxFailure.None,
                OutboxFailure.ErrorAtPublishOutboxItem | OutboxFailure.ErrorAtRemoveOutboxItems,
                OutboxFailure.ErrorAtPublishOutboxItem | OutboxFailure.ErrorAtSaveChanges
            }
        };

    private readonly OutboxFailureContext _failureContext;
    private readonly OutboxItemPublisherMock _outboxItemPublisher;
    private readonly OutboxProcessor<DefaultOutboxItem> _outboxProcessor;
    private readonly ServiceProvider _serviceProvider;
    private readonly OutboxProcessorSessionMockFactory _sessionFactory;

    public OutboxProcessorTests(ITestOutputHelper testOutputHelper)
    {
        var logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .WriteTo.TestOutput(testOutputHelper)
           .CreateLogger();
        var configuration = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string?> { ["TransactionalOutbox:BatchSize"] = "3" })
           .Build();

        _failureContext = new OutboxFailureContext();
        _sessionFactory = new OutboxProcessorSessionMockFactory(_failureContext);
        _outboxItemPublisher = new OutboxItemPublisherMock(_failureContext);

        _serviceProvider = new ServiceCollection()
           .AddLogging(builder => builder.AddSerilog(logger))
           .AddSingleton<IConfiguration>(configuration)
           .AddSingleton(TimeProvider.System)
           .AddSingleton<OutboxFailureContext>()
           .AddOutboxProcessor<DefaultOutboxItem>(registerDefaultResiliencePipeline: false)
           .AddResiliencePipeline(
                OutboxConstants.ResiliencePipelineKey,
                pipelineBuilder => pipelineBuilder.AddRetry(
                    new RetryStrategyOptions
                    {
                        MaxRetryAttempts = int.MaxValue,
                        Delay = TimeSpan.Zero,
                        BackoffType = DelayBackoffType.Constant
                    }
                )
            )
           .AddScoped<IOutboxProcessorSession<DefaultOutboxItem>>(_ => _sessionFactory.Create())
           .AddSingleton<IOutboxItemPublisher<DefaultOutboxItem>>(_outboxItemPublisher)
           .BuildServiceProvider();
        _outboxProcessor = _serviceProvider.GetRequiredService<OutboxProcessor<DefaultOutboxItem>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _serviceProvider.DisposeAsync().AsTask();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task OutboxProcessorShouldWorkCorrectlyWhenNoErrorsOccur(int amountOfItems)
    {
        var outboxItems = CreateOutboxItems(amountOfItems);

        await RunOutboxProcessor();

        _sessionFactory
           .OutboxItemsShouldBeEmpty()
           .AllSessionsShouldBeCommittedAndDisposed();
        _outboxItemPublisher.ShouldHaveReceivedAtLeastOnce(outboxItems);
    }

    [Theory]
    [MemberData(nameof(OutboxFailures))]
    public async Task OutboxProcessorShouldRetryWhenErrorsOccur(List<OutboxFailure> failures)
    {
        var outboxItems = CreateOutboxItems(10);
        _failureContext.Failures.AddRange(failures);

        await RunOutboxProcessor();

        _sessionFactory
           .OutboxItemsShouldBeEmpty()
           .AllSuccessfulSessionsShouldBeCommitted()
           .AllSessionsShouldBeDisposed();
        _outboxItemPublisher.ShouldHaveReceivedAtLeastOnce(outboxItems);
    }

    private List<DefaultOutboxItem> CreateOutboxItems(int amount)
    {
        var timeProvider = _serviceProvider.GetRequiredService<TimeProvider>();
        var outboxItems = new List<DefaultOutboxItem>(amount);
        for (var i = 0; i < amount; i++)
        {
            var item = new DefaultOutboxItem
            {
                Id = i + 1,
                Type = nameof(MyMessage),
                JsonPayload = JsonSerializer.Serialize(new MyMessage(Guid.NewGuid(), $"Message Content {i + 1}")),
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
                CorrelationId = Guid.NewGuid()
            };
            outboxItems.Add(item);
        }

        _sessionFactory.OutboxItems.AddRange(outboxItems);

        return outboxItems;
    }

    private async Task RunOutboxProcessor()
    {
        await _outboxProcessor.TryTriggerOutboxAsync();
        await _outboxProcessor.WaitForOutboxCompletionAsync();
    }
}