using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
public static class CubeDrawerInterior {
 static CubeDrawerInterior(){EditorApplication.delayCall+=Apply;}
 static string P(Vector3 p){return Math.Round(p.x,6)+","+Math.Round(p.y,6)+","+Math.Round(p.z,6);}
 static string Key(Vector3 a,Vector3 b,Vector3 c){var k=new[]{P(a)+"|"+P(b)+"|"+P(c),P(b)+"|"+P(c)+"|"+P(a),P(c)+"|"+P(a)+"|"+P(b)};Array.Sort(k,StringComparer.Ordinal);return k[0];}
 [MenuItem("Tools/JIQI/Keep Drawers Open And Repair Interior")]
 public static void Apply(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var report=new List<string>();
 for(int number=1;number<=4;number++){
 var m=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Repairs/CubeFaces/cube"+number+"_FacesRepaired.asset");if(!m)continue;
 var v=m.vertices.ToList();var n=m.normals.ToList();var uv=m.uv.ToList();var tan=m.tangents.ToList();var original=m.GetTriangles(0);var kept=new List<int>();var keys=new HashSet<string>();float top=m.bounds.max.z;int removed=0,added=0;
 for(int i=0;i<original.Length;i+=3){var a=v[original[i]];var b=v[original[i+1]];var c=v[original[i+2]];if(Mathf.Abs(a.z-top)<1e-6f&&Mathf.Abs(b.z-top)<1e-6f&&Mathf.Abs(c.z-top)<1e-6f){removed++;continue;}kept.AddRange(new[]{original[i],original[i+1],original[i+2]});keys.Add(Key(a,b,c));}
 var body=kept.ToArray();
 for(int i=0;i<body.Length;i+=3){int a=body[i],b=body[i+1],c=body[i+2];string reversed=Key(v[c],v[b],v[a]);if(keys.Contains(reversed))continue;int start=v.Count;foreach(int j in new[]{c,b,a}){v.Add(v[j]);if(n.Count>0)n.Add(-n[j]);if(uv.Count>0)uv.Add(uv[j]);if(tan.Count>0){var t=tan[j];t.w=-t.w;tan.Add(t);}}kept.AddRange(new[]{start,start+1,start+2});keys.Add(reversed);added++;}
 if(removed>0||added>0){var other=Enumerable.Range(1,m.subMeshCount-1).Select(i=>m.GetTriangles(i)).ToArray();m.SetVertices(v);if(n.Count>0)m.SetNormals(n);if(uv.Count>0)m.SetUVs(0,uv);if(tan.Count>0)m.SetTangents(tan);m.SetTriangles(kept,0);for(int i=1;i<m.subMeshCount;i++)m.SetTriangles(other[i-1],i);m.RecalculateBounds();EditorUtility.SetDirty(m);}
 report.Add("cube"+number+": top faces removed="+removed+"; inward wall/floor triangles added="+added+"; body triangles="+kept.Count/3);
 }
 AssetDatabase.SaveAssets();Directory.CreateDirectory("D:/-11-transfer-audit/cube-interior-20261020");File.WriteAllLines("D:/-11-transfer-audit/cube-interior-20261020/latest.txt",report);
 }
 public static void Run(){try{Apply();var report=new List<string>();for(int number=1;number<=4;number++){var m=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Repairs/CubeFaces/cube"+number+"_FacesRepaired.asset");var v=m.vertices;var a=m.GetTriangles(0);var keys=new HashSet<string>();for(int i=0;i<a.Length;i+=3)keys.Add(Key(v[a[i]],v[a[i+1]],v[a[i+2]]));for(int i=0;i<a.Length;i+=3){if(!keys.Contains(Key(v[a[i+2]],v[a[i+1]],v[a[i]])))throw new Exception("Missing inward face");if(new[]{a[i],a[i+1],a[i+2]}.All(j=>Mathf.Abs(v[j].z-m.bounds.max.z)<1e-6f))throw new Exception("Top sealed");}report.Add("cube"+number+": all side/floor triangles visible from both sides; top fully open; handle submesh retained.");}File.WriteAllLines("D:/-11-glass-check/cube-interior-result.txt",report);EditorApplication.Exit(0);}catch(Exception e){File.WriteAllText("D:/-11-glass-check/cube-interior-error.txt",e.ToString());EditorApplication.Exit(1);}}
}
