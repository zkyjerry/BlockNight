using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace BlockNight.Editor {
 public static class WaveRadiusChecks {
  static void Check(bool ok,string message){if(!ok)throw new Exception("WAVE RADIUS FAIL: "+message);}
  [MenuItem("Block Night/Apply Configurable Wave")]
  public static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");EditorSceneManager.SaveOpenScenes();var path="Assets/BlockNight/Scenes/BlockNight.unity";var backup="Docs/Backups/BlockNight-before-wave-radius.unity.backup";if(!File.Exists(backup))File.Copy(path,backup);var scene=EditorSceneManager.OpenScene(path);var f=UnityEngine.Object.FindObjectOfType<CombatFeedback>();
   if(!f.waveDissolve){var go=UnityEngine.Object.Instantiate(f.arena.shards.gameObject);go.name="UPGRADE WAVE — dissolve particles";f.waveDissolve=go.GetComponent<ParticleSystem>();}
   var ps=f.waveDissolve;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=1536;var emission=ps.emission;emission.enabled=false;var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;ps.GetComponent<ParticleSystemRenderer>().sortingOrder=25;
   EditorUtility.SetDirty(f);EditorUtility.SetDirty(f.settings);EditorUtility.SetDirty(ps);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");
  }
  [MenuItem("Block Night/QA/Run Wave Radius Checks")]
  public static void Run(){
   Check(EditorApplication.isPlaying,"requires combat Play Mode");var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.enabled=false;d.routeScenes=false;d.Begin(false);var m=d.model;var f=d.feedback;m.tutorial=true;
   m.cell=m.target=new Vector2Int(3,3);m.player=CombatModel.Center(m.cell);m.Spawn(0,CombatModel.Center(new Vector2Int(4,3)));m.Spawn(0,CombatModel.Center(new Vector2Int(5,3)));m.Spawn(0,CombatModel.Center(new Vector2Int(5,4)));m.Spawn(0,CombatModel.Center(new Vector2Int(7,7)));
   m.reward=30;m.StartUpgradeWave(.5f,2);int emitted=f.waveDissolveCount;for(int i=0;i<51;i++)m.Step(.01f);d.arena.Render(m);f.Render(m,0);
   Check(m.kills==2&&m.score==225&&Mathf.Abs(m.reward-27.6f)<.001f,"only inside and boundary enemies score: kills="+m.kills+" score="+m.score+" reward="+m.reward+" radius="+m.upgradeWaveRadius+" max="+m.upgradeWaveMaxRadius+" count="+m.foes.Count(x=>x.active));Check(m.foes.Count(x=>x.active)==2,"outside radius survives");Check(m.upgradeWaveRadius==2&&!m.upgradeWaveActive&&!f.upgradeRing.enabled,"stops exactly at configured radius");Check(f.waveDissolveCount==emitted+1&&f.waveDissolve.particleCount==128,"one endpoint particle burst");
   var particles=new ParticleSystem.Particle[1536];int count=f.waveDissolve.GetParticles(particles);for(int i=0;i<count;i++)Check(Mathf.Abs(Vector2.Distance(particles[i].position,d.arena.World(m.upgradeWaveOrigin))-2*d.arena.cellSize)<.002f,"particles originate at final circumference");
   for(int i=0;i<30;i++)m.Step(.01f);Check(f.waveDissolveCount==emitted+1&&m.kills==2,"no repeated dissolve or outside kills");
   m.StartUpgradeWave(.5f,3);m.Step(.1f);var saved=m.Capture();m.Step(.1f);m.Restore(saved);Check(m.upgradeWaveMaxRadius==3&&m.upgradeWaveRadius==saved.upgradeWaveRadius,"rewind retains configured radius");d.Rewind();
   d.Begin(false);m=d.model;m.tutorial=true;m.StartUpgradeWave(.5f,0);m.Step(1);Check(m.upgradeWaveMaxRadius==.1f&&m.upgradeWaveRadius==.1f,"invalid radius clamped");
   File.WriteAllText("Docs/QA/wave-radius-checks.txt","PASS: radius 2 kills inside/boundary only, outside survives; 225 score / 2.4 seconds; exact endpoint; one 128-particle circumference burst; no repeats; rewind radius snapshot; invalid radius clamp.\n"+DateTime.Now.ToString("s"));d.Begin(false);
  }
  [MenuItem("Block Night/QA/Wave Endpoint Fixture")]
  public static void Endpoint(){FeedbackChecks.Combat();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.model.tutorial=true;d.model.StartUpgradeWave(d.feedback.settings.shockwaveSeconds,d.feedback.settings.shockwaveRadius);d.model.Step(d.feedback.settings.shockwaveSeconds+.01f);d.arena.Render(d.model);d.feedback.Render(d.model,0);d.hud.Render(d,true);d.feedback.waveDissolve.Pause();d.feedback.waveDissolve.Simulate(.08f,true,false);d.feedback.waveDissolve.Pause();}
 }
}
