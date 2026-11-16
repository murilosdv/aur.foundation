using System;

namespace Aurore.Foundation.Core.Entities;

/// <summary>
/// <see cref="Entity"/> that is marked as deleted instead of removed.
/// </summary>
public abstract class SoftDeletableEntity : Entity, ISoftDeletable
{
    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }
}
