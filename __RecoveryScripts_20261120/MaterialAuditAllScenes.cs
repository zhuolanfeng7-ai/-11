using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MaterialAuditAllScenes
{
    private const string Request = "Temp/RequestMaterialAudit.flag";
    private const string Report = "Temp/MaterialAuditReport.txt";

    static MaterialAuditAllScenes()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(Request))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunIfRequested;
            return;
        }

        File.Delete(Request);
        var lines = new List<string>();
        var openedForAudit = new List<Scene>();
        try
        {
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string scenePath in scenePaths)
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                bool wasLoaded = scene.IsValid() && scene.isLoaded;
                if (!wasLoaded)
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    openedForAudit.Add(scene);
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                    ScanHierarchy(scenePath, root.transform, lines);
            }

            string[] materialPaths = AssetDatabase.FindAssets("t:Material")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (string materialPath in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                    lines.Add($"MATERIAL_LOAD_FAILED | {materialPath}");
                else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                    lines.Add($"MATERIAL_SHADER_MISSING | {materialPath} | {material.name}");
            }

            File.WriteAllLines(Report, new[]
            {
                $"SCENES={scenePaths.Length}",
                $"ISSUES={lines.Count}",
                $"TIME={DateTime.Now:O}"
            }.Concat(lines));
            Debug.Log($"[Material Audit] Finished. Issues={lines.Count}. Report={Report}");
        }
        catch (Exception exception)
        {
            File.WriteAllText(Report, "AUDIT_FAILED\n" + exception);
            Debug.LogException(exception);
        }
        finally
        {
            foreach (Scene scene in openedForAudit.Where(scene => scene.IsValid() && scene.isLoaded))
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ScanHierarchy(string scenePath, Transform transform, List<string> lines)
    {
        foreach (Renderer renderer in transform.GetComponents<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                string objectPath = GetPath(transform);
                if (material == null)
                {
                    lines.Add($"NULL_MATERIAL_SLOT | {scenePath} | {objectPath} | Renderer={renderer.GetType().Name} | Slot={index}");
                    continue;
                }

                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    lines.Add($"ERROR_SHADER | {scenePath} | {objectPath} | Slot={index} | Material={material.name} | Asset={materialPath}");
                }
            }
        }

        for (int index = 0; index < transform.childCount; index++)
            ScanHierarchy(scenePath, transform.GetChild(index), lines);
    }

    private static string GetPath(Transform transform)
    {
        var names = new Stack<string>();
        while (transform != null)
        {
            names.Push(transform.name);
            transform = transform.parent;
        }
        return string.Join("/", names);
    }
}
