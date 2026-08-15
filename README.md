# RedBolt.Glow

Baked-lighting support for [RedBolt](https://github.com/RedBoltFramework/RedBolt).

The repository is split by shipping intent:

- `RedBolt.Glow` contains lightmap UV generation, baking, runtime data, and the `LightBaker` component.
- `RedBolt.Glow.Tools` contains the asset transformer that preserves the established `.assetpipeline` and per-model `ModelImporter.json` workflows.
- `RedBolt.Glow.Editor` is an automatically discovered Editor companion. It registers Glow reflection and contributes the UV-generation transformer through RedBolt's plugin system.
- `RedBolt.Sample.BakedLighting` demonstrates rebaking and displaying Glow lightmaps and probes.

Core has no Glow-specific interface or dependency. Games use `RedBolt.Glow` directly; removing `RedBolt.Glow.Editor` removes only developer integration.

Third-party XAtlas and Embree sources are acquired through CPM. A sibling `../RedBolt` checkout is required.
