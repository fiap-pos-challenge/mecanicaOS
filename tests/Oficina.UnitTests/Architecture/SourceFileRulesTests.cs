using System.Text.RegularExpressions;

namespace Oficina.UnitTests.Architecture;

public sealed partial class SourceFileRulesTests
{
    private static readonly string SourceRoot = Path.Combine(RepositoryRoot(), "src");

    [Fact]
    public void EverySourceFile_DeclaresOneTopLevelTypeNamedAfterTheFile()
    {
        var offenders = new List<string>();
        foreach (var file in SourceFiles())
        {
            var names = TopLevelType().Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value).ToList();
            if (names.Count > 1 || (names.Count == 1 && names[0] != TypeNameOf(file)))
            {
                offenders.Add($"{Path.GetRelativePath(SourceRoot, file)}: {string.Join(", ", names)}");
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void SourceFiles_HaveNoComments()
    {
        var offenders = SourceFiles().Where(file => Comment().IsMatch(File.ReadAllText(file))).Select(file => Path.GetRelativePath(SourceRoot, file)).ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Rounding_OnlyHappensInTheDomainValueObjects()
    {
        var offenders = SourceFiles()
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}ValueObjects{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => RoundingBypass().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(SourceRoot, file))
            .ToList();

        Assert.Empty(offenders);
    }

    private static string TypeNameOf(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file).Split('.')[0];
        return name.EndsWith("OfT", StringComparison.Ordinal) ? name[..^3] : name;
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Oficina.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Oficina.slnx");
    }

    [GeneratedRegex(@"^(?:public|internal)\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+|readonly\s+)*(?:class|record|interface|enum|struct)\s+(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [GeneratedRegex(@"^\s*(//|/\*)", RegexOptions.Multiline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"\bdecimal\.Round\b|\bMidpointRounding\b|\bMath\.Round\b|\bRounding\.HalfUp\b")]
    private static partial Regex RoundingBypass();
}
