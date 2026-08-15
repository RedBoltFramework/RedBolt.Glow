// Copyright (c) 2026 RedBolt contributors.
// This work is licensed under the terms of the MIT license.

namespace RedBolt.Utility;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Migrates lightmap-UV settings from the core model importer to the Glow transformer.</summary>
public static class LegacyGlowAssetPipelineMigrator
{
    public const string ModelImporterClass = "ModelImporter";
    public const string GlowTransformerClass = "GenerateLightmapUVTransformer";
    public const string GenerateProperty = "LightMap UV: Generate";
    public const string TexelsPerUnitProperty = "LightMap UV: Texels per Unit";
    public const string UVChannelProperty = "LightMap UV: Channel";

    private static readonly string[] LegacyProperties =
    [
        GenerateProperty,
        TexelsPerUnitProperty,
        UVChannelProperty,
    ];

    /// <summary>Migrate all asset pipeline files below <paramref name="rootPath"/>.</summary>
    public static int MigrateDirectory(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (!Directory.Exists(rootPath))
            return 0;

        var count = 0;
        foreach (var fileName in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (IsAssetPipeline(fileName) && TryMigrateFile(fileName))
                ++count;
        }
        return count;
    }

    /// <summary>Migrate one asset pipeline file, returning whether it was rewritten.</summary>
    public static bool TryMigrateFile(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var source = File.ReadAllText(fileName);
        var root = JsonNode.Parse(source, documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        }) as JsonObject;
        if (root?["Transformers"] is not JsonArray transformers)
            return false;

        var changed = false;
        for (var index = 0; index < transformers.Count; ++index)
        {
            if (transformers[index] is not JsonObject modelImporter
                || !string.Equals(modelImporter["_Class"]?.GetValue<string>(), ModelImporterClass,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var glowTransformer = FindGlowTransformer(transformers);
            if (glowTransformer is null)
            {
                // Even when pipeline defaults are omitted, ModelImporter.json may enable UV generation
                // for an individual model. Keep that legacy behavior by always adding the optional stage.
                glowTransformer = new JsonObject { ["_Class"] = GlowTransformerClass };
                transformers.Insert(index + 1, glowTransformer);
                ++index;
                changed = true;
            }

            var values = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
            foreach (var property in LegacyProperties)
            {
                if (modelImporter.TryGetPropertyValue(property, out var value))
                    values[property] = value?.DeepClone();
            }
            if (values.Count == 0)
                continue;

            foreach (var (property, value) in values)
            {
                if (!glowTransformer.ContainsKey(property))
                    glowTransformer[property] = value;
                modelImporter.Remove(property);
            }
            changed = true;
        }

        if (!changed)
            return false;

        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        WriteAtomically(fileName, json);
        return true;
    }

    private static JsonObject? FindGlowTransformer(JsonArray transformers)
    {
        foreach (var transformer in transformers)
        {
            if (transformer is JsonObject candidate
                && string.Equals(candidate["_Class"]?.GetValue<string>(), GlowTransformerClass,
                    StringComparison.Ordinal))
            {
                return candidate;
            }
        }
        return null;
    }

    private static bool IsAssetPipeline(string fileName) =>
        fileName.EndsWith(".assetpipeline", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".AssetPipeline.json", StringComparison.OrdinalIgnoreCase);

    private static void WriteAtomically(string fileName, string content)
    {
        var directoryName = Path.GetDirectoryName(Path.GetFullPath(fileName))!;
        var temporaryFileName = Path.Combine(
            directoryName, $".{Path.GetFileName(fileName)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryFileName, content, new UTF8Encoding(false));
            File.Move(temporaryFileName, fileName, true);
        }
        finally
        {
            if (File.Exists(temporaryFileName))
                File.Delete(temporaryFileName);
        }
    }
}
