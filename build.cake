#addin nuget:?package=Cake.Git&version=0.20.0

var target = Argument("target", "All");
var exe = "foo";
var solution = $"{exe}.sln";
var configuration = Argument("configuration", "Debug");
var outputDir = new DirectoryPath($"./bin/{configuration}");

DotNetCoreMSBuildSettings MsBuildFlags = new DotNetCoreMSBuildSettings()
    .SetConfiguration(configuration);

#load "version.cake"

Information($"Version:           {version}");
Information($"Is Tagged Release: {isTaggedRelease}");
Information($"Configuration:     {configuration}");
Information($"ReleaseNotes:      {releaseNotesPropsFile}");

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetCoreBuild($"{solution}", new DotNetCoreBuildSettings {
            MSBuildSettings = MsBuildFlags,
        });
    });

Task("Restore")
    .Does(() =>
    {
        DotNetCoreRestore($"{solution}", new DotNetCoreRestoreSettings {
            MSBuildSettings = MsBuildFlags,
        });
    });

Task("Test")
    .Does(() =>
    {
        SolutionParserResult parsedSolution = ParseSolution(solution);

        foreach (SolutionProject project in parsedSolution.Projects)
            if (project.Path.FullPath.Contains("tests/"))
                DotNetCoreTest(project.Path.FullPath);
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() => {
        DotNetCorePack(solution, new DotNetCorePackSettings {
            MSBuildSettings = MsBuildFlags,
            OutputDirectory = outputDir,
        });
    });

Task("Push")
    .IsDependentOn("Pack")
    .WithCriteria(releaseNotesPropsFile != null && FileExists(releaseNotesPropsFile), "Verify release notes exist!")
    .WithCriteria(!version.Contains("dirty"), "Dirty builds cannot be pushed")
    .Does(() =>
    {
        Information("Packages were pushed!");
    });

Task("All")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
