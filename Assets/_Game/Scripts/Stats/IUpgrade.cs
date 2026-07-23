namespace CraneMachine
{
    public interface IUpgrade
    {
        int CurrentCost { get; }
        bool MaxedOut { get; }
        string Label { get; }
        string DisplayName { get; }
        string IconPath { get; }
        int TimesPurchased { get; }
        void Apply();
    }
}