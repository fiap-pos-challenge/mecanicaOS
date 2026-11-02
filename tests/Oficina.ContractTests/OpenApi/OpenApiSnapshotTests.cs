using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Oficina.ContractTests.Fixtures;

namespace Oficina.ContractTests.OpenApi;

[Collection(nameof(ContractCollection))]
public sealed class OpenApiSnapshotTests(ContractApiFactory factory)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    [Fact]
    public async Task OpenApiDocument_MatchesTheVersionedSnapshot()
    {
        var current = Normalize(await LoadDocumentAsync());
        var snapshotPath = SnapshotPath();

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI_SNAPSHOT") == "1")
        {
            await File.WriteAllTextAsync(snapshotPath, current + "\n", TestContext.Current.CancellationToken);
        }

        Assert.True(File.Exists(snapshotPath), "Run with UPDATE_OPENAPI_SNAPSHOT=1 to create the snapshot.");
        var snapshot = await File.ReadAllTextAsync(snapshotPath, TestContext.Current.CancellationToken);
        Assert.Equal(Normalize(JsonNode.Parse(snapshot)!), current);
    }

    [Fact]
    public async Task EveryOperation_HasSummaryAndDeclaresSuccessAndErrorResponses()
    {
        var document = await LoadDocumentAsync();

        foreach (var (path, operation) in Operations(document))
        {
            var responses = operation["responses"]!.AsObject().Select(response => response.Key).ToArray();
            Assert.False(string.IsNullOrWhiteSpace((string?)operation["summary"]), path);
            Assert.Contains(responses, status => status.StartsWith('2'));
            Assert.Contains("500", responses);
            if (path != "post /api/auth/login")
            {
                Assert.Contains("401", responses);
            }

            if (path.Contains("/api/admin/", StringComparison.Ordinal))
            {
                Assert.Contains("403", responses);
            }

            foreach (var error in operation["responses"]!.AsObject().Where(response => response.Key[0] is '4' or '5'))
            {
                Assert.NotNull(error.Value!["content"]?["application/problem+json"]);
            }
        }
    }

    [Fact]
    public async Task Document_DeclaresTheBearerScheme()
    {
        var document = await LoadDocumentAsync();

        Assert.Equal("bearer", (string?)document["components"]!["securitySchemes"]!["Bearer"]!["scheme"]);
    }

    private static IEnumerable<(string Path, JsonNode Operation)> Operations(JsonNode document) =>
        document["paths"]!.AsObject().SelectMany(path => path.Value!.AsObject()
            .Select(operation => ($"{operation.Key} {path.Key}", operation.Value!)));

    private static string Normalize(JsonNode node) => node.ToJsonString(Indented).ReplaceLineEndings("\n");

    private static string SnapshotPath([CallerFilePath] string sourceFile = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots", "openapi.json");

    private async Task<JsonNode> LoadDocumentAsync()
    {
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
        return JsonNode.Parse(json)!;
    }
}
