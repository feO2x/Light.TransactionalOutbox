using System;

namespace Light.TransactionalOutbox.Core;

public interface IHasCreatedAtUtc
{
    DateTime CreatedAtUtc { get; }
}