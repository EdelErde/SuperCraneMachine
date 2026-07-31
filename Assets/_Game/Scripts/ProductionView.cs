using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Container that shows one production card per active ResourceConverter, generating them
    // at runtime (like UpgradeView generates buttons). Rebuilds when converters are added or
    // removed, so adding a new resource later = drop a ResourceConverter in the scene; a card
    // appears here automatically with no extra wiring.
    public class ProductionView : MonoBehaviour
    {
        [Header("Generation")]
        [Tooltip("Card prefab (a ProductionCard) spawned once per converter.")]
        [SerializeField] private ProductionCard cardPrefab;
        [Tooltip("Parent the cards spawn under (give it a vertical/grid layout group).")]
        [SerializeField] private RectTransform cardParent;

        [Tooltip("Optional: hide this view entirely while there are no converters.")]
        [SerializeField] private bool hideWhenEmpty = false;
        [SerializeField] private GameObject viewRoot;

        private readonly List<ProductionCard> _cards = new List<ProductionCard>();

        private void OnEnable()
        {
            var reg = ServiceLocator.ResourceConverters;
            if (reg != null) reg.OnChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            var reg = ServiceLocator.ResourceConverters;
            if (reg != null) reg.OnChanged -= Rebuild;
        }

        private void Rebuild()
        {
            ClearCards();

            var reg = ServiceLocator.ResourceConverters;
            var converters = reg != null ? reg.Converters : null;

            if (cardPrefab != null && cardParent != null && converters != null)
            {
                for (int i = 0; i < converters.Count; i++)
                {
                    var converter = converters[i];
                    if (converter == null) continue;

                    var card = Instantiate(cardPrefab, cardParent);
                    card.Bind(converter);
                    _cards.Add(card);
                }
            }

            if (hideWhenEmpty && viewRoot != null)
                viewRoot.SetActive(_cards.Count > 0);
        }

        private void ClearCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
        }
    }
}