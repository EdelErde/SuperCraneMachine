using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    public class UiClickSfx : MonoBehaviour
    {
        [SerializeField] private SfxSource sfx;
        [Tooltip("Also catch buttons that get created/enabled after startup.")]
        [SerializeField] private bool rescanOnEnable = true;

        private void Awake()
        {
            if (sfx == null) sfx = GetComponent<SfxSource>();
            Wire();
        }

        private void OnEnable()
        {
            if (rescanOnEnable) Wire();
        }

        public void Wire()
        {
            if (sfx == null) return;

            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            foreach (var b in buttons)
            {
                // Skip prefab assets / anything not in a loaded scene.
                if (b == null || !b.gameObject.scene.IsValid()) continue;

                // Remove first so repeated scans never stack duplicate calls.
                b.onClick.RemoveListener(sfx.Play);
                b.onClick.AddListener(sfx.Play);
            }
        }
    }
}