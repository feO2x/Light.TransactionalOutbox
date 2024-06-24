using System;
using System.Text.Json.Serialization;
using Light.TransactionalOutbox.Core;
using Light.TransactionalOutbox.Core.MessageSerialization;

// ReSharper disable NotAccessedPositionalProperty.Global -- just for testing purposes

namespace Light.TransactionalOutbox.SharedTesting;

[MessageType("MyMessage")]
public sealed record MyMessage(Guid Id, string Content);

[MessageType("MySecondMessage")]
public sealed record MySecondMessage(Guid CorrelationId, string Content) : IHasCorrelationId;

[JsonSerializable(typeof(MyMessage))]
[JsonSerializable(typeof(MySecondMessage))]
public sealed partial class MessagesJsonSerializerContext : JsonSerializerContext;