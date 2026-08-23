# RedBolt.Glow repository

## Scope

- `src/RedBolt/Glow` is the game-shipping baked-lighting module.
- `src/RedBolt/Glow.Tools` owns asset-pipeline lightmap UV generation.
- `src/RedBolt/Glow.Editor` is the optional process-resident Editor companion.
- `src/RedBolt/Sample/BakedLighting` is the module-owned baking sample.
- `bin/CoreData.Glow` contains shipping Glow resources; `bin/SampleData.Glow` contains module test data.

Keep RedBolt Core independent from Glow. Do not add a Glow abstraction layer: runtime users reference Glow directly and Editor integration belongs in the companion plugin.

## Compatibility

`.assetpipeline` and per-model `ModelImporter.json` are the only user-facing configuration files for model lightmap UV generation. Preserve their existing field names and behavior; do not introduce a companion-specific parameter file.

## Dependencies and verification

The sibling `../RedBolt` repository is required. Shared build and packaging helpers stay in RedBolt. XAtlas and Embree use CPM; do not add git submodules. Run one build invocation at a time and follow `../RedBolt/AGENTS.md`.
