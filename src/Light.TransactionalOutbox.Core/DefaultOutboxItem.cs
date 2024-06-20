using System;
using Light.SharedCore.Entities;

namespace Light.TransactionalOutbox.Core;

public class DefaultOutboxItem : Int64Entity<DefaultOutboxItem>
{
    public string Type { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string JsonPayload { get; set; } = string.Empty;
}