namespace CraneMachine
{
    // Identifies a screen/window that can be locked until an unlock condition is met.
    // Mirrors UnlockTarget's approach (fixed enum, type-safe). Extend this list as new
    // screens are added to the game (see the mockup's Window 1/2/3/4 + per-screen
    // breakdown for naming).
    public enum ScreenId
    {
        Screen1,
        Screen2,
        Screen3,
        Screen4,
        Screen5,
    }
}