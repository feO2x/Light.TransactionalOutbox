using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Light.GuardClauses;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

public sealed class MessageTypes
{
#if NET8_0_OR_GREATER
    private const string RequiresUnreferencedCodeMessage =
        "Scans assembly for type decorated with MessageTypeAttribute. These might be removed by AOT trimmer.";
#endif
    private readonly Dictionary<string, Type> _messageTypeToDotnetTypeMapping;
    private readonly Dictionary<Type, string> _dotnetTypeToMessageTypeMapping;

    public MessageTypes(MessageTypesMappings mappings) : this(
        mappings.MessageTypeToDotnetTypeMapping,
        mappings.DotnetTypeToMessageTypeMapping
    ) { }

    public MessageTypes(
        Dictionary<string, Type> messageTypeToDotnetTypeMapping,
        Dictionary<Type, string> dotnetTypeToMessageTypeMapping
    )
    {
        _messageTypeToDotnetTypeMapping = messageTypeToDotnetTypeMapping;
        _dotnetTypeToMessageTypeMapping = dotnetTypeToMessageTypeMapping;
    }

    public bool TryGetDotnetType(string messageType, [NotNullWhen(true)] out Type? dotnetType) =>
        _messageTypeToDotnetTypeMapping.TryGetValue(messageType, out dotnetType);
    
    public bool TryGetMessageType(Type dotnetType, [NotNullWhen(true)] out string? messageType) =>
        _dotnetTypeToMessageTypeMapping.TryGetValue(dotnetType, out messageType);

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
#endif
    public static MessageTypes CreateDefault(params Assembly[] assemblies)
    {
        assemblies = ApplyCallingAssemblyIfNecessary(assemblies, Assembly.GetCallingAssembly());
        var mappings = CreateMessageTypesMappingsInternal(assemblies);
        return new MessageTypes(mappings);
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
#endif
    public static MessageTypesMappings CreateMessageTypesMapping(params Assembly[] assemblies)
    {
        assemblies = ApplyCallingAssemblyIfNecessary(assemblies, Assembly.GetCallingAssembly());
        return CreateMessageTypesMappingsInternal(assemblies);
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
#endif
    private static MessageTypesMappings CreateMessageTypesMappingsInternal(Assembly[] assemblies)
    {
        var messageTypeToDotnetTypeMapping = new Dictionary<string, Type>();
        var dotnetTypeToMessageTypeMapping = new Dictionary<Type, string>();
        
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.ExportedTypes)
            {
                var messageTypeAttribute = type.GetCustomAttribute<MessageTypeAttribute>();
                if (messageTypeAttribute is null)
                {
                    continue;
                }
                
                dotnetTypeToMessageTypeMapping.Add(type, messageTypeAttribute.PrimaryName);

                foreach (var name in messageTypeAttribute.Names)
                {
                    messageTypeToDotnetTypeMapping.Add(name, type);
                }
            }
        }

        return new MessageTypesMappings(messageTypeToDotnetTypeMapping, dotnetTypeToMessageTypeMapping);
    }

    private static Assembly[] ApplyCallingAssemblyIfNecessary(Assembly[] assemblies, Assembly callingAssembly) =>
        assemblies.MustNotBeNull().Length == 0 ? [callingAssembly] : assemblies;
}