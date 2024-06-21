namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public interface IOutboxProcessor : IOutboxTrigger, IAwaitOutboxCompletion;