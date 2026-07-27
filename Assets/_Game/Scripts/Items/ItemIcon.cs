using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    public class ItemIcon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI chanceLabel;
        [SerializeField] private Image icon;

        public void Set(string itemName, float chance01, string chanceFormat, Sprite sprite)
        {
            if (nameLabel != null) nameLabel.text = itemName;
            if (chanceLabel != null) chanceLabel.text = string.Format(chanceFormat, chance01 * 100f);

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }
    }
}