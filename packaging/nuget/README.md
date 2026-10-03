# RedBolt.Glow NuGet packages

RedBolt.Glow versions independently from RedBolt, while recording exact dependencies on the base RedBolt package version used for the build. Produce the matching base packages first in `<build root>/package/nuget/RedBolt/out` (`<build root>` is the monorepo `build/` inside the monorepo and the repository's own `build/` standalone; `REDBOLT_BUILD_ROOT` overrides it).

Create the module packages with:

```sh
dotnet run --file packaging/nuget/package.cs -- create
```

Use `--no-build` when Release outputs already exist. The command produces `RedBolt.Glow`, `RedBolt.Glow.Tools`, `RedBolt.Glow.CoreData`, and RID-specific `RedBolt.Glow.Runtime` packages.

Validate package contents and a package-only consumer with:

```sh
dotnet run --file packaging/nuget/package.cs -- validate
```
