using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BlockNight.Editor
{
    public static class TMPMigration
    {
        const string MapPath="Docs/Backups/tmp-bindings.json";
        static readonly string[] Scenes={"MainMenu","BlockNight","GameOver"};
        [Serializable] class Link {public string scene, owner, property, target;}
        [Serializable] class Links {public List<Link> items=new List<Link>();}
        static string PathOf(Transform t) {string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
        static GameObject Find(UnityEngine.SceneManagement.Scene s,string path) {var parts=path.Split('/');var root=s.GetRootGameObjects().First(x=>x.name==parts[0]);return parts.Length==1?root:root.transform.Find(string.Join("/",parts.Skip(1))).gameObject;}
        static string Geometry(RectTransform r)=>JsonUtility.ToJson(new RectState(r));
        [Serializable] class RectState {
            public Vector2 min,max,pivot,position,size; public Vector3 scale;public Quaternion rotation;
            public RectState(RectTransform r){min=r.anchorMin;max=r.anchorMax;pivot=r.pivot;position=r.anchoredPosition;size=r.sizeDelta;scale=r.localScale;rotation=r.localRotation;}
            public void Restore(RectTransform r){r.anchorMin=min;r.anchorMax=max;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=scale;r.localRotation=rotation;}
        }
        [MenuItem("Block Night/TMP/1 Prepare Migration")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new Exception("先退出运行模式");
            var links=new Links();Directory.CreateDirectory("Docs/Backups");
            foreach(string name in Scenes){string path="Assets/BlockNight/Scenes/"+name+".unity";var scene=EditorSceneManager.OpenScene(path);EditorSceneManager.SaveScene(scene);File.Copy(path,"Docs/Backups/"+name+"-before-TMP.unity.backup",true);
                foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
                    if(!(c is GameHUD)&&!(c is MenuController))continue;
                    var so=new SerializedObject(c);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue is Text text)
                        links.items.Add(new Link{scene=name,owner=PathOf(c.transform),property=p.propertyPath,target=PathOf(text.transform)});
                }
            }
            File.WriteAllText(MapPath,JsonUtility.ToJson(links,true));
            EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");
            AssetDatabase.ImportPackage("Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage",false);
            Debug.Log("TMP binding snapshot saved: "+links.items.Count);
        }
        [MenuItem("Block Night/TMP/2 Convert Scenes")]
        public static void Convert()
        {
            if(EditorApplication.isPlaying)throw new Exception("先退出运行模式");
            var links=JsonUtility.FromJson<Links>(File.ReadAllText(MapPath));
            const string assetPath="Assets/BlockNight/Fonts/FZXS12 TMP.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if(!font){font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/BlockNight/Fonts/FZXIANGSU12.TTF"),72,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="FZXS12 TMP";
                AssetDatabase.CreateAsset(font,assetPath);font.material.name="FZXS12 SDF Material";AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);}
            int converted=0, rebound=0;var report=new System.Text.StringBuilder();
            foreach(string name in Scenes){var scene=EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/"+name+".unity");
                font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                var roots=scene.GetRootGameObjects();var before=roots.SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).ToDictionary(r=>r.GetInstanceID(),Geometry);
                foreach(var old in roots.SelectMany(r=>r.GetComponentsInChildren<Text>(true)).ToArray()){
                    var go=old.gameObject;var state=new RectState(old.rectTransform);string text=old.text;var color=old.color;int size=old.fontSize;bool raycast=old.raycastTarget,rich=old.supportRichText,enabled=old.enabled,best=old.resizeTextForBestFit;int min=old.resizeTextMinSize,max=old.resizeTextMaxSize;var align=old.alignment;var style=old.fontStyle;bool wrap=old.horizontalOverflow==HorizontalWrapMode.Wrap;float line=old.lineSpacing;
                    UnityEngine.Object.DestroyImmediate(old);var tmp=go.AddComponent<TextMeshProUGUI>();tmp.font=font;tmp.text=text;tmp.color=color;tmp.fontSize=size;tmp.raycastTarget=raycast;tmp.richText=rich;tmp.enabled=enabled;tmp.enableAutoSizing=best;tmp.fontSizeMin=min;tmp.fontSizeMax=max;tmp.enableWordWrapping=wrap;tmp.overflowMode=TextOverflowModes.Overflow;tmp.margin=Vector4.zero;tmp.lineSpacing=(line-1)*100;
                    tmp.fontStyle=style==FontStyle.Bold?FontStyles.Bold:style==FontStyle.Italic?FontStyles.Italic:style==FontStyle.BoldAndItalic?FontStyles.Bold|FontStyles.Italic:FontStyles.Normal;
                    var alignments=new[]{TextAlignmentOptions.TopLeft,TextAlignmentOptions.Top,TextAlignmentOptions.TopRight,TextAlignmentOptions.Left,TextAlignmentOptions.Center,TextAlignmentOptions.Right,TextAlignmentOptions.BottomLeft,TextAlignmentOptions.Bottom,TextAlignmentOptions.BottomRight};tmp.alignment=alignments[(int)align];state.Restore(tmp.rectTransform);converted++;
                }
                foreach(var label in roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true))) { label.font=font;label.fontSharedMaterial=font.material;label.lineSpacing=label.fontSize<=26?12:0;EditorUtility.SetDirty(label); }
                foreach(var link in links.items.Where(x=>x.scene==name)){
                    var owner=Find(scene,link.owner);var component=(Component)owner.GetComponent<GameHUD>()??owner.GetComponent<MenuController>();var so=new SerializedObject(component);var p=so.FindProperty(link.property);if(p==null)throw new Exception("Missing field "+link.property);p.objectReferenceValue=link.property.StartsWith("cardTexts.Array.data[") ? owner.GetComponent<GameHUD>().cards[int.Parse(link.property.Split('[')[1].TrimEnd(']'))].GetComponentInChildren<TMP_Text>(true) : Find(scene,link.target).GetComponent<TMP_Text>();so.ApplyModifiedPropertiesWithoutUndo();rebound++;
                }
                int changed=roots.SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).Count(r=>before[r.GetInstanceID()]!=Geometry(r));if(changed!=0)throw new Exception("TMP layout changed: "+changed);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.AppendLine(name+": TMP="+roots.Sum(r=>r.GetComponentsInChildren<TMP_Text>(true).Length)+", Text="+roots.Sum(r=>r.GetComponentsInChildren<Text>(true).Length)+", geometry changes="+changed);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");
            File.WriteAllText("Docs/QA/tmp-migration.txt",report+"Converted="+converted+", rebound="+rebound+"\nFont="+assetPath);Debug.Log("TMP migration PASS: "+converted+" text components, "+rebound+" bindings");
        }
    }
}
