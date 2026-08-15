#:project ../../../RedBolt/src/RedBolt/Build/Packaging/RedBolt.ExternalModule.Packaging.csproj

using System.Runtime.CompilerServices;
using RedBolt.ExternalModule.Packaging;

return ExternalModulePackager.Run(args, new ExternalModulePackageManifest
{
    Name = "RedBolt.Glow",
    BasePackages = ["RedBolt.Core", "RedBolt.Utility", "SharpCpp.Std", "SharpCpp.Support"],
    RequireBaseRuntime = true,
    SupportedRids = ["linux-x64", "win-x64"],
    DefaultRids = ["linux-x64"],
    BuildSteps =
    [
        new("Native/RedBolt.Glow.Native.csproj", BuildWhen.Linux),
        new("Native/XAtlas/RedBolt.Bindings.XAtlas.csproj", BuildWhen.Linux, true),
        new("Native/Embree/RedBolt.Bindings.Embree.csproj", BuildWhen.Linux, true),
        new("Glow/RedBolt.Glow.csproj", BuildWhen.Linux, true, "-p:BuildProjectReferences=false"),
        new("Glow.Tools/RedBolt.Glow.Tools.csproj", BuildWhen.Linux, true, "-p:BuildProjectReferences=false"),
        new("Glow/RedBolt.Glow.csproj", BuildWhen.Windows, false,
            "-p:RedBoltPlatform=win-x64", "-p:RedBoltNativePreset=win-x64")
    ],
    ResourcePackages =
    [
        new("RedBolt.Glow.CoreData", "CoreData", "bin/CoreData")
    ],
    ManagedPackages =
    [
        new(
            "RedBolt.Glow",
            "Lightmap UV generation and baked-lighting runtime for RedBolt.",
            [
                new("build/bin/Release/linux-x64/core/RedBolt.Glow.dll", "lib/net10.0/RedBolt.Glow.dll"),
                new("build/bin/Release/linux-x64/extern/RedBolt.Bindings.XAtlas.dll", "lib/net10.0/RedBolt.Bindings.XAtlas.dll"),
                new("build/bin/Release/linux-x64/extern/RedBolt.Bindings.Embree.dll", "lib/net10.0/RedBolt.Bindings.Embree.dll")
            ],
            [
                new("RedBolt.Core", PackageVersions.RedBolt),
                new("SharpCpp.Std", PackageVersions.RedBolt),
                new("SharpCpp.Support", PackageVersions.RedBolt),
                new("RedBolt.Glow.CoreData", PackageVersions.Module)
            ]),
        new(
            "RedBolt.Glow.Tools",
            "Asset-pipeline lightmap UV generation for RedBolt Glow developer workflows.",
            [new("build/bin/Release/linux-x64/core/RedBolt.Glow.Tools.dll", "lib/net10.0/RedBolt.Glow.Tools.dll")],
            [
                new("RedBolt.Glow", PackageVersions.Module),
                new("RedBolt.Utility", PackageVersions.RedBolt)
            ])
    ],
    RuntimePackage = new(
        "RedBolt.Glow.Runtime",
        "Glow",
        "*RedBolt.Native.*",
        "",
        [
            new("RedBolt.Glow", PackageVersions.Module),
            new("RedBolt.Core.Runtime.{rid}", PackageVersions.RedBolt)
        ]),
    Consumer = new(
        "RedBolt.Glow.Runtime.linux-x64",
        """
        using System;
        using RedBolt.Graphics;
        Console.WriteLine(typeof(LightBaker).FullName);
        """,
        OnlyWithLinuxRuntime: true)
}, SourcePath());

static string SourcePath([CallerFilePath] string path = "") => path;
