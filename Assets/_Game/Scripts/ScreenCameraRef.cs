using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace CraneMachine
{
    // Tags a CinemachineCamera as belonging to a screen. Place next to (or on) the
    // CinemachineCamera for each screen. Switching screens = raising that screen's
    // camera priority above every other registered screen camera, so the
    // CinemachineBrain on Main Camera blends to it. Mirrors ScreenRef's registry
    // pattern (static Dictionary<ScreenId, List<T>>).
    public class ScreenCameraRef : MonoBehaviour
    {
        [SerializeField] private ScreenId screen;
        [SerializeField] private CinemachineCamera cam;

        [Tooltip("Priority given to the active screen's camera.")]
        [SerializeField] private int activePriority = 10;
        [Tooltip("Priority given to every other screen's camera.")]
        [SerializeField] private int inactivePriority = 0;

        private static readonly Dictionary<ScreenId, List<ScreenCameraRef>> _refs
            = new Dictionary<ScreenId, List<ScreenCameraRef>>();

        // Tracks which screen is currently live, for anything that needs to react to
        // screen switches (see SfxManager's screen-scoped sounds). Defaults to the
        // first ScreenId (Screen1) until the first Activate() call.
        public static ScreenId Current { get; private set; }
        public static event System.Action<ScreenId> OnActivated;

        public ScreenId Screen => screen;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<CinemachineCamera>();
            Register(screen, this);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(screen, out var list))
            {
                list.Remove(this);
                if (list.Count == 0) _refs.Remove(screen);
            }
        }

        private static void Register(ScreenId screen, ScreenCameraRef r)
        {
            if (!_refs.TryGetValue(screen, out var list))
            {
                list = new List<ScreenCameraRef>();
                _refs[screen] = list;
            }
            if (!list.Contains(r)) list.Add(r);
        }

        // Raise 'screen's camera(s) to activePriority, drop every other registered
        // screen's camera(s) to inactivePriority. CinemachineBrain does the blending.
        public static void Activate(ScreenId screen)
        {
            Current = screen;
            OnActivated?.Invoke(screen);

            foreach (var kvp in _refs)
            {
                bool isTarget = kvp.Key == screen;
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var r = list[i];
                    if (r == null || r.cam == null) continue;
                    r.cam.Priority.Value = isTarget ? r.activePriority : r.inactivePriority;
                }
            }
        }
    }
}