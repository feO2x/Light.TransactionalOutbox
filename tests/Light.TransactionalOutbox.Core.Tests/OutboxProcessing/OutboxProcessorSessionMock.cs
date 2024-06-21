using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Light.DataAccessMocks;
using Light.TransactionalOutbox.Core.OutboxProcessing;

namespace Light.TransactionalOutbox.Core.Tests.OutboxProcessing;

public sealed class OutboxProcessorSessionMock : AsyncSessionMock, IOutboxProcessorSession<DefaultOutboxItem>
{
    private readonly List<DefaultOutboxItem> _outboxItems;

    public OutboxProcessorSessionMock(List<DefaultOutboxItem> outboxItems, OutboxFailure failure)
    {
        _outboxItems = outboxItems;
        Failure = failure;
    }

    public OutboxFailure Failure { get; }

    public Task<List<DefaultOutboxItem>> LoadNextOutboxItemsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtLoadNextOutboxItems))
        {
            throw new IOException("LoadNextOutboxItems failed");
        }

        return Task.FromResult(_outboxItems.Take(batchSize).ToList());
    }

    public Task RemoveOutboxItemsAsync(
        List<DefaultOutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    )
    {
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtRemoveOutboxItems))
        {
            throw new IOException("RemoveOutboxItems failed");
        }

        foreach (var outboxItem in successfullyProcessedOutboxItems)
        {
            _outboxItems.Remove(outboxItem);
        }

        return Task.CompletedTask;
    }

    public new Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtSaveChanges))
        {
            throw new IOException("SaveChanges failed");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}