using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public static class BuildScenesAutoSetup
{
    static BuildScenesAutoSetup()
    {
        EnsureScenesInBuildSettings();
    }

    [MenuItem("Tools/BlockBlast/Sync Build Settings Scenes")]
    public static void EnsureScenesInBuildSettings()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/Loading scene.unity",
            "Assets/Scenes/Main scene.unity",
            "Assets/Scenes/Classicmode.unity",
            "Assets/Scenes/Setting scene.unity"
        };

        List<EditorBuildSettingsScene> currentScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool modified = false;

        foreach (string path in scenePaths)
        {
            if (File.Exists(path))
            {
                bool found = false;
                foreach (var s in currentScenes)
                {
                    if (s.path == path)
                    {
                        found = true;
                        if (!s.enabled)
                        {
                            s.enabled = true;
                            modified = true;
                        }
                        break;
                    }
                }

                if (!found)
                {
                    currentScenes.Add(new EditorBuildSettingsScene(path, true));
                    modified = true;
                }
            }
        }

        if (modified)
        {
            EditorBuildSettings.scenes = currentScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[BuildScenesAutoSetup] Đã tự động cập nhật danh sách scene vào Build Settings thành công!</color>");
        }
    }
}

