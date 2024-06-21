using System;
using System.Collections.Generic;

namespace Light.TransactionalOutbox.Core.MessageSerialization;

public readonly record struct MessageTypesMappings(
    Dictionary<string, Type> MessageTypeToDotnetTypeMapping,
    Dictionary<Type, string> DotnetTypeToMessageTypeMapping
);