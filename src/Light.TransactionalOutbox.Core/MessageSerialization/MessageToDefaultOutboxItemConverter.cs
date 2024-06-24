using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Light.GuardClauses;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

public sealed class MessageToDefaultOutboxItemConverter : IMessageToOutboxItemConverter<DefaultOutboxItem>
{
    private const string NativeAotMessage =
        "Please make sure that the JsonSerializerOptions passed to this converter incorporate System.Text.Json source generators in AOT scenarios. See https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation for more information.";
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

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(NativeAotMessage)]
    [RequiresDynamicCode(NativeAotMessage)]
#endif
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
            SerializedMessage = JsonSerializer.Serialize(message, dotnetType, _options)
        };
    }
}