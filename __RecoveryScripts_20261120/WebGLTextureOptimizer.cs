using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WebGLTextureOptimizer
{
    const string MarkerPath="Library/WebGLTextureOptimization-v2.done";
    const int WebGLMaxSize=512;

    static WebGLTextureOptimizer(){EditorApplication.delayCall+=RunOnceForWebGL;}

    [MenuItem("Tools/WebGL/Compress Project Textures")]
    public static void CompressProjectTextures(){Optimize(true);}

    static void RunOnceForWebGL()
    {
        if(EditorUserBuildSettings.activeBuildTarget==BuildTarget.WebGL&&!File.Exists(MarkerPath))Optimize(false);
    }

    static void Optimize(bool force)
    {
        if(!force&&File.Exists(MarkerPath))return;
        string[] guids=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets"});
        int changed=0;
        try
        {
            for(int i=0;i<guids.Length;i++)
            {
                string path=AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)continue;
                EditorUtility.DisplayProgressBar("Compressing textures for WebGL",path,guids.Length==0?1f:(float)i/guids.Length);
                var settings=importer.GetPlatformTextureSettings("WebGL");
                bool needsChange=!settings.overridden||settings.maxTextureSize>WebGLMaxSize
                    ||settings.format!=TextureImporterFormat.Automatic
                    ||settings.textureCompression!=TextureImporterCompression.Compressed
                    ||!settings.crunchedCompression||settings.compressionQuality!=80;
                if(!needsChange)continue;
                settings.name="WebGL";settings.overridden=true;settings.maxTextureSize=WebGLMaxSize;
                settings.resizeAlgorithm=TextureResizeAlgorithm.Mitchell;
                settings.format=TextureImporterFormat.Automatic;
                settings.textureCompression=TextureImporterCompression.Compressed;
                settings.crunchedCompression=true;settings.compressionQuality=80;
                importer.SetPlatformTextureSettings(settings);
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                changed++;
            }
            Directory.CreateDirectory("Library");
            File.WriteAllText(MarkerPath,DateTime.Now.ToString("O")+"\nchanged="+changed);
            AssetDatabase.SaveAssets();
            Debug.Log("[WebGL Texture Optimizer] Finished. Updated "+changed+" textures. WebGL max size="+WebGLMaxSize+", compressed=true, crunch=true. Gameplay and non-WebGL settings were not changed.");
        }
        catch(Exception exception){Debug.LogException(exception);}
        finally{EditorUtility.ClearProgressBar();}
    }
}
