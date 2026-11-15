using System;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Support;

/// <summary>
/// What the model of the test entities needs from the provider under test. Each provider project sets it once.
/// </summary>
public static class TestModel
{
    /// <summary>
    /// Makes the index keep only the rows that are not deleted, with the filter syntax of the provider.
    /// </summary>
    public static Func<IndexBuilder<Person>, IndexBuilder<Person>> NotDeleted { get; set; } = index => index;
}
