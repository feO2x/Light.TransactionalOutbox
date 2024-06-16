using System.Threading;
using System.Threading.Tasks;

namespace Light.TransactionalOutbox.Core;

public interface IOutboxTrigger
{
    ValueTask<bool> TryTriggerOutboxAsync(
        int timeoutInMilliseconds = 300,
        CancellationToken cancellationToken = default
    );
}