#if LOCAL_SCENE3_AUDIT
using System.IO;using UnityEditor;
// Passive diagnostic only: never starts or stops Play Mode or changes scenes/saves.
[InitializeOnLoad]public static class PlayModeStabilityObserver{
 static double began;static bool watching;
 static PlayModeStabilityObserver(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Tick;File.WriteAllText("D:/-11-scene3-audit/playmode-current.txt",EditorApplication.isPlaying?"Playing":"Editing");if(EditorApplication.isPlaying){began=EditorApplication.timeSinceStartup;watching=true;}}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode){began=EditorApplication.timeSinceStartup;watching=true;File.WriteAllText("D:/-11-scene3-audit/playmode-current.txt","Playing");}if(s==PlayModeStateChange.EnteredEditMode){File.WriteAllText("D:/-11-scene3-audit/playmode-current.txt","Editing");if(watching){File.WriteAllText("D:/-11-scene3-audit/playmode-stability.txt","Stopped after "+(EditorApplication.timeSinceStartup-began)+" seconds");watching=false;}}}
 static void Tick(){if(watching&&EditorApplication.isPlaying&&EditorApplication.timeSinceStartup-began>=10){watching=false;File.WriteAllText("D:/-11-scene3-audit/playmode-stability.txt","PASS: Play Mode remained active for at least 10 seconds; observer did not stop or start playback.");}}
}

#endif
