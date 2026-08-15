// Copyright (c) 2026 RedBolt contributors.
// This work is licensed under the terms of the MIT license.

using RedBolt.Glow.Editor;
using RedBolt.Plugins;

[assembly: PluginModule("RedBolt.Glow.Editor", typeof(GlowEditorPlugin),
    ReloadPolicy = PluginReloadPolicy.ProcessResident, AutoLoad = true)]

namespace RedBolt.Glow.Editor;

using System;
using System.IO;

using Microsoft.Extensions.Logging;

using RedBolt.Core;
using RedBolt.Editor.Core;
using RedBolt.Graphics;
using RedBolt.Logging;
using RedBolt.Resource;
using RedBolt.Utility;

using EditorProject = RedBolt.Editor.Project.Project;
using ProjectFlag = RedBolt.Editor.Project.ProjectFlag;

/// <summary>Developer-only Glow reflection, resources, and asset-pipeline integration.</summary>
public sealed class GlowEditorPlugin : PluginApplication
{
    protected override void Load()
    {
        _glowReflection = Context.RegisterAssemblyScoped(typeof(LightBaker).Assembly);
        _toolsReflection = Context.RegisterAssemblyScoped(typeof(GenerateLightmapUVTransformer).Assembly);

        var assemblyDirectory = Path.GetDirectoryName(typeof(GlowEditorPlugin).Assembly.Location)!;
        var resourcesPath = Path.Combine(assemblyDirectory, "GlowCoreData");
        var cache = GetSubsystem<ResourceCache>();
        if (Directory.Exists(resourcesPath) && cache.AddResourceDir(resourcesPath))
            _resourcesPath = resourcesPath;

        _registration = GetSubsystem<EditorPluginManager>().AddScopedPlugin<EditorProject>(
            "Glow.DefaultLightmapUVTransformer", ApplyProject);
    }

    protected override void Unload()
    {
        _registration?.Dispose();
        _registration = null;

        if (_resourcesPath is not null)
            GetSubsystem<ResourceCache>().RemoveResourceDir(_resourcesPath);
        _resourcesPath = null;

        _toolsReflection?.Dispose();
        _toolsReflection = null;
        _glowReflection?.Dispose();
        _glowReflection = null;
    }

    private static IDisposable ApplyProject(EditorProject project)
    {
        if ((project.Flags & ProjectFlag.ReadOnly) == 0)
        {
            try
            {
                var count = LegacyGlowAssetPipelineMigrator.MigrateDirectory(project.DataPath);
                if (count != 0)
                {
                    Log.GetLogger().LogInformation(
                        "Migrated Glow settings in {AssetPipelineCount} asset pipeline file(s)", count);
                }
            }
            catch (Exception exception)
            {
                Log.GetLogger().LogError(exception, "Failed to migrate legacy Glow asset-pipeline settings");
            }
        }

        return project.RegisterDefaultAssetTransformer<GenerateLightmapUVTransformer>();
    }

    private IDisposable? _registration;
    private AssemblyRegistration? _toolsReflection;
    private AssemblyRegistration? _glowReflection;
    private string? _resourcesPath;
}
