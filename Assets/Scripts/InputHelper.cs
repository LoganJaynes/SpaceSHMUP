using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Works whether the project uses the old Input Manager, the new Input System, or both.
/// Arrow keys / WASD to move, Space to fire, R to restart.
/// </summary>
public static class InputHelper
{
#if ENABLE_LEGACY_INPUT_MANAGER
    public static float Horizontal => Input.GetAxis("Horizontal");
    public static float Vertical => Input.GetAxis("Vertical");
    public static bool FireHeld => Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");
    public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
#elif ENABLE_INPUT_SYSTEM
    public static float Horizontal
    {
        get
        {
            Keyboard k = Keyboard.current; if (k == null) return 0;
            float x = 0;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1;
            return x;
        }
    }
    public static float Vertical
    {
        get
        {
            Keyboard k = Keyboard.current; if (k == null) return 0;
            float y = 0;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1;
            if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1;
            return y;
        }
    }
    public static bool FireHeld => Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    public static bool RestartPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
    public static float Horizontal => 0;
    public static float Vertical => 0;
    public static bool FireHeld => false;
    public static bool RestartPressed => false;
#endif
}
