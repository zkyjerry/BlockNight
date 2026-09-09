namespace BlockNight.Editor
{
    // Compatibility entry point. Never recreate the user's hand-edited UI.
    public static class DemoBuilder
    {
        [UnityEditor.MenuItem("Block Night/Build Demo")]
        public static void Build() => GridUpgrade.Upgrade();
    }
}
