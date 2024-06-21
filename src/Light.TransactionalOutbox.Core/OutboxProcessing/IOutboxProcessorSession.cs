using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public interface IOutboxProcessorSession<TOutboxItem> : IAsyncSession
{
    Task<List<TOutboxItem>> LoadNextOutboxItemsAsync(int batchSize, CancellationToken cancellationToken = default);

    Task RemoveOutboxItemsAsync(
        List<TOutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    );
}