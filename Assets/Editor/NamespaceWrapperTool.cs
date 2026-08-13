using UnityEditor;
using UnityEngine;
using System.IO;

public class NamespaceWrapperTool : EditorWindow
{
    private string folderPath = "Assets/chess/Scripts";
    private string namespaceName = "ChessGame";
    private string lastBackupPath = "";

    [MenuItem("Tools/Namespace Wrapper")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceWrapperTool>("Namespace Wrapper");
    }

    void OnGUI()
    {
        GUILayout.Label("Wrap all scripts in a folder with a namespace", EditorStyles.boldLabel);
        GUILayout.Space(10);

        folderPath = EditorGUILayout.TextField("Folder Path (relative to project)", folderPath);
        namespaceName = EditorGUILayout.TextField("Namespace Name", namespaceName);

        GUILayout.Space(10);

        if (GUILayout.Button("Backup + Wrap Namespace"))
        {
            if (EditorUtility.DisplayDialog("Confirm",
                $"This will backup and then wrap all .cs files in:\n{folderPath}\n\ninto namespace '{namespaceName}'.\n\nContinue?",
                "Yes, do it", "Cancel"))
            {
                lastBackupPath = BackupFolder();
                WrapFolder();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", "Wrapping complete.\nBackup saved OUTSIDE Assets at:\n" + lastBackupPath, "OK");
            }
        }

        GUILayout.Space(15);

        GUI.enabled = !string.IsNullOrEmpty(lastBackupPath) && Directory.Exists(lastBackupPath);
        if (GUILayout.Button("Restore Last Backup (undo wrap)"))
        {
            if (EditorUtility.DisplayDialog("Confirm Restore",
                "This will overwrite the current files in:\n" + folderPath + "\n\nwith the backup version. Continue?",
                "Yes, restore", "Cancel"))
            {
                RestoreBackup(lastBackupPath, folderPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", "Files restored from backup.", "OK");
            }
        }
        GUI.enabled = true;

        GUILayout.Space(15);
        EditorGUILayout.HelpBox("Backups are saved OUTSIDE the Assets folder (in a '_UnityBackups' folder next to your project), so Unity never compiles them by accident. Use 'Restore Last Backup' to undo, like Ctrl+Z.", MessageType.Info);
    }

    private string GetBackupRoot()
    {
        // Project root = one level above Assets
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, "_UnityBackups");
    }

    private string BackupFolder()
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Folder not found: " + folderPath, "OK");
            return "";
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderName = Path.GetFileName(folderPath.TrimEnd('/', '\\'));
        string backupPath = Path.Combine(GetBackupRoot(), $"{folderName}_{timestamp}");

        CopyDirectory(folderPath, backupPath);
        Debug.Log("Backup created OUTSIDE Assets at: " + backupPath);
        return backupPath;
    }

    private void RestoreBackup(string backupPath, string targetPath)
    {
        if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
        {
            Debug.LogError("Backup path not found: " + backupPath);
            return;
        }

        // Delete current files, then copy backup back in
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
            // also delete the leftover .meta file for the folder itself if present
            string metaFile = targetPath.TrimEnd('/', '\\') + ".meta";
            if (File.Exists(metaFile)) File.Delete(metaFile);
        }

        CopyDirectory(backupPath, targetPath);
        Debug.Log("Restored from backup: " + backupPath + " -> " + targetPath);
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir, "*.cs"))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirNameOnly = Path.GetFileName(subDir);
            // never recurse into meta/backup artifacts
            string destSubDir = Path.Combine(destDir, dirNameOnly);
            CopyDirectory(subDir, destSubDir);
        }
    }

    private void WrapFolder()
    {
        WrapFilesRecursively(folderPath);
    }

    private void WrapFilesRecursively(string dir)
    {
        foreach (string file in Directory.GetFiles(dir, "*.cs"))
        {
            WrapFile(file);
        }

        foreach (string subDir in Directory.GetDirectories(dir))
        {
            WrapFilesRecursively(subDir);
        }
    }

    private void WrapFile(string filePath)
    {
        string content = File.ReadAllText(filePath);

        if (content.Contains($"namespace {namespaceName}"))
        {
            Debug.Log($"Skipped (already wrapped): {filePath}");
            return;
        }

        var lines = content.Split('\n');
        int lastUsingIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("using "))
            {
                lastUsingIndex = i;
            }
        }

        string usingsPart = "";
        string restPart = "";

        if (lastUsingIndex >= 0)
        {
            usingsPart = string.Join("\n", lines, 0, lastUsingIndex + 1);
            restPart = string.Join("\n", lines, lastUsingIndex + 1, lines.Length - lastUsingIndex - 1);
        }
        else
        {
            restPart = content;
        }

        string wrapped = usingsPart + "\n\nnamespace " + namespaceName + "\n{\n" + restPart + "\n}\n";

        File.WriteAllText(filePath, wrapped);
        Debug.Log("Wrapped: " + filePath);
    }
}