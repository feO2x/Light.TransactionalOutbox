using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public interface IOutboxItemBatchPublisher<TOutboxItem>
{
    Task PublishOutboxItemsAsync(List<TOutboxItem> outboxItems, CancellationToken cancellationToken = default);
}