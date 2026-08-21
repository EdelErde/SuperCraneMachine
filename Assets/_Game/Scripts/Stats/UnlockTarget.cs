namespace CraneMachine
{
    public enum UnlockTarget
    {
        Conveyor,
        Magnet,
        ResourceHole,
        LeafBlower,
        SortingMachine,
        SortingMachineExitC,   // per-machine: unlocks the third exit on the sorter(s) that opt in
        DroneFab,
        WinScreen,
    }
}