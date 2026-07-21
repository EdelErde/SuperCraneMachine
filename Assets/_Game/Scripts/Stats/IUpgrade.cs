namespace CraneMachine
{
    public interface IUpgrade
    {
        int CurrentCost { get; }
        bool MaxedOut { get; }
        string Label { get; }
        void Apply();
    }
}