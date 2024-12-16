using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.RunContext.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using System;
using System.Collections.Generic;

class Build : BaseNukeBuildHelpers
{
    public static int Main() => Execute<Build>(x => x.Interactive);

    public override string[] EnvironmentBranches { get; } = ["prerelease", "master"];

    public override string MainEnvironmentBranch => "master";

    public BuildEntry BuildBinaries => _ => _
        .AppId("dtr_converter")
        .RunnerOS(RunnerOS.Windows2022)
        .Execute(context =>
        {
            string projectVersion = "0.0.0";
            if (context.TryGetVersionedContext(out var versionedContext))
            {
                projectVersion = versionedContext.AppVersion.Version.WithoutMetadata().ToString();
            }
            var projPath = RootDirectory / "DTRConverter" / "DTRConverter.csproj";

            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(projPath)
                .SetVersion(projectVersion)
                .SetInformationalVersion(projectVersion)
                .SetFileVersion(projectVersion)
                .SetAssemblyVersion(projectVersion)
                .SetConfiguration("Release"));
            DotNetTasks.DotNetPublish(_ => _
                .SetProject(projPath)
                .SetConfiguration("Release")
                .EnableSelfContained()
                .SetRuntime($"win-x64")
                .SetVersion(projectVersion)
                .SetInformationalVersion(projectVersion)
                .SetFileVersion(projectVersion)
                .SetAssemblyVersion(projectVersion)
                .EnablePublishSingleFile()
                .SetOutput(OutputDirectory));
        });

    public PublishEntry PublishAssets => _ => _
        .AppId("dtr_converter")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .ReleaseAsset(OutputDirectory / "dtrc.exe");
}
