using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Light.TransactionalOutbox.Core;
using Light.TransactionalOutbox.Core.OutboxProcessing;
using Microsoft.EntityFrameworkCore;

namespace Light.TransactionalOutbox.EntityFrameworkCore;

public sealed class EfOutboxProcessorSession<TDbContext, TOutboxItem> : EfAsyncSession<TDbContext>.WithTransaction,
                                                                        IOutboxProcessorSession<TOutboxItem>
    where TDbContext : DbContext, IHasOutboxItems<TOutboxItem>
    where TOutboxItem : class, IHasCreatedAtUtc
{
    private readonly ILoadOutboxItemsStrategy<TDbContext, TOutboxItem> _loadStrategy;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EfOutboxProcessorSession(
        TDbContext dbContext,
        ILoadOutboxItemsStrategy<TDbContext, TOutboxItem> loadStrategy
    ) : base(dbContext) =>
        _loadStrategy = loadStrategy;

    public async Task<List<TOutboxItem>> LoadNextOutboxItemsAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await _loadStrategy.LoadNextOutboxItemsAsync(dbContext, batchSize, cancellationToken);
    }

    public async Task RemoveOutboxItemsAsync(
        List<TOutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    )
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        dbContext.OutboxItems.RemoveRange(successfullyProcessedOutboxItems);
    }
}