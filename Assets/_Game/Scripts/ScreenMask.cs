using System;

namespace CraneMachine
{
    // Multi-select bitmask over ScreenId — Unity renders any [Flags] enum field as a
    // native multi-select dropdown, no custom drawer needed. Used by SoundDef.screens
    // so one sound can be scoped to one, several, or every screen.
    //
    // IMPORTANT: keep this in sync with ScreenId — each ScreenId needs a matching bit
    // here (see ScreenMaskUtil.From). Bits double each time (1, 2, 4, 8...) since it's
    // a flags enum; "Everything" is all bits ORed together as a convenience value.
    [Flags]
    public enum ScreenMask
    {
        None = 0,
        Screen1 = 1 << 0,
        Screen2 = 1 << 1,
        Screen3 = 1 << 2,
        Screen4 = 1 << 3,
        Screen5 = 1 << 4,
        Everything = ~0,
    }

    public static class ScreenMaskUtil
    {
        // Converts a single ScreenId (the "currently active screen") into its matching
        // ScreenMask bit, so it can be tested against a SoundDef's mask with HasFlag.
        public static ScreenMask From(ScreenId screen)
        {
            switch (screen)
            {
                case ScreenId.Screen1: return ScreenMask.Screen1;
                case ScreenId.Screen2: return ScreenMask.Screen2;
                case ScreenId.Screen3: return ScreenMask.Screen3;
                case ScreenId.Screen4: return ScreenMask.Screen4;
                case ScreenId.Screen5: return ScreenMask.Screen5;
                default: return ScreenMask.None;
            }
        }
    }
}