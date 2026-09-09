using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace BlockNight.Editor {
 public static class FontAtlasRepair {
  [MenuItem("Block Night/TMP/Repair Demo Glyph Atlas")]
  public static void Repair(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");const string path="Assets/BlockNight/Fonts/FZXS12 TMP.asset";string backup="Docs/Backups/FZXS12-before-v6.asset.backup";if(!File.Exists(backup))File.Copy(path,backup);
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);var chars=new System.Collections.Generic.HashSet<char>(font.characterTable.Where(x=>x.unicode<=65535).Select(x=>(char)x.unicode));
   foreach(var file in Directory.GetFiles("Assets/BlockNight/Scripts","*.cs"))foreach(char c in File.ReadAllText(file))if(c>=' ')chars.Add(c);
   foreach(string name in new[]{"MainMenu","BlockNight","GameOver"}){var scene=EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/"+name+".unity");foreach(var text in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)))foreach(char c in text.text)if(c>=' ')chars.Add(c);}
   font.ClearFontAssetData();font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.isMultiAtlasTexturesEnabled=true;string missing;font.TryAddCharacters(new string(chars.OrderBy(x=>x).ToArray()),out missing,true);
   foreach(var texture in font.atlasTextures){if(!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);EditorUtility.SetDirty(texture);}font.material.mainTexture=font.atlasTexture;EditorUtility.SetDirty(font.material);EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
   foreach(string name in new[]{"MainMenu","BlockNight","GameOver"}){var scene=EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/"+name+".unity");foreach(var label in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true))){label.fontSharedMaterial=font.material;label.maxVisibleCharacters=int.MaxValue;label.ForceMeshUpdate(true);EditorUtility.SetDirty(label);}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
   AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");File.WriteAllText("Docs/QA/font-atlas-v6.txt","Glyphs="+font.characterTable.Count+"; atlases="+font.atlasTextures.Length+"; missing="+missing);
  }
 }
}
