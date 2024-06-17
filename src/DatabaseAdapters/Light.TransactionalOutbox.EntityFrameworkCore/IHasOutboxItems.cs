using Microsoft.EntityFrameworkCore;

namespace Light.TransactionalOutbox.EntityFrameworkCore;

public interface IHasOutboxItems<TOutboxItem>
    where TOutboxItem : class
{
    DbSet<TOutboxItem> OutboxItems { get; }
}