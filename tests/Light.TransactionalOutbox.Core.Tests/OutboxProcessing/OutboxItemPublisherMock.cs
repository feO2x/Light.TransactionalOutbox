using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.TransactionalOutbox.Core.OutboxProcessing;

namespace Light.TransactionalOutbox.Core.Tests.OutboxProcessing;

public sealed class OutboxItemPublisherMock : IOutboxItemPublisher<DefaultOutboxItem>
{
    private readonly OutboxFailureContext _failureContext;
    private readonly List<DefaultOutboxItem> _receivedOutboxItems = new ();

    public OutboxItemPublisherMock(OutboxFailureContext failureContext) => _failureContext = failureContext;

    public Task PublishOutboxItemAsync(DefaultOutboxItem outboxItem, CancellationToken cancellationToken = default)
    {
        var currentFailure = _failureContext.CurrentFailure;
        if (currentFailure.HasFlagValue(OutboxFailure.ErrorAtPublishOutboxItem))
        {
            throw new IOException("PublishOutboxItem failed");
        }

        _receivedOutboxItems.Add(outboxItem);
        return Task.CompletedTask;
    }

    public OutboxItemPublisherMock ShouldHaveReceivedAtLeastOnce(List<DefaultOutboxItem> outboxItems)
    {
        outboxItems.Should().Equal(_receivedOutboxItems.Distinct());
        return this;
    }
}