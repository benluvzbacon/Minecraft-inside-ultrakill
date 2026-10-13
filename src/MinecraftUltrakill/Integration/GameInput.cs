using System;
using System.Reflection;
using UnityEngine;

namespace MinecraftUltrakill.Integration
{
    // Current game builds use the new Input System. LegacyInput is New Blood's real bridge,
    // not Unity's disabled legacy Input backend. All optional signatures are checked first.
    public sealed class GameInput
    {
        readonly PropertyInfo legacyCurrent, mouseCurrent;
        readonly MethodInfo getButton;
        public GameInput()
        {
            var legacy = Type.GetType("NewBlood.LegacyInput, NewBlood.LegacyInput", false);
            var mouse = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem", false);
            if (legacy != null)
            {
                legacyCurrent = legacy.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                getButton = legacy.GetMethod("GetButton", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(KeyCode) }, null);
            }
            if (mouse != null) mouseCurrent = mouse.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
        }
        object Button(KeyCode code)
        {
            var current = legacyCurrent == null ? null : legacyCurrent.GetValue(null, null);
            return current == null || getButton == null ? null : getButton.Invoke(current, new object[] { code });
        }
        public bool Down(KeyCode code)
        {
            var button = Button(code);
            if (button != null) return GameBridge.Flag(button, "wasPressedThisFrame");
            return Input.GetKeyDown(code);
        }
        public float Scroll()
        {
            var current = mouseCurrent == null ? null : mouseCurrent.GetValue(null, null);
            var scroll = GameBridge.Read(current, "scroll");
            if (scroll != null)
            {
                var read = scroll.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                if (read != null) { var value = read.Invoke(scroll, null); if (value is Vector2) return ((Vector2)value).y; }
            }
            return Input.mouseScrollDelta.y;
        }
        object Action(PlayerContext p, string name)
        {
            var manager = GameBridge.Read(p.Movement, "inman");
            var source = GameBridge.Read(manager, "InputSource");
            return GameBridge.Read(source, name);
        }
        public bool FireHeld(PlayerContext p)
        {
            var action = Action(p, "Fire1");
            if (action != null) return GameBridge.Flag(action, "IsPressed");
            var button = Button(KeyCode.Mouse0);
            return button != null ? GameBridge.Flag(button, "isPressed") : Input.GetMouseButton(0);
        }
        public bool FireDown(PlayerContext p, bool secondary = false)
        {
            var action = Action(p, secondary ? "Fire2" : "Fire1");
            if (action != null) return GameBridge.Flag(action, "WasPerformedThisFrame");
            return Down(secondary ? KeyCode.Mouse1 : KeyCode.Mouse0);
        }
    }
}
