# RedBolt.Glow NuGet packages

RedBolt.Glow versions independently from RedBolt, while recording exact dependencies on the base RedBolt package version used for the build. Produce the matching base packages first in `../RedBolt/build/package/nuget/out`.

Create the module packages with:

```sh
dotnet run --file packaging/nuget/package.cs -- create
```

Use `--no-build` when Release outputs already exist. The command produces `RedBolt.Glow`, `RedBolt.Glow.Tools`, `RedBolt.Glow.CoreData`, and RID-specific `RedBolt.Glow.Runtime` packages.

Validate package contents and a package-only consumer with:

```sh
dotnet run --file packaging/nuget/package.cs -- validate
```
