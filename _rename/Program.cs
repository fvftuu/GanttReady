using System;
using System.IO;
using System.Text;

string srcDir = args.Length > 0 ? args[0] : @"src\GanttReady.Server";
int count = 0;

foreach (var file in Directory.EnumerateFiles(srcDir, "*.*", SearchOption.AllDirectories))
{
    string ext = Path.GetExtension(file).ToLower();
    if (ext != ".cs" && ext != ".razor" && ext != ".cshtml" && ext != ".json" && ext != ".csproj") continue;
    if (file.Contains("\\bin\\") || file.Contains("\\obj\\") || file.Contains("\\node_modules\\")) continue;

    string content = File.ReadAllText(file, Encoding.UTF8);
    if (!content.Contains("NetPlan.Server")) continue;

    string newContent = content.Replace("NetPlan.Server", "GanttReady.Server");
    File.WriteAllText(file, newContent, new UTF8Encoding(true));
    Console.WriteLine(file);
    count++;
}

Console.WriteLine($"Done. {count} files updated.");
