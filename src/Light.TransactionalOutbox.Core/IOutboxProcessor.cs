namespace Light.TransactionalOutbox.Core;

public interface IOutboxProcessor : IOutboxTrigger, IAwaitOutboxCompletion;