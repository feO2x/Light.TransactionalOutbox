﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Light.DataAccessMocks;

namespace Light.TransactionalOutbox.Core.Tests;

public sealed class OutboxProcessorSessionMock : AsyncSessionMock, IOutboxProcessorSession<OutboxItem>
{
    private readonly List<OutboxItem> _outboxItems;

    public OutboxProcessorSessionMock(List<OutboxItem> outboxItems, OutboxFailure failure)
    {
        _outboxItems = outboxItems;
        Failure = failure;
    }

    public OutboxFailure Failure { get; }

    public Task<List<OutboxItem>> LoadNextOutboxItemsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (Failure == OutboxFailure.ErrorAtLoadNextOutboxItems)
        {
            throw new IOException("LoadNextOutboxItems failed");
        }

        return Task.FromResult(_outboxItems.Take(batchSize).ToList());
    }

    public Task RemoveOutboxItemsAsync(
        List<OutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    )
    {
        if (Failure == OutboxFailure.ErrorAtRemoveOutboxItems)
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
        if (Failure == OutboxFailure.ErrorAtSaveChanges)
        {
            throw new IOException("SaveChanges failed");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}