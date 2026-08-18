using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Central SFX system — definition, triggering, AND playback all in one place.
    // Nothing else in the scene has any SFX-specific component. SfxManager finds every
    // relevant machine/service at startup and subscribes to ITS events directly (or
    // polls its state for things that aren't discrete events, like "is blowing").
    // Machines only expose plain gameplay events (OnIntake, OnSorted, OnItemEntered,
    // etc.) for their own reasons — SfxManager is just one more subscriber, same as
    // anything else that cares when a sorter processes an item.
    //
    // DEFINITION: every sound is one entry in 'sounds' below — id, clips, volume,
    // which screen(s) it's allowed to play on, concurrency settings, AND which trigger
    // fires it (see SfxTrigger). Configure a sound completely here; nothing to wire up
    // elsewhere.
    //
    // SCREENS: each sound has a ScreenMask. Play() checks it against
    // ScreenCameraRef.Current and silently does nothing if the active screen isn't
    // included.
    //
    // PLAYBACK: auto-sets-up, no manual pool wiring. Most sounds share one global pool
    // capped at maxGlobalVoices; a sound with useSeparateLimit checked instead draws
    // from its own named SfxCategory pool, capped independently. When the relevant
    // pool is at its cap, the new sound is simply dropped (not queued, not stolen).
    public class SfxManager : MonoBehaviour
    {
        [Tooltip("Every sound in the game. Add an entry, give it an id and a trigger, configure it here.")]
        [SerializeField] private List<SoundDef> sounds = new List<SoundDef>();

        [Tooltip("Max sounds playing at once from the shared global pool (anything without its own separate limit).")]
        [SerializeField] private int maxGlobalVoices = 12;

        [Tooltip("Parent transform for pooled voice GameObjects (auto-created if left empty).")]
        [SerializeField] private Transform poolParent;

        [Tooltip("Minimum seconds between rescans for newly created/destroyed machine instances. " +
                 "0 = only scan once at Start (fine for scenes where machines are placed at design " +
                 "time and not spawned/destroyed during play).")]
        [SerializeField] private float rescanInterval = 0f;

        private readonly Dictionary<string, SoundDef> _byId = new Dictionary<string, SoundDef>();

        private class Voice
        {
            public AudioSource source;
            public bool inUse;
        }

        private readonly List<Voice> _globalPool = new List<Voice>();
        private readonly Dictionary<SfxCategory, List<Voice>> _categoryPools = new Dictionary<SfxCategory, List<Voice>>();
        private readonly Dictionary<SfxCategory, int> _categoryLimits = new Dictionary<SfxCategory, int>();

        // ---- Discovery / subscription bookkeeping ----
        private readonly List<SortingMachine> _sorters = new List<SortingMachine>();
        private readonly List<LeafBlower> _blowers = new List<LeafBlower>();
        private readonly List<ConveyorBelt> _belts = new List<ConveyorBelt>();
        private readonly List<FuelHole> _fuelHoles = new List<FuelHole>();
        private readonly List<FuelFilter> _fuelFilters = new List<FuelFilter>();
        private readonly List<FuelFunnel> _fuelFunnels = new List<FuelFunnel>();
        private readonly List<SellHole> _sellHoles = new List<SellHole>();
        private readonly List<DestroyZone> _destroyZones = new List<DestroyZone>();

        // Per-instance polling state (things without a discrete "it happened" event).
        private readonly Dictionary<LeafBlower, bool> _wasBlowing = new Dictionary<LeafBlower, bool>();
        private readonly Dictionary<Item, bool> _wasDragging = new Dictionary<Item, bool>();
        private MagnetController.State _lastMagnetState;
        private int _lastPurchases;
        private float _nextRescan;

        private void Awake()
        {
            ServiceLocator.Sfx = this;

            if (poolParent == null)
            {
                var go = new GameObject("SfxVoicePool");
                go.transform.SetParent(transform, worldPositionStays: false);
                poolParent = go.transform;
            }

            RebuildLookup();
        }

        private void Start()
        {
            SubscribeSingletons();
            RefreshSubscriptions();
            Item.OnImpact += HandleItemImpact;

            if (ServiceLocator.Magnet != null) _lastMagnetState = ServiceLocator.Magnet.Current;
            if (ServiceLocator.UpgradeService != null) _lastPurchases = ServiceLocator.UpgradeService.TotalPurchases();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.Sfx == this) ServiceLocator.Sfx = null;

            UnsubscribeSingletons();
            UnsubscribeAllInstances();
            Item.OnImpact -= HandleItemImpact;
        }

        private void Update()
        {
            if (rescanInterval > 0f && Time.time >= _nextRescan)
            {
                _nextRescan = Time.time + rescanInterval;
                RefreshSubscriptions();
            }

            PollMagnet();
            PollBlowers();
            PollDraggedItems();
        }

        // ---------------------------------------------------------------------------
        // Singleton service events — subscribed once, these never come and go.
        // ---------------------------------------------------------------------------

        private void SubscribeSingletons()
        {
            if (ServiceLocator.ItemSpawner != null) ServiceLocator.ItemSpawner.OnSpawned += HandleSpawned;
            if (ServiceLocator.StatService != null) ServiceLocator.StatService.OnMoneyEarned += HandleMoneyEarned;
            if (ServiceLocator.UpgradeService != null) ServiceLocator.UpgradeService.OnUpgradesChanged += HandleUpgradesChanged;
            if (ServiceLocator.SellService != null) ServiceLocator.SellService.OnItemSold += HandleItemSold;
        }

        private void UnsubscribeSingletons()
        {
            if (ServiceLocator.ItemSpawner != null) ServiceLocator.ItemSpawner.OnSpawned -= HandleSpawned;
            if (ServiceLocator.StatService != null) ServiceLocator.StatService.OnMoneyEarned -= HandleMoneyEarned;
            if (ServiceLocator.UpgradeService != null) ServiceLocator.UpgradeService.OnUpgradesChanged -= HandleUpgradesChanged;
            if (ServiceLocator.SellService != null) ServiceLocator.SellService.OnItemSold -= HandleItemSold;
        }

        private void HandleSpawned() => Play(SfxTrigger.ItemSpawned);
        private void HandleMoneyEarned(int amount) => Play(SfxTrigger.MoneyEarned);
        private void HandleItemSold(int amount, Vector3 where) => Play(SfxTrigger.ItemSold);

        private void HandleUpgradesChanged()
        {
            // OnUpgradesChanged also fires on non-purchase refreshes (e.g. view startup) —
            // only treat it as a "buy" when the purchase COUNT actually goes up.
            int now = ServiceLocator.UpgradeService != null ? ServiceLocator.UpgradeService.TotalPurchases() : 0;
            if (now > _lastPurchases) Play(SfxTrigger.UpgradePurchased);
            _lastPurchases = now;
        }

        // ---------------------------------------------------------------------------
        // Multi-instance machines — discovered at runtime, subscribed per instance.
        // ---------------------------------------------------------------------------

        public void RefreshSubscriptions()
        {
            UnsubscribeAllInstances();

#if UNITY_2023_1_OR_NEWER
            _sorters.AddRange(FindObjectsByType<SortingMachine>(FindObjectsSortMode.None));
            _blowers.AddRange(FindObjectsByType<LeafBlower>(FindObjectsSortMode.None));
            _belts.AddRange(FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None));
            _fuelHoles.AddRange(FindObjectsByType<FuelHole>(FindObjectsSortMode.None));
            _fuelFilters.AddRange(FindObjectsByType<FuelFilter>(FindObjectsSortMode.None));
            _fuelFunnels.AddRange(FindObjectsByType<FuelFunnel>(FindObjectsSortMode.None));
            _sellHoles.AddRange(FindObjectsByType<SellHole>(FindObjectsSortMode.None));
            _destroyZones.AddRange(FindObjectsByType<DestroyZone>(FindObjectsSortMode.None));
#else
            _sorters.AddRange(FindObjectsOfType<SortingMachine>());
            _blowers.AddRange(FindObjectsOfType<LeafBlower>());
            _belts.AddRange(FindObjectsOfType<ConveyorBelt>());
            _fuelHoles.AddRange(FindObjectsOfType<FuelHole>());
            _fuelFilters.AddRange(FindObjectsOfType<FuelFilter>());
            _fuelFunnels.AddRange(FindObjectsOfType<FuelFunnel>());
            _sellHoles.AddRange(FindObjectsOfType<SellHole>());
            _destroyZones.AddRange(FindObjectsOfType<DestroyZone>());
#endif

            foreach (var m in _sorters) { m.OnIntake += HandleSortIntake; m.OnSorted += HandleSorted; }
            foreach (var m in _belts) m.OnItemEntered += HandleConveyorEntered;
            foreach (var m in _fuelHoles) { m.OnIntake += HandleFuelHoleIntake; m.OnEject += HandleFuelHoleEject; }
            foreach (var m in _fuelFilters) { m.OnIntake += HandleFuelFilterIntake; m.OnProduce += HandleFuelFilterProduce; }
            foreach (var m in _fuelFunnels) m.OnFunneled += HandleFunneled;
            foreach (var m in _sellHoles) m.OnItemEntered += HandleSellHoleEntered;
            foreach (var m in _destroyZones) m.OnItemDestroyed += HandleItemDestroyed;

            WireButtons();

            // LeafBlower has no discrete "blowing" event (it's a continuous state) —
            // seed polling bookkeeping so Update()'s poll doesn't fire a false trigger
            // on the very first frame after a rescan.
            _wasBlowing.Clear();
            foreach (var b in _blowers) _wasBlowing[b] = b.IsBlowing;
        }

        private void UnsubscribeAllInstances()
        {
            foreach (var m in _sorters) { m.OnIntake -= HandleSortIntake; m.OnSorted -= HandleSorted; }
            foreach (var m in _belts) m.OnItemEntered -= HandleConveyorEntered;
            foreach (var m in _fuelHoles) { m.OnIntake -= HandleFuelHoleIntake; m.OnEject -= HandleFuelHoleEject; }
            foreach (var m in _fuelFilters) { m.OnIntake -= HandleFuelFilterIntake; m.OnProduce -= HandleFuelFilterProduce; }
            foreach (var m in _fuelFunnels) m.OnFunneled -= HandleFunneled;
            foreach (var m in _sellHoles) m.OnItemEntered -= HandleSellHoleEntered;
            foreach (var m in _destroyZones) m.OnItemDestroyed -= HandleItemDestroyed;

            _sorters.Clear();
            _blowers.Clear();
            _belts.Clear();
            _fuelHoles.Clear();
            _fuelFilters.Clear();
            _fuelFunnels.Clear();
            _sellHoles.Clear();
            _destroyZones.Clear();
        }

        private void HandleSortIntake() => Play(SfxTrigger.SorterIntake);
        private void HandleSorted() => Play(SfxTrigger.SorterSorted);
        private void HandleConveyorEntered() => Play(SfxTrigger.ConveyorItemEntered);
        private void HandleFuelHoleIntake() => Play(SfxTrigger.FuelHoleIntake);
        private void HandleFuelHoleEject() => Play(SfxTrigger.FuelHoleEject);
        private void HandleFuelFilterIntake() => Play(SfxTrigger.FuelFilterIntake);
        private void HandleFuelFilterProduce() => Play(SfxTrigger.FuelFilterProduce);
        private void HandleFunneled() => Play(SfxTrigger.FuelFunneled);
        private void HandleSellHoleEntered() => Play(SfxTrigger.SellHoleEntered);
        private void HandleItemDestroyed() => Play(SfxTrigger.ItemDestroyed);
        private void HandleItemImpact(Item item, float speed) => Play(SfxTrigger.ItemImpact);

        // Every UI Button in the scene gets wired to play the UiClick trigger.
        // Re-wiring is idempotent (RemoveListener then AddListener) so repeated
        // rescans never stack duplicate calls on the same button.
        private void WireButtons()
        {
            var buttons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
            foreach (var b in buttons)
            {
                if (b == null || !b.gameObject.scene.IsValid()) continue;
                b.onClick.RemoveListener(HandleButtonClick);
                b.onClick.AddListener(HandleButtonClick);
            }
        }

        private void HandleButtonClick() => Play(SfxTrigger.UiClick);

        // ---------------------------------------------------------------------------
        // Polling — continuous/state-based triggers with no discrete event to hook.
        // ---------------------------------------------------------------------------

        private void PollMagnet()
        {
            if (ServiceLocator.Magnet == null) return;
            var s = ServiceLocator.Magnet.Current;
            if (s == _lastMagnetState) return;

            switch (s)
            {
                case MagnetController.State.Grabbing: Play(SfxTrigger.MagnetGrab); break;
                case MagnetController.State.Raising: Play(SfxTrigger.MagnetRaise); break;
                case MagnetController.State.Dropping: Play(SfxTrigger.MagnetDrop); break;
            }
            _lastMagnetState = s;
        }

        private void PollBlowers()
        {
            foreach (var blower in _blowers)
            {
                if (blower == null) continue;
                bool now = blower.IsBlowing;
                _wasBlowing.TryGetValue(blower, out bool was);
                if (now && !was) Play(SfxTrigger.LeafBlowerStart);
                _wasBlowing[blower] = now;
            }
        }

        // Item grab/release: polled across every currently-dragging-capable item.
        // Cheap enough (a HashSet-style dictionary walk) for the item counts this game
        // deals with; revisit if that ever changes.
        private void PollDraggedItems()
        {
#if UNITY_2023_1_OR_NEWER
            var items = FindObjectsByType<Item>(FindObjectsSortMode.None);
#else
            var items = FindObjectsOfType<Item>();
#endif
            foreach (var item in items)
            {
                bool now = item.IsDragging;
                _wasDragging.TryGetValue(item, out bool was);
                if (now && !was) Play(SfxTrigger.ItemGrabbed);
                if (!now && was) Play(SfxTrigger.ItemReleased);
                _wasDragging[item] = now;
            }
        }

        // ---------------------------------------------------------------------------
        // Playback
        // ---------------------------------------------------------------------------

        private void RebuildLookup()
        {
            _byId.Clear();
            for (int i = 0; i < sounds.Count; i++)
            {
                var def = sounds[i];
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                _byId[def.Id] = def;
            }
        }

        // Plays every sound entry configured with this trigger (usually one, but
        // nothing stops two sounds sharing a trigger — e.g. a layered impact sound).
        private void Play(SfxTrigger trigger)
        {
            for (int i = 0; i < sounds.Count; i++)
            {
                var def = sounds[i];
                if (def != null && def.Trigger == trigger) Play(def);
            }
        }

        private void Play(SoundDef def)
        {
            if (def == null || !def.HasClips) return;
            if (!def.AllowedOn(ScreenCameraRef.Current)) return;

            float now = Time.time;
            if (def.OnCooldown(now)) return;

            var pool = def.UseSeparateLimit ? GetCategoryPool(def.Category, def.MaxConcurrent) : _globalPool;
            int cap = def.UseSeparateLimit ? _categoryLimits[def.Category] : maxGlobalVoices;

            var voice = FindFreeVoice(pool);
            if (voice == null)
            {
                if (pool.Count >= cap) return; // pool full -> drop the sound, per design
                voice = CreateVoice(pool);
            }

            def.StartCooldown(now);
            PlayOn(voice, def);
        }

        private List<Voice> GetCategoryPool(SfxCategory category, int maxConcurrent)
        {
            if (!_categoryPools.TryGetValue(category, out var pool))
            {
                pool = new List<Voice>();
                _categoryPools[category] = pool;
                _categoryLimits[category] = Mathf.Max(1, maxConcurrent);
            }
            return pool;
        }

        private static Voice FindFreeVoice(List<Voice> pool)
        {
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].inUse && !pool[i].source.isPlaying)
                    return pool[i];
            return null;
        }

        private Voice CreateVoice(List<Voice> pool)
        {
            var go = new GameObject("SfxVoice");
            go.transform.SetParent(poolParent, worldPositionStays: false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;

            var voice = new Voice { source = source };
            pool.Add(voice);
            return voice;
        }

        private void PlayOn(Voice voice, SoundDef def)
        {
            var clip = def.PickClip();
            if (clip == null) return;

            voice.source.outputAudioMixerGroup = def.Output;
            voice.source.pitch = 1f + Random.Range(-def.PitchJitter, def.PitchJitter);
            voice.inUse = true;
            voice.source.PlayOneShot(clip, def.Volume);

            StartCoroutine(FreeAfter(voice, clip.length / Mathf.Max(0.01f, voice.source.pitch)));
        }

        private System.Collections.IEnumerator FreeAfter(Voice voice, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            voice.inUse = false;
        }
    }
}