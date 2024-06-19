using System;
// ReSharper disable NotAccessedPositionalProperty.Global -- just for testing purposes

namespace Light.TransactionalOutbox.Core.Tests;

public sealed record MyMessage(Guid Id, string Content);