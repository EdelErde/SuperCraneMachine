namespace CraneMachine
{
    public enum GameStat
    {
        MoneyMultiplier,   // multiplies all sell income
        MagnetSweepSpeed,
        MagnetVerticalSpeed,
        MagnetGrabCapacity,    // how many items the magnet can carry at once
        MagnetRange,           // side length (width) of the square magnet pickup zone (world units)
        SpawnInterval,     // lower = faster rain
        MaxLiveItems,
        HandStrength,      // higher = drag heavier/faster without slipping
        DragCount,         // how many items one drag can hold
        DragRadius,        // pickup radius for multi-grab
        AutoMagnet,        // >0 = auto drop unlocked
        AutoMagnetInterval,// average seconds between automatic drops
        ConveyorSpeed,     // belt units per second
        ConveyorGrip,      // how fast items lock onto the belt
    }

    public enum ItemStat
    {
        SellValue,
        Mass,
        Unlocked,   // >0 = appears in the spawn pool
        Weight,     // spawn weight; higher = more common
    }
}