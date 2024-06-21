namespace Light.TransactionalOutbox.Core.MessageSerialization;

public interface IMessageToOutboxItemConverter<out TOutboxItem>
{
    TOutboxItem Convert(object message);
}