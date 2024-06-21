using System;

namespace Light.TransactionalOutbox.Core;

public interface IHasCorrelationId
{
    Guid CorrelationId { get; }
}