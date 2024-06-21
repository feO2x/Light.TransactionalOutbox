using System;
using Light.SharedCore.Entities;

namespace Light.TransactionalOutbox.Core;

public class DefaultOutboxItem : Int64Entity<DefaultOutboxItem>, IHasCreatedAtUtc
{
    public required string MessageType { get; init; }
    public required Guid CorrelationId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required string MessageAsJson { get; init; }
}