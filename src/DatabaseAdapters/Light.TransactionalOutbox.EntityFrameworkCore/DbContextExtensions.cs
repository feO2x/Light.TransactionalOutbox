using System;
using Light.GuardClauses;
using Light.TransactionalOutbox.Core;
using Microsoft.EntityFrameworkCore;

namespace Light.TransactionalOutbox.EntityFrameworkCore;

public static class DbContextExtensions
{
    public static LoadNextOutboxItemsInfo GetLoadNextOutboxItemsInfo<TDbContext, TOutboxItem>(this TDbContext dbContext)
        where TDbContext : DbContext, IHasOutboxItems<TOutboxItem>
        where TOutboxItem : class, IHasCreatedAtUtc
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TOutboxItem));
        if (entityType is null)
        {
            throw new InvalidOperationException(
                $"Could not obtain entity type for {typeof(TOutboxItem)}. Please ensure that you have configured this type with your db context."
            );
        }

        var schemaQualifiedTableName = entityType.GetSchemaQualifiedTableName();
        if (schemaQualifiedTableName.IsNullOrWhiteSpace())
        {
            throw new InvalidOperationException(
                $"Could not obtain table name for {typeof(TOutboxItem)}. Please ensure that you have configured this type with your db context."
            );
        }
        
        var createdAtUtcProperty = entityType.FindProperty(nameof(IHasCreatedAtUtc.CreatedAtUtc));
        if (createdAtUtcProperty is null)
        {
            throw new InvalidOperationException(
                $"Could not obtain property {nameof(IHasCreatedAtUtc.CreatedAtUtc)} for {typeof(TOutboxItem)}. Please ensure that you have configured this type with your db context."
            );
        }

        var createdAtUtColumnName = createdAtUtcProperty.GetColumnName();
        if (createdAtUtColumnName.IsNullOrWhiteSpace())
        {
            throw new InvalidOperationException(
                $"Could not obtain column name for {typeof(TOutboxItem)}. Please ensure that you have configured this type with your db context."
            );
        }
        

        return new LoadNextOutboxItemsInfo(schemaQualifiedTableName, createdAtUtColumnName);
    }
}