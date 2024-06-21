using System.Threading;
using System.Threading.Tasks;

namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public interface IOutboxItemPublisher<in TOutboxItem>
{
    Task PublishOutboxItemAsync(TOutboxItem outboxItem, CancellationToken cancellationToken = default);
}