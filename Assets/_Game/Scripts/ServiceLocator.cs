namespace CraneMachine
{
    public static class ServiceLocator
    {
        public static StatService StatService;
        public static UpgradeService UpgradeService;
        public static SellService SellService;
        public static FuelService FuelService;
        public static FuelConsumerRegistry FuelConsumers;
        public static PickupFilterService PickupFilter;
        public static ScreenUnlockService ScreenUnlocks;
        public static ItemSpawner ItemSpawner;
        public static MagnetController Magnet;
        public static CursorManager CursorManager;
        public static ParticleBurstPool Particles;
        public static AudioMixerController Audio;
    }
}