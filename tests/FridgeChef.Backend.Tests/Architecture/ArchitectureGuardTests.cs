using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;

namespace FridgeChef.Backend.Tests.Architecture;

public sealed partial class ArchitectureGuardTests
{
    [Fact]
    public void ProductionCode_ShouldNotDeclareOptionalParameters()
    {
        var offenders = EnumerateProductionFiles()
            .SelectMany(file => OptionalParameterRegex()
                .Matches(File.ReadAllText(file))
                .Select(match => ToRelativePath(file)))
            .Distinct()
            .OrderBy(path => path)
            .ToList();

        offenders.Should().BeEmpty("project style requires all parameter values to be passed explicitly");
    }

    [Fact]
    public void ApplicationAndDomainProjects_ShouldNotReferenceInfrastructureOrApi()
    {
        var offenders = EnumerateProjectFiles()
            .Select(projectFile => new
            {
                Project = ToRelativePath(projectFile),
                References = GetProjectReferences(projectFile)
            })
            .Where(project =>
                (project.Project.Contains(".Application/", StringComparison.Ordinal) ||
                 project.Project.Contains(".Domain/", StringComparison.Ordinal)) &&
                project.References.Any(reference =>
                    reference.Contains(".Infrastructure/", StringComparison.Ordinal) ||
                    reference.Contains("FridgeChef.Api/", StringComparison.Ordinal)))
            .Select(project => project.Project)
            .OrderBy(path => path)
            .ToList();

        offenders.Should().BeEmpty("inner layers must not depend on infrastructure or API projects");
    }

    [Fact]
    public void SwaggerExamples_ShouldUseExistingEndpointRoutes()
    {
        var examplesSource = File.ReadAllText(
            Path.Combine(ProjectRoot, "src/FridgeChef.Api/Extensions/RequestExamplesOperationFilter.cs"));

        examplesSource.Should().Contain("[\"/auth/registration|POST\"]");
        examplesSource.Should().Contain("[\"/auth/sessions|POST\"]");
        examplesSource.Should().Contain("[\"/auth/tokens|POST\"]");
        examplesSource.Should().Contain("[\"/users/me/password|PUT\"]");
        examplesSource.Should().Contain("[\"/recipes/matches|POST\"]");
        examplesSource.Should().Contain("[\"/admin/pricing/test-queries|POST\"]");

        examplesSource.Should().NotContain("/auth/register");
        examplesSource.Should().NotContain("/auth/login");
        examplesSource.Should().NotContain("/auth/refresh");
        examplesSource.Should().NotContain("/users/me/change-password");
        examplesSource.Should().NotContain("/recipes/search");
        examplesSource.Should().NotContain("/admin/pricing/search-test");
    }

    private static IEnumerable<string> EnumerateProductionFiles()
    {
        return Directory.EnumerateFiles(Path.Combine(ProjectRoot, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IEnumerable<string> EnumerateProjectFiles()
    {
        return Directory.EnumerateFiles(Path.Combine(ProjectRoot, "src"), "*.csproj", SearchOption.AllDirectories);
    }

    private static List<string> GetProjectReferences(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile)
            ?? throw new InvalidOperationException($"Cannot resolve project directory for {projectFile}.");
        var document = XDocument.Load(projectFile);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .Select(ToRelativePath)
            .ToList();
    }

    private static string ToRelativePath(string path) =>
        Path.GetRelativePath(ProjectRoot, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string ProjectRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FridgeChef.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new InvalidOperationException("Cannot locate repository root.");
        }
    }

    [GeneratedRegex(@"(?:\(|,)\s*(?:\[[^\]]+\]\s*)?(?:(?:this|ref|in|out)\s+)?[A-Za-z_][A-Za-z0-9_.<>\[\]?]*\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*(?:default|null|true|false|-?\d+|""[^""]*"")")]
    private static partial Regex OptionalParameterRegex();
}
