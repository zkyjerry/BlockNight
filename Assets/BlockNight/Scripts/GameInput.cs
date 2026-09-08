using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace BlockNight {
/// <summary>Physical keyboard plus the Input System devices driven by unity-cli.</summary>
public static class GameInput {
 public static bool Down(KeyCode code){
#if ENABLE_LEGACY_INPUT_MANAGER
 if(Input.GetKeyDown(code))return true;
#endif
#if ENABLE_INPUT_SYSTEM
 var k=Keyboard.current;if(k==null)return false;
 switch(code){case KeyCode.W:return k.wKey.wasPressedThisFrame;case KeyCode.A:return k.aKey.wasPressedThisFrame;case KeyCode.S:return k.sKey.wasPressedThisFrame;case KeyCode.D:return k.dKey.wasPressedThisFrame;case KeyCode.Q:return k.qKey.wasPressedThisFrame;case KeyCode.M:return k.mKey.wasPressedThisFrame;case KeyCode.Space:return k.spaceKey.wasPressedThisFrame;case KeyCode.Return:return k.enterKey.wasPressedThisFrame;case KeyCode.Escape:return k.escapeKey.wasPressedThisFrame;case KeyCode.UpArrow:return k.upArrowKey.wasPressedThisFrame;case KeyCode.DownArrow:return k.downArrowKey.wasPressedThisFrame;case KeyCode.LeftArrow:return k.leftArrowKey.wasPressedThisFrame;case KeyCode.RightArrow:return k.rightArrowKey.wasPressedThisFrame;case KeyCode.Alpha1:return k.digit1Key.wasPressedThisFrame;case KeyCode.Alpha2:return k.digit2Key.wasPressedThisFrame;case KeyCode.Alpha3:return k.digit3Key.wasPressedThisFrame;}
#endif
 return false;
 }
}
}
