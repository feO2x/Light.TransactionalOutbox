using System.Threading.Tasks;

namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public interface IAwaitOutboxCompletion
{
    Task WaitForOutboxCompletionAsync();
}