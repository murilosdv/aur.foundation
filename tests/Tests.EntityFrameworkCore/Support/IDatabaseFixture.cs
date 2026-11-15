using System;
using Aurore.Foundation.Core.Features.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Support;

/// <summary>
/// A database of its own on a real server, for the tests that every provider has to pass.
/// </summary>
public interface IDatabaseFixture
{
    /// <summary>
    /// A container with the DbContext of the foundation (interceptors included) on this database, a time that tests can
    /// move, and the sample features. Dispose it when done.
    /// </summary>
    public ServiceProvider Provider(Action<FeaturesBuilder>? features = null, Action<IServiceCollection>? register = null, FakeTimeProvider? time = null);

    /// <summary>
    /// Whether the exception is the unique violation of the provider.
    /// </summary>
    public bool IsUniqueViolation(Exception exception);
}
