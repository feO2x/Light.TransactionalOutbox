using System;
using System.Text.Json;
using Light.GuardClauses;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

public sealed class MessageToDefaultOutboxItemConverter : IMessageToOutboxItemConverter<DefaultOutboxItem>
{
    private readonly MessageTypes _messageTypes;
    private readonly JsonSerializerOptions _options;
    private readonly TimeProvider _timeProvider;

    public MessageToDefaultOutboxItemConverter(
        MessageTypes messageTypes,
        TimeProvider timeProvider,
        JsonSerializerOptions options
    )
    {
        _messageTypes = messageTypes;
        _timeProvider = timeProvider;
        _options = options;
    }

    public DefaultOutboxItem Convert(object message)
    {
        message.MustNotBeNull();
        var dotnetType = message.GetType();
        if (!_messageTypes.TryGetMessageType(dotnetType, out var messageType))
        {
            throw new InvalidOperationException(
                $"No message type found for type \"{dotnetType}\". Please ensure to register all message types with the {nameof(MessageTypes)} class."
            );
        }

        var correlationId = message is IHasCorrelationId messageWithCorrelationId ?
            messageWithCorrelationId.CorrelationId :
            Guid.NewGuid();

        return new DefaultOutboxItem
        {
            MessageType = messageType,
            CorrelationId = correlationId,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            MessageAsJson = JsonSerializer.Serialize(message, dotnetType, _options)
        };
    }
}