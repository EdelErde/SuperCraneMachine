using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Plays a click when this Button is pressed. Put on any UI Button.
    [RequireComponent(typeof(SfxSource))]
    [RequireComponent(typeof(Button))]
    public class UiClickSfx : MonoBehaviour
    {
        private SfxSource _sfx;
        private void Awake()
        {
            _sfx = GetComponent<SfxSource>();
            GetComponent<Button>().onClick.AddListener(_sfx.Play);
        }
    }
}
