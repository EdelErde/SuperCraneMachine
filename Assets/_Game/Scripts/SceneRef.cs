using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class SceneRef : MonoBehaviour
    {
        [SerializeField] private UnlockTarget target;
        [SerializeField] private bool disableOnAwake = true;

        private static readonly Dictionary<UnlockTarget, List<GameObject>> _refs
            = new Dictionary<UnlockTarget, List<GameObject>>();

        private void Awake()
        {
            Register(target, gameObject);
            if (disableOnAwake)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(target, out var list))
            {
                list.Remove(gameObject);
                if (list.Count == 0) _refs.Remove(target);
            }
        }

        private static void Register(UnlockTarget target, GameObject go)
        {
            if (!_refs.TryGetValue(target, out var list))
            {
                list = new List<GameObject>();
                _refs[target] = list;
            }
            if (!list.Contains(go)) list.Add(go);
        }

        public static GameObject Get(UnlockTarget target)
        {
            if (_refs.TryGetValue(target, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null) return list[i];
            }
            return null;
        }

        public static IReadOnlyList<GameObject> GetAll(UnlockTarget target)
            => _refs.TryGetValue(target, out var list)
                ? (IReadOnlyList<GameObject>)list
                : System.Array.Empty<GameObject>();

        public static void SetActive(UnlockTarget target, bool active)
        {
            if (!_refs.TryGetValue(target, out var list)) return;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) list[i].SetActive(active);
        }
    }
}