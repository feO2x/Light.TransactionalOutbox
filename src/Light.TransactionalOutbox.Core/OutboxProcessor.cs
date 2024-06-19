using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace Light.TransactionalOutbox.Core;

public sealed class OutboxProcessor<TOutboxItem> : IOutboxProcessor, IDisposable
{
    public const string ResiliencePipelineKey = "OutboxProcessorResiliencePipeline";
    private readonly ILogger<OutboxProcessor<TOutboxItem>> _logger;
    private readonly OutboxProcessorOptions _options;
    private readonly Func<OutboxProcessor<TOutboxItem>, CancellationToken, ValueTask> _processAsyncDelegate;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly List<TOutboxItem> _successfullyProcessedOutboxItems;
    private Task? _currentTask;

    public OutboxProcessor(
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor<TOutboxItem>> logger
    )
    {
        _resiliencePipeline = resiliencePipelineProvider.MustNotBeNull().GetPipeline(ResiliencePipelineKey);
        _serviceScopeFactory = serviceScopeFactory.MustNotBeNull();
        _logger = logger.MustNotBeNull();
        _options = options.MustNotBeNull().Value;
        _processAsyncDelegate = (op, ct) => op.ProcessAsync(ct);
        _successfullyProcessedOutboxItems = new List<TOutboxItem>(options.Value.BatchSize);
    }

    public async ValueTask<bool> TryTriggerOutboxAsync(
        int timeoutInMilliseconds = 300,
        CancellationToken cancellationToken = default
    )
    {
        timeoutInMilliseconds.MustBeGreaterThanOrEqualTo(-1);

        // This method can be called from multiple threads concurrently. To make it thread-safe, we
        // use a semaphore with a double-check lock.
        if (_currentTask is not null)
        {
            return false;
        }

        // To not keep callers waiting indefinitely, we use a timeout to access the semaphore.
        // The default value is a maximum wait time of 300 milliseconds.
        if (!await _semaphore.WaitAsync(timeoutInMilliseconds, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        // If we end up here, we are in the critical section. We use a try-finally block to ensure that
        // the semaphore is released even if an exception occurs.
        try
        {
            // Here is the second part of the double-check lock.
            if (_currentTask is not null)
            {
                return false;
            }

            // If we end up here, we need to start processing of the outbox messages.
            _currentTask = _resiliencePipeline.ExecuteAsync(_processAsyncDelegate, this, cancellationToken).AsTask();
            HandleCurrentTask(_currentTask);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task WaitForOutboxCompletionAsync()
    {
        // We do not need to enter the semaphore here because we simply copy the current task to a local variable.
        // This is an atomic operations on x86, x64, and ARM processors.
        var currentTask = _currentTask;
        return currentTask ?? Task.CompletedTask;
    }

    private async void HandleCurrentTask(Task task)
    {
        // This method is async void because we do not want callers of TryTriggerOutboxAsync to wait until the whole
        // outbox processing is done. The calling method cannot track the task associated with this async method and
        // thus returns early. To accomodate this, the await task.ConfigureAwait(false) call is wrapped in a try-catch
        // block so that we do not lose any exceptions that might occur during the processing.
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogMessages.LogErrorDuringOutboxProcessing(_logger, exception);
        }

        // We will not use any cancellation token or timeout here because we absolutely must enter a critical section.
        // Otherwise, other services won't be able to trigger the outbox processing again in the future.
        await _semaphore.WaitAsync();
        _currentTask = null;
        _semaphore.Release();
    }

    private async ValueTask ProcessAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var session = scope.ServiceProvider.GetRequiredService<IOutboxProcessorSession<TOutboxItem>>();
            var outboxItems = await session.LoadNextOutboxItemsAsync(_options.BatchSize, cancellationToken);
            if (outboxItems.Count == 0)
            {
                return;
            }

            await SendOutboxItemsAsync(scope, session, outboxItems, cancellationToken);
        }
    }

    private Task SendOutboxItemsAsync(
        AsyncServiceScope scope,
        IOutboxProcessorSession<TOutboxItem> session,
        List<TOutboxItem> outboxItems,
        CancellationToken cancellationToken
    )
    {
        var batchPublisher = scope.ServiceProvider.GetService<IOutboxItemBatchPublisher<TOutboxItem>>();
        return batchPublisher is not null ?
            SendOutboxItemBatchAsync(batchPublisher, session, outboxItems, cancellationToken) :
            SendEachOutboxItemAsync(scope, session, outboxItems, cancellationToken);
    }

    private static async Task SendOutboxItemBatchAsync(
        IOutboxItemBatchPublisher<TOutboxItem> batchPublisher,
        IOutboxProcessorSession<TOutboxItem> session,
        List<TOutboxItem> outboxItems,
        CancellationToken cancellationToken
    )
    {
        await batchPublisher.PublishOutboxItemsAsync(outboxItems, cancellationToken);
        await session.RemoveOutboxItemsAsync(outboxItems, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task SendEachOutboxItemAsync(
        AsyncServiceScope scope,
        IOutboxProcessorSession<TOutboxItem> session,
        List<TOutboxItem> outboxItems,
        CancellationToken cancellationToken
    )
    {
        _successfullyProcessedOutboxItems.Clear();

        var outboxItemPublisher = scope.ServiceProvider.GetRequiredService<IOutboxItemPublisher<TOutboxItem>>();

        try
        {
            foreach (var outboxItem in outboxItems)
            {
                await outboxItemPublisher.PublishOutboxItemAsync(outboxItem, cancellationToken);
                _successfullyProcessedOutboxItems.Add(outboxItem);
                LogMessages.LogSuccessfullyPublishedOutboxItem(_logger, outboxItem);
            }
        }
        finally
        {
            // No matter if all or only a part of the outbox items have been published,
            // we try to remove the successfully published ones
            // from the database to avoid sending them again in the future.
            if (_successfullyProcessedOutboxItems.Count > 0)
            {
                await session.RemoveOutboxItemsAsync(_successfullyProcessedOutboxItems, cancellationToken);
                await session.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static class LogMessages
    {
        // ReSharper disable StaticMemberInGenericType -- outbox processor will likely be resolved only once per app
        private static readonly Action<ILogger, Exception> ErrorDuringOutboxProcessing = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(70001, nameof(ErrorDuringOutboxProcessing)),
            "An error occurred during outbox processing"
        );

        public static void LogErrorDuringOutboxProcessing(ILogger logger, Exception exception) =>
            ErrorDuringOutboxProcessing(logger, exception);

        private static readonly Action<ILogger, TOutboxItem, Exception?> OutboxItemPublished =
            LoggerMessage.Define<TOutboxItem>(
                LogLevel.Debug,
                new EventId(70002, nameof(OutboxItemPublished)),
                "Successfully published message {@OutboxItem}"
            );

        public static void LogSuccessfullyPublishedOutboxItem(ILogger logger, TOutboxItem outboxItem) =>
            OutboxItemPublished(logger, outboxItem, null);
        // ReSharper restore StaticMemberInGenericType
    }

    public void Dispose() => _semaphore.Dispose();
}