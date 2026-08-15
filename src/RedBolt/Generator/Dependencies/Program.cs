namespace RedBolt.Generator.Dependencies;

using System;
using System.IO;

using SharpCpp.Model.Logging;

internal static class Program
{
    private static int Main(string[] args)
    {
        StreamWriter? logWriter = null;
        try
        {
            var options = GeneratorOptions.Parse(args);
            BindingTargetGenerator generator = options.Surface.ToLowerInvariant() switch
            {
                "xatlas" => new XAtlasGenerator(options.NativeLibraryName),
                "embree" => new EmbreeGenerator(options.NativeLibraryName),
                _ => throw new ArgumentException($"Unknown binding surface '{options.Surface}'.", nameof(options.Surface))
            };
            Directory.CreateDirectory(options.OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(options.LogPath)!);
            logWriter = new StreamWriter(options.LogPath, false);
            SharpCppLog.Configure(fileWriter: logWriter);

            var summary = generator.Generate(options);
            Console.WriteLine(summary);
            logWriter.WriteLine(summary);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            logWriter?.WriteLine(exception);
            Console.Error.WriteLine(
                "Usage: RedBolt.Glow.Generator.Dependencies --surface <xatlas|embree> --header <path> --include-name <include> --output-dir <dir> --file-stem <stem> --metadata <path> --log <path> --sharpcpp-root <path> --redbolt-root <path> --native-library-name <name> --compiler <path> [--compiler-target <triple>] [--compiler-sysroot <path>] [--include <dir>]... [--define <name[=value]>]... [--owned-header <path>]...");
            return 1;
        }
        finally
        {
            SharpCppLog.Configure();
            logWriter?.Dispose();
        }
    }
}
