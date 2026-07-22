using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class SceneRef : MonoBehaviour
    {
        [SerializeField] private UnlockTarget target;
        [SerializeField] private bool disableOnAwake = true;

        private static readonly Dictionary<UnlockTarget, GameObject> _refs = new Dictionary<UnlockTarget, GameObject>();

        private void Awake()
        {
            _refs[target] = gameObject;
            if (disableOnAwake)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(target, out var go) && go == gameObject)
                _refs.Remove(target);
        }

        public static GameObject Get(UnlockTarget target)
            => _refs.TryGetValue(target, out var go) ? go : null;

        public static void SetActive(UnlockTarget target, bool active)
        {
            var go = Get(target);
            if (go != null) go.SetActive(active);
        }
    }
}