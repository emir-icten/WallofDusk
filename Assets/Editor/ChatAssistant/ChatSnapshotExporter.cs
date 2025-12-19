using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

public static class ChatSnapshotExporter
{
    // Zip’e dahil edilecek kök klasör/dosyalar:
    // Büyük assetleri şişirmemek için Assets'in tamamını değil,
    // genelde kod tarafını alıyoruz. Gerekirse burayı genişletebilirsin.
    private static readonly string[] IncludePaths =
    {
        "Assets/Scripts",
        "Assets/Editor",
        "Packages",          // manifest.json burada
        "ProjectSettings"
    };

    // Zip’e dahil ETME (gereksiz büyütür):
    private static readonly string[] ExcludeContains =
    {
        "/Library/",
        "/Temp/",
        "/Obj/",
        "/Build/",
        "/Logs/",
        "/UserSettings/",
        "/.git/",
        "/.vs/",
    };

    /// <summary>
    /// Snapshot zip oluşturur. Başarılıysa zip path döner, iptalse/hataysa "" döner.
    /// </summary>
    public static string ExportSnapshotZip()
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            string savePath = EditorUtility.SaveFilePanel(
                "Save Snapshot ZIP",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"UnitySnapshot_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
                "zip"
            );

            if (string.IsNullOrEmpty(savePath))
                return "";

            if (File.Exists(savePath))
                File.Delete(savePath);

            using (var zip = ZipFile.Open(savePath, ZipArchiveMode.Create))
            {
                // Proje bilgisi: benim debug/analiz için çok işime yarıyor
                AddSnapshotInfo(zip);

                foreach (var rel in IncludePaths)
                {
                    string full = Path.Combine(projectRoot, rel);

                    if (!Directory.Exists(full) && !File.Exists(full))
                        continue;

                    if (Directory.Exists(full))
                    {
                        AddDirectory(zip, projectRoot, full);
                    }
                    else
                    {
                        AddFile(zip, projectRoot, full);
                    }
                }
            }

            EditorUtility.RevealInFinder(savePath);
            Debug.Log($"Snapshot ZIP created: {savePath}");
            EditorUtility.DisplayDialog("Snapshot ZIP", "ZIP oluşturuldu ✅\nŞimdi buraya yükleyebilirsin.", "OK");
            return savePath;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Snapshot ZIP Error", ex.Message, "OK");
            return "";
        }
    }

    private static void AddSnapshotInfo(ZipArchive zip)
    {
        try
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;

            string info =
$@"Snapshot Info
Date: {DateTime.Now}
Unity: {Application.unityVersion}
Platform: {EditorUserBuildSettings.activeBuildTarget}
ColorSpace: {PlayerSettings.colorSpace}
Scripting Backend: {PlayerSettings.GetScriptingBackend(group)}
Api Compatibility: {PlayerSettings.GetApiCompatibilityLevel(group)}
Company: {PlayerSettings.companyName}
Product: {PlayerSettings.productName}
";

            var entry = zip.CreateEntry("SnapshotInfo.txt");
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(info);
            }
        }
        catch
        {
            // info eklenemese de zip üretimi devam etsin
        }
    }

    private static void AddDirectory(ZipArchive zip, string projectRoot, string dirPath)
    {
        var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (ShouldExclude(file)) continue;
            AddFile(zip, projectRoot, file);
        }
    }

    private static void AddFile(ZipArchive zip, string projectRoot, string filePath)
    {
        if (ShouldExclude(filePath)) return;

        string rel = MakeRelativePath(projectRoot, filePath);
        rel = rel.Replace("\\", "/"); // zip standardı

        // Unity 6'da CompressionLevel çakışması olabildiği için full-qualify:
        zip.CreateEntryFromFile(filePath, rel, System.IO.Compression.CompressionLevel.Optimal);
    }

    private static bool ShouldExclude(string path)
    {
        string norm = path.Replace("\\", "/");

        foreach (var ex in ExcludeContains)
        {
            if (norm.Contains(ex, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // İstersen ekstra filtreler:
        // - dev dosyalar: .psd .mp4 .mov vb.
        // - build çıktıları, cache vs.
        // Buraya eklenebilir.

        return false;
    }

    private static string MakeRelativePath(string root, string fullPath)
    {
        var rootUri = new Uri(root.EndsWith(Path.DirectorySeparatorChar.ToString()) ? root : root + Path.DirectorySeparatorChar);
        var fileUri = new Uri(fullPath);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
    }
}
