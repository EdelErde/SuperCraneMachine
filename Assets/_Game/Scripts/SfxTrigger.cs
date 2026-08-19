namespace CraneMachine
{
    // Every moment SfxManager can fire a sound for. A SoundDef entry picks one of
    // these via a plain dropdown — no listener components anywhere else needed, since
    // SfxManager itself is what raises these (by subscribing to machine/service events
    // or polling state — see SfxManager for how each one fires).
    public enum SfxTrigger
    {
        ItemSpawned,
        ItemGrabbed,
        ItemReleased,
        ItemImpact,
        ItemDestroyed,
        ItemSold,

        MoneyEarned,
        UpgradePurchased,

        MagnetGrab,
        MagnetRaise,
        MagnetDrop,

        LeafBlowerStart,

        SorterIntake,
        SorterSorted,

        ConveyorItemEntered,

        SellHoleEntered,

        FuelHoleIntake,
        FuelHoleEject,
        FuelFilterIntake,
        FuelFilterProduce,
        FuelFunneled,

        UiClick,
    }
}