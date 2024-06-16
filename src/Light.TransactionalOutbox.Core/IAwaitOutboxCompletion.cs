using System.Threading.Tasks;

namespace Light.TransactionalOutbox.Core;

public interface IAwaitOutboxCompletion
{
    Task WaitForOutboxCompletionAsync();
}