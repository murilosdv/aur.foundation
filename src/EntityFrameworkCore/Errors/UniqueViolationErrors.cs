using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.EntityFrameworkCore.Errors;

internal static class UniqueViolationErrors
{
    internal static ApplicationError Describe(DbUpdateException exception, string? constraintName)
    {
        if (constraintName is null)
            return DatabaseErrors.Conflict;

        var fields = new HashSet<string>();
        foreach (var entityType in exception.Entries.Select(entry => entry.Metadata))
        {
            var indexed = entityType.GetIndexes().Where(index => index.GetDatabaseName() == constraintName).SelectMany(index => index.Properties);
            var keyed = entityType.GetKeys().Where(key => key.GetName() == constraintName).SelectMany(key => key.Properties);
            foreach (var property in indexed.Concat(keyed))
                fields.Add(property.Name.ToCamelCase());
        }

        return fields.Count is 0
            ? DatabaseErrors.Conflict
            : DatabaseErrors.Conflict.WithDetails(fields.ToDictionary(field => field, _ => new[] { "A record with this value already exists." }));
    }
}
