using System.Diagnostics.CodeAnalysis;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

public interface IMessageToOutboxItemConverter<out TOutboxItem>
{
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(MessageToOutboxItemConverterCompilerMessage.NativeAotMessage)]
    [RequiresDynamicCode(MessageToOutboxItemConverterCompilerMessage.NativeAotMessage)]
#endif
    TOutboxItem Convert(object message);
}

internal static class MessageToOutboxItemConverterCompilerMessage
{
    public const string NativeAotMessage =
        "Implementations of this interface might use serializers that use unbound reflection which would lead to errors in Native AOT scenarios. Please ensure that these serializers are properly configrured in this case, for example System.Text.Json with Source Generators. If you do not use Native AOT, you can safely ignore this message.";
}