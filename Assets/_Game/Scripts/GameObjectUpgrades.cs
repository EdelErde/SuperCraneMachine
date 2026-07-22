namespace CraneMachine
{
    public abstract class ActivateObjectUpgrade : Upgrade
    {
        protected abstract UnlockTarget Target { get; }
        protected override int MaxPurchases => 1;

        protected override void ApplyEffect()
            => SceneRef.SetActive(Target, true);
    }

    public class UnlockConveyorUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Conveyor Belt";
        protected override int BaseCost => 150;
        protected override UnlockTarget Target => UnlockTarget.Conveyor;
    }
    
    public class UnlockClawUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Claw";
        protected override int BaseCost => 25;
        protected override UnlockTarget Target => UnlockTarget.Claw;
    }
    
    public class UnlockAutoSeller : ActivateObjectUpgrade
    {
        protected override string Name => "AutoSeller";
        protected override int BaseCost => 500;
        protected override UnlockTarget Target => UnlockTarget.AutoSeller;
    }
}