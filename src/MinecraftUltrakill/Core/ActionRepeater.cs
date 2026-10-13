using System;

namespace MinecraftUltrakill.Core
{
    // Requires a fresh press after switching/menu/selection changes. No burst catch-up on a slow frame.
    public sealed class ActionRepeater
    {
        bool armed;
        float next;
        public void Reset() { armed = false; next = 0; }
        public bool Take(float now, bool pressed, bool held, float interval)
        {
            if (!Rules.Finite(now) || !Rules.Finite(interval) || interval < .1f) throw new ArgumentOutOfRangeException();
            if (!pressed && !held) { armed = false; return false; }
            if (pressed) armed = true;
            if (!armed || now < next) return false;
            next = now + interval;
            return true;
        }
    }
}
