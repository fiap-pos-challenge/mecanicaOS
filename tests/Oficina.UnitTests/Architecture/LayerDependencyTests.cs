using System.Reflection;
using Oficina.Domain.Common.Entities;

namespace Oficina.UnitTests.Architecture;

public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = Oficina.Application.AssemblyReference.Assembly;

    [Fact]
    public void Domain_ReferencesOnlyTheBaseClassLibrary()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty);

        Assert.All(references, name => Assert.True(
            name.StartsWith("System", StringComparison.Ordinal) || name == "netstandard",
            $"Domain must not depend on '{name}'."));
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies().Select(reference => reference.Name).ToList();

        Assert.DoesNotContain("Oficina.Infrastructure", references);
        Assert.DoesNotContain("Oficina.Api", references);
    }
}
