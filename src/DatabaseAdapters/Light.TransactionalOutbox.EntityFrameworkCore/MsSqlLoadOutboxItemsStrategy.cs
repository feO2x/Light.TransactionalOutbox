using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.TransactionalOutbox.Core;
using Microsoft.EntityFrameworkCore;

namespace Light.TransactionalOutbox.EntityFrameworkCore;

public sealed class MsSqlLoadOutboxItemsStrategy<TDbContext, TOutboxItem>
    : ILoadOutboxItemsStrategy<TDbContext, TOutboxItem>
    where TDbContext : DbContext, IHasOutboxItems<TOutboxItem>
    where TOutboxItem : class, IHasCreatedAtUtc
{
    private LoadNextOutboxItemsInfo? _info;

    public Task<List<TOutboxItem>> LoadNextOutboxItemsAsync(
        TDbContext dbContext,
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        batchSize.MustBeGreaterThan(0);
        var info = GetOrCreateInfo(dbContext);
        return dbContext
           .OutboxItems
           .FromSql(
                $"""
                 SELECT TOP {batchSize} *
                 FROM {info.SchemaQualifiedTableName} WITH (UPDLOCK, READPAST)
                 ORDER BY {info.CreatedAtUtcColumnName};
                 """
            )
           .ToListAsync(cancellationToken);
    }

    private LoadNextOutboxItemsInfo GetOrCreateInfo(TDbContext dbContext)
    {
        var info = _info;
        if (info is not null)
        {
            return info;
        }

        lock (this)
        {
            info = _info;
            if (info is not null)
            {
                return info;
            }

            info = _info = dbContext.GetLoadNextOutboxItemsInfo<TDbContext, TOutboxItem>();
        }

        return info;
    }
}