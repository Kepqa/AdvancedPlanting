using System;
using System.IO;
using AdvancedPlanting.UI;

// Usage: dotnet run --project tools/IconPreview -- <output-dir>
// Writes <name>.rgba (256x256, RGBA, rows top to bottom) for every icon.
string outDir = args.Length > 0 ? args[0] : "icons";
Directory.CreateDirectory(outDir);
foreach (string name in IconArt.Names)
{
    File.WriteAllBytes(Path.Combine(outDir, name + ".rgba"), IconArt.Render(name));
    Console.WriteLine(name);
}
