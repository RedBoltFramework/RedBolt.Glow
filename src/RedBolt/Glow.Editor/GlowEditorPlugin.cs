// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

using RedBolt.Glow.Editor;
using RedBolt.Plugins;

[assembly: PluginModule("RedBolt.Glow.Editor", typeof(GlowEditorPlugin),
    ReloadPolicy = PluginReloadPolicy.ProcessResident, AutoLoad = true)]

namespace RedBolt.Glow.Editor;

using System;
using System.IO;

using Microsoft.Extensions.Logging;

using RedBolt.Editor.Core;
using RedBolt.Logging;
using RedBolt.Resource;
using RedBolt.Utility;

using EditorProject = RedBolt.Editor.Project.Project;
using ProjectFlag = RedBolt.Editor.Project.ProjectFlag;

/// <summary>Developer-only Glow reflection, resources, and asset-pipeline integration.</summary>
public sealed class GlowEditorPlugin : EditorPluginApplication
{
    protected override void LoadEditorPlugin()
    {
        var resourcesPath = GetPluginPath("GlowCoreData");
        var cache = GetSubsystem<ResourceCache>();
        if (Directory.Exists(resourcesPath) && cache.AddResourceDir(resourcesPath))
            _resourcesPath = resourcesPath;
    }

    protected override void UnloadEditorPlugin()
    {
        if (_resourcesPath is not null)
            GetSubsystem<ResourceCache>().RemoveResourceDir(_resourcesPath);
        _resourcesPath = null;
    }

    private string? _resourcesPath;
}

[EditorExtension("Glow.DefaultLightmapUVTransformer")]
internal static class GlowDefaultLightmapUVTransformerExtension
{
    public static IDisposable Apply(EditorProject project)
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
}
