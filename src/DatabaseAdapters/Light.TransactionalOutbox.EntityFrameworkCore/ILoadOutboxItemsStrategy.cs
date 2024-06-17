using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Light.TransactionalOutbox.EntityFrameworkCore;

public interface ILoadOutboxItemsStrategy<in TDbContext, TOutboxItem>
    where TDbContext : DbContext, IHasOutboxItems<TOutboxItem>
    where TOutboxItem : class
{
    Task<List<TOutboxItem>> LoadNextOutboxItemsAsync(
        TDbContext dbContext,
        int batchSize,
        CancellationToken cancellationToken = default
    );
}