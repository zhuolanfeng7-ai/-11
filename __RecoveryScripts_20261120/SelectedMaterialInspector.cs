using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectedMaterialInspector
{
    private const string Request = "Temp/RequestSelectedMaterialInspect.flag";
    private const string Report = "Temp/SelectedMaterialInspect.txt";

    static SelectedMaterialInspector() { EditorApplication.delayCall += Run; }

    private static void Run()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        File.Delete(Request);
        var lines = new List<string>();
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            File.WriteAllText(Report, "NO_SELECTION");
            return;
        }
        lines.Add("SELECTED=" + GetPath(selected.transform));
        foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
        {
            lines.Add("RENDERER=" + GetPath(renderer.transform) + " | " + renderer.GetType().Name);
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null)
                {
                    lines.Add("  SLOT=" + index + " NULL");
                    continue;
                }
                lines.Add("  SLOT=" + index + " MAT=" + material.name + " ASSET=" + AssetDatabase.GetAssetPath(material) + " SHADER=" + (material.shader ? material.shader.name : "NULL"));
                if (material.shader == null) continue;
                int count = ShaderUtil.GetPropertyCount(material.shader);
                for (int propertyIndex = 0; propertyIndex < count; propertyIndex++)
                {
                    if (ShaderUtil.GetPropertyType(material.shader, propertyIndex) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                    string propertyName = ShaderUtil.GetPropertyName(material.shader, propertyIndex);
                    Texture texture = material.GetTexture(propertyName);
                    lines.Add("    TEX=" + propertyName + " -> " + (texture ? AssetDatabase.GetAssetPath(texture) : "NULL"));
                }
            }
        }
        File.WriteAllLines(Report, lines);
        Debug.Log("[Selected Material Inspector] " + Report);
    }

    private static string GetPath(Transform transform)
    {
        var names = new Stack<string>();
        while (transform != null) { names.Push(transform.name); transform = transform.parent; }
        return string.Join("/", names);
    }
}
