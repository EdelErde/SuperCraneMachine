namespace CraneMachine
{
    public interface IUpgrade
    {
        int CurrentCost { get; }
        bool MaxedOut { get; }
        string Label { get; }
        int TimesPurchased { get; }
        void Apply();
    }
}