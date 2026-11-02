using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class P5RepairNoteSetup{
 static P5RepairNoteSetup(){EditorApplication.delayCall+=Apply;}
 [MenuItem("Tools/JIQI/Refresh P5 Repair Note")]
 public static void Apply(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}P5RepairNoteTexture.ApplyScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
}
