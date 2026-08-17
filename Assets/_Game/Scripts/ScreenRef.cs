using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Tags a GameObject as belonging to a screen. Place on the root of each screen
    // (Window 1/Screen 1, Window 2/Screen 5, etc.) so ScreenUnlockService can find and
    // activate it once its unlock condition is met.
    //
    // Deliberately separate from SceneRef/UnlockTarget (used for individual machine
    // unlocks) rather than reusing it — screens are a different concept (whole
    // sections of the game turning on) and keeping them apart avoids overloading one
    // enum with both machine-unlocks and screen-unlocks.
    public class ScreenRef : MonoBehaviour
    {
        [SerializeField] private ScreenId screen;
        [Tooltip("Whether this screen starts hidden until its unlock condition is met.")]
        [SerializeField] private bool disableOnAwake = true;

        private static readonly Dictionary<ScreenId, List<GameObject>> _refs
            = new Dictionary<ScreenId, List<GameObject>>();

        public ScreenId Screen => screen;

        private void Awake()
        {
            Register(screen, gameObject);
            if (disableOnAwake)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(screen, out var list))
            {
                list.Remove(gameObject);
                if (list.Count == 0) _refs.Remove(screen);
            }
        }

        private static void Register(ScreenId screen, GameObject go)
        {
            if (!_refs.TryGetValue(screen, out var list))
            {
                list = new List<GameObject>();
                _refs[screen] = list;
            }
            if (!list.Contains(go)) list.Add(go);
        }

        public static GameObject Get(ScreenId screen)
        {
            if (_refs.TryGetValue(screen, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null) return list[i];
            }
            return null;
        }

        public static IReadOnlyList<GameObject> GetAll(ScreenId screen)
            => _refs.TryGetValue(screen, out var list)
                ? (IReadOnlyList<GameObject>)list
                : System.Array.Empty<GameObject>();

        // SetActive is deliberately just visibility — HOW you actually switch between
        // screens (camera pan, layout swap, etc.) is left to whatever's built on top of
        // this. See ScreenUnlockService's class comment.
        public static void SetActive(ScreenId screen, bool active)
        {
            if (!_refs.TryGetValue(screen, out var list)) return;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) list[i].SetActive(active);
        }
    }
}