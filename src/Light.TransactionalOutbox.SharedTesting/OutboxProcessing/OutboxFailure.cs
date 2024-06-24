using System;

namespace Light.TransactionalOutbox.SharedTesting.OutboxProcessing;

[Flags]
public enum OutboxFailure
{
    None,
    ErrorAtLoadNextOutboxItems,
    ErrorAtRemoveOutboxItems,
    ErrorAtSaveChanges,
    ErrorAtPublishOutboxItem
}

public static class OutboxFailureExtensions
{
    public static bool HasFlagValue(this OutboxFailure sourceValue, OutboxFailure singleValue)
    {
        return (sourceValue & singleValue) == singleValue;
    }
}