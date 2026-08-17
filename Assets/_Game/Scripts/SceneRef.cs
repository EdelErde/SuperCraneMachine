using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class SceneRef : MonoBehaviour
    {
        [SerializeField] private UnlockTarget target;
        [SerializeField] private bool disableOnAwake = true;
        [Tooltip("If true, this object does the OPPOSITE of what SetActive is told: " +
                 "when the upgrade/trigger calls SetActive(target, true), this object " +
                 "turns OFF instead of on (and vice versa). Lets one upgrade activate " +
                 "some objects and deactivate others under the same target.")]
        [SerializeField] private bool invert = false;

        private static readonly Dictionary<UnlockTarget, List<SceneRef>> _refs
            = new Dictionary<UnlockTarget, List<SceneRef>>();

        private void Awake()
        {
            Register(target, this);
            if (disableOnAwake)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(target, out var list))
            {
                list.Remove(this);
                if (list.Count == 0) _refs.Remove(target);
            }
        }

        private static void Register(UnlockTarget target, SceneRef sceneRef)
        {
            if (!_refs.TryGetValue(target, out var list))
            {
                list = new List<SceneRef>();
                _refs[target] = list;
            }
            if (!list.Contains(sceneRef)) list.Add(sceneRef);
        }

        public static GameObject Get(UnlockTarget target)
        {
            if (_refs.TryGetValue(target, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null) return list[i].gameObject;
            }
            return null;
        }

        public static IReadOnlyList<GameObject> GetAll(UnlockTarget target)
        {
            if (!_refs.TryGetValue(target, out var list)) return System.Array.Empty<GameObject>();

            var result = new List<GameObject>(list.Count);
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) result.Add(list[i].gameObject);
            return result;
        }

        // Each registered SceneRef resolves its own resulting state: normally it goes
        // to 'active', but if 'invert' is checked on that instance it goes to
        // '!active' instead. This is what lets a single upgrade/trigger call turn some
        // objects on and others off at the same time — tag them with the same target,
        // check 'invert' on the ones that should do the opposite.
        public static void SetActive(UnlockTarget target, bool active)
        {
            if (!_refs.TryGetValue(target, out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                if (r == null) continue;
                r.gameObject.SetActive(r.invert ? !active : active);
            }
        }
    }
}