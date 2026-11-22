using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// One allowed field of a SortMap: how to order by it, and what a cursor needs from it (read its value from an item, and
/// compare against a value that came back in a cursor).
/// </summary>
internal interface ISortField<T>
{
    /// <summary>
    /// The type of the property.
    /// </summary>
    public Type KeyType { get; }

    /// <summary>
    /// Orders the query by the field. The first ordering uses OrderBy, the following ones ThenBy.
    /// </summary>
    public IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction, bool first);

    /// <summary>
    /// Reads the value of the field from an item, ready to go into a cursor.
    /// </summary>
    public JsonElement Read(T item);

    /// <summary>
    /// The field's expression, written for the given parameter so several fields can share one lambda.
    /// </summary>
    public Expression Member(ParameterExpression parameter);

    /// <summary>
    /// The value that came back in a cursor, as an expression of the field's type. Throws JsonException when it does not fit.
    /// </summary>
    public Expression Bound(JsonElement value);
}
