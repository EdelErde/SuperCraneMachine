namespace CraneMachine
{
    // Named limit groups a SoundDef can opt into (see SoundDef.useSeparateLimit).
    // Add more here as new categories are needed — shows up as a dropdown on any
    // SoundDef field, no string matching involved.
    public enum SfxCategory
    {
        ItemDrop,
        ItemImpact,
    }
}