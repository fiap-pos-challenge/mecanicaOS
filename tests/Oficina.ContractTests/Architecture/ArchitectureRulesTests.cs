using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Controllers;
using Oficina.Domain.Common.Entities;
using Oficina.Infrastructure.Persistence;

namespace Oficina.ContractTests.Architecture;

public sealed class ArchitectureRulesTests
{
    private const string CatalogNamespace = "Oficina.Domain.Catalog";

    private static readonly Assembly[] Layers =
    [
        typeof(Entity).Assembly,
        Oficina.Application.AssemblyReference.Assembly,
        typeof(OficinaDbContext).Assembly,
        typeof(ApiControllerBase).Assembly,
    ];

    private static IEnumerable<Type> OwnTypes() =>
        Layers.SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsNested && type.Namespace?.StartsWith("Oficina.", StringComparison.Ordinal) == true && !type.Name.StartsWith('<'));

    [Fact]
    public void ServiceSuffix_IsOnlyUsedByTheCatalogServiceEntity()
    {
        var offenders = OwnTypes()
            .Where(type => type.Name.EndsWith("Service", StringComparison.Ordinal) && type.Namespace?.StartsWith(CatalogNamespace, StringComparison.Ordinal) != true)
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Controllers_DependOnlyOnTheMediatorSender()
    {
        var controllers = typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllers);
        foreach (var controller in controllers)
        {
            var parameters = controller.GetConstructors().SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType).ToList();
            Assert.True(parameters.All(type => type == typeof(ISender)), $"{controller.Name} must depend only on ISender.");
        }
    }

    [Fact]
    public void Domain_DoesNotReferenceApplicationInfrastructureOrApi()
    {
        var references = typeof(Entity).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToList();

        Assert.DoesNotContain("Oficina.Application", references);
        Assert.DoesNotContain("Oficina.Infrastructure", references);
        Assert.DoesNotContain("Oficina.Api", references);
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceTheApi()
    {
        var references = typeof(OficinaDbContext).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToList();

        Assert.DoesNotContain("Oficina.Api", references);
    }
}
