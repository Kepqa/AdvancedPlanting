using System;
using System.IO;
using AdvancedPlanting.UI;

// Usage: dotnet run --project tools/IconPreview -- <output-dir>
// Writes icons/<name>.rgba (256x256) and ui/<name>.rgba + <name>.txt ("width height border")
// as RGBA with rows top to bottom.
string outDir = args.Length > 0 ? args[0] : "art";
string icons = Path.Combine(outDir, "icons");
string ui = Path.Combine(outDir, "ui");
Directory.CreateDirectory(icons);
Directory.CreateDirectory(ui);
foreach (string name in IconArt.Names)
    File.WriteAllBytes(Path.Combine(icons, name + ".rgba"), IconArt.Render(name));
foreach (string name in UiArt.Names)
{
    UiArt.Image img = UiArt.Render(name);
    File.WriteAllBytes(Path.Combine(ui, name + ".rgba"), img.Rgba);
    File.WriteAllText(Path.Combine(ui, name + ".txt"), $"{img.Width} {img.Height} {img.BorderL}");
}
Console.WriteLine("ok");
