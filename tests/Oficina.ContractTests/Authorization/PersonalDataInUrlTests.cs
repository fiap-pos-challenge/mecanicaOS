using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Oficina.ContractTests.Fixtures;

namespace Oficina.ContractTests.Authorization;

[Collection(nameof(ContractCollection))]
public sealed class PersonalDataInUrlTests(ContractApiFactory factory)
{
    private static readonly string[] PersonalData = { "document", "cpf", "cnpj", "email", "phone", "password" };

    [Fact]
    public void NoRouteOrQueryParameter_CarriesPersonalData()
    {
        var descriptions = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .ToArray();

        Assert.NotEmpty(descriptions);
        foreach (var description in descriptions)
        {
            var urlParameters = description.ParameterDescriptions
                .Where(parameter => parameter.Source == BindingSource.Path || parameter.Source == BindingSource.Query)
                .Select(parameter => parameter.Name.ToLowerInvariant());
            Assert.DoesNotContain(urlParameters, name => PersonalData.Any(name.Contains));
            Assert.DoesNotContain(PersonalData, word => description.RelativePath!.ToLowerInvariant().Split('/').Any(segment => segment.Contains(word, StringComparison.Ordinal)));
        }
    }
}
