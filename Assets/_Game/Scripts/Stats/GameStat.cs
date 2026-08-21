namespace CraneMachine
{
    public enum GameStat
    {
        MoneyMultiplier,   // multiplies all sell income
        MagnetSweepSpeed,
        MagnetVerticalSpeed,
        MagnetGrabCapacity,    // how many items the magnet can carry at once
        MagnetRange,           // side length (width) of the square magnet pickup zone (world units)
        MagnetDepth,           // how far DOWN the magnet pickup zone reaches (world units)
        SpawnInterval,     // lower = faster rain
        MaxLiveItems,
        HandStrength,      // higher = drag heavier/faster without slipping
        DragCount,         // how many items one drag can hold
        DragRadius,        // pickup radius for multi-grab
        AutoMagnet,        // >0 = auto drop unlocked
        AutoMagnetInterval,// average seconds between automatic drops
        ConveyorSpeed,     // belt units per second
        ConveyorGrip,      // how fast items lock onto the belt

        // Fuel economy
        Fuel,              // current global fuel pool (uncapped)
        FuelPerEgg,        // fuel produced per egg converted
        FuelConvertRate,   // eggs converted per second by the converter

        // Leaf blower
        BlowPower,             // force strength applied to items in the blow zone
        BlowFuelEfficiency,    // multiplier on blower fuel drain (higher = uses less)

        // Sorting machine
        SortFuelEfficiency,    // multiplier on sorter fuel drain (higher = uses less)
        SortCapacity,          // how many items the sorter can buffer/process at once

        // Fuel filter (item -> Fuel item)
        FuelFilterProcessTime, // seconds a queued item takes to become a Fuel item (lower = faster)
        FuelFilterCapacity,    // how many items the filter can buffer/process at once

        // Drone fab / drones
        DroneProductionTime, // seconds to build one drone (lower = faster; DroneFab floors it at 0.1)
        DroneCharges,        // deliveries a fresh drone makes before it dies
        DroneSpeed,          // empty-travel speed multiplier (1 = base)
        DroneCarrySpeed,     // carrying speed multiplier (usually < DroneSpeed)
        DroneGrip,           // reserved: how hard a drone resists having items stolen (unused for now)
    }

    public enum ItemStat
    {
        SellValue,
        Mass,
        Unlocked,   // >0 = appears in the spawn pool
        Weight,     // spawn weight; higher = more common
        DroneCarry, // >0 = drones are allowed to pick this type up and carry it. Gated by
                    // upgrades so drone hauling unlocks one item type at a time (Fuel first).
    }
}