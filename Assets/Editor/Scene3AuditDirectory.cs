using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class Scene3AuditDirectory
{
    public const string Root = "Temp/Scene3Audit";

    static Scene3AuditDirectory()
    {
        Directory.CreateDirectory(Root);
    }
}
