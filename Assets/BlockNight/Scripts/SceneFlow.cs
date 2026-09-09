using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockNight
{
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu", Gameplay = "BlockNight", GameOver = "GameOver";
        public static bool NextRunTutorial;
        public static int Score, Kills, BestChain;
        public static float Elapsed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { NextRunTutorial = false; Score = Kills = BestChain = 0; Elapsed = 0; }
        public static void StartRun(bool tutorial)
        {
            NextRunTutorial = tutorial;
            SceneManager.LoadScene(Gameplay);
        }
        public static void Finish(CombatModel model)
        {
            Score = model.score; Kills = model.kills; BestChain = model.bestChain; Elapsed = model.elapsed;
            SceneManager.LoadScene(GameOver);
        }
        public static void ToMenu() => SceneManager.LoadScene(MainMenu);
    }
}
