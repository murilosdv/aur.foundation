using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// The field of a SortMap that is read from the property <typeparamref name="TKey"/> of <typeparamref name="T"/>.
/// </summary>
internal sealed class SortField<T, TKey>(Expression<Func<T, TKey>> key) : ISortField<T>
{
    private readonly Lazy<Func<T, TKey>> _read = new(key.Compile);

    public Type KeyType => typeof(TKey);

    public IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction, bool first)
    {
        return (first, direction) switch
        {
            (true, SortDirection.Asc) => query.OrderBy(key),
            (true, SortDirection.Desc) => query.OrderByDescending(key),
            (false, SortDirection.Asc) => ((IOrderedQueryable<T>)query).ThenBy(key),
            _ => ((IOrderedQueryable<T>)query).ThenByDescending(key),
        };
    }

    public JsonElement Read(T item)
    {
        return JsonSerializer.SerializeToElement(_read.Value(item));
    }

    public Expression Member(ParameterExpression parameter)
    {
        return new Replace(key.Parameters[0], parameter).Visit(key.Body);
    }

    public Expression Bound(JsonElement value)
    {
        var bound = value.Deserialize<TKey>() ?? throw new JsonException("A cursor value cannot be null.");

        // Read from a holder, not inlined as a constant, so the provider can parameterize the query.
        return Expression.Property(Expression.Constant(new Holder(bound)), nameof(Holder.Value));
    }

    private sealed record Holder(TKey Value);

    private sealed class Replace(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }
}
