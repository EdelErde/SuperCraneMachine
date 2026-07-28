using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    public class ItemIcon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI chanceLabel;
        [Tooltip("Optional: shows the current sell price (incl. all multipliers). Sits below the chance.")]
        [SerializeField] private TextMeshProUGUI priceLabel;
        [SerializeField] private Image icon;

        [SerializeField] private string priceFormat = "${0:N0}";

        public void Set(string itemName, float chance01, string chanceFormat, Sprite sprite, int price = -1)
        {
            if (nameLabel != null) nameLabel.text = itemName;
            if (chanceLabel != null) chanceLabel.text = string.Format(chanceFormat, chance01 * 100f);

            if (priceLabel != null)
            {
                bool show = price >= 0;
                priceLabel.gameObject.SetActive(show);
                if (show)
                    priceLabel.text = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture, priceFormat, price);
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }
    }
}