using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlockNight
{
    /// <summary>Edge-triggered keyboard input; supports physical and unity-cli virtual keyboards.</summary>
    public static class GameInput
    {
        static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> pressed = new HashSet<KeyCode>();
#if ENABLE_INPUT_SYSTEM
        static readonly KeyCode[] Codes = {KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.J,KeyCode.K,KeyCode.L,KeyCode.M,KeyCode.Escape,KeyCode.Return,KeyCode.UpArrow,KeyCode.DownArrow,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3};
        static readonly Key[] Keys = {Key.W,Key.A,Key.S,Key.D,Key.J,Key.K,Key.L,Key.M,Key.Escape,Key.Enter,Key.UpArrow,Key.DownArrow,Key.LeftArrow,Key.RightArrow,Key.Digit1,Key.Digit2,Key.Digit3};
#endif
        public static void Poll()
        {
            pressed.Clear();
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) { held.Clear(); return; }
            for (int i = 0; i < Codes.Length; i++)
            {
                bool down = keyboard[Keys[i]].isPressed;
                if (down && !held.Contains(Codes[i])) pressed.Add(Codes[i]);
                if (down) held.Add(Codes[i]); else held.Remove(Codes[i]);
            }
#endif
        }
        public static bool Down(KeyCode code)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(code)) return true;
#endif
            return pressed.Contains(code);
        }
    }
}
