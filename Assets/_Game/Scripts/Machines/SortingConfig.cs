using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // One routing rule: "send this item type to hole B with probability 'ratioToB'".
    // ratioToB = 1 -> always B, 0 -> always A. The remainder goes to A.
    [Serializable]
    public class SortRule
    {
        [SerializeReference] public ItemType type = new Egg();
        [Range(0f, 1f)] public float ratioToB = 1f;
    }

    // Per-machine sorting configuration the player edits via the config window.
    // Any item type without a rule (or when out of fuel) routes to hole A.
    [Serializable]
    public class SortingConfig
    {
        // Starts with eggs split 50/50 between hole A and hole B. Cleared/overwritten
        // as soon as the player edits the config in-game.
        [SerializeField] private List<SortRule> rules = new List<SortRule>
        {
            new SortRule { type = new Egg(), ratioToB = 0.5f },
        };

        public IReadOnlyList<SortRule> Rules => rules;

        public event Action OnChanged;

        public SortRule GetRule(Type itemType)
        {
            if (itemType == null) return null;
            foreach (var r in rules)
                if (r.type != null && r.type.GetType() == itemType)
                    return r;
            return null;
        }

        // Set (or create) the B-ratio for an item type. ratio is clamped 0..1.
        public void SetRatio(ItemType type, float ratio)
        {
            if (type == null) return;
            ratio = Mathf.Clamp01(ratio);

            var rule = GetRule(type.GetType());
            if (rule == null)
            {
                rule = new SortRule { type = type, ratioToB = ratio };
                rules.Add(rule);
            }
            else
            {
                rule.ratioToB = ratio;
            }
            OnChanged?.Invoke();
        }

        public void RemoveRule(Type itemType)
        {
            rules.RemoveAll(r => r.type != null && r.type.GetType() == itemType);
            OnChanged?.Invoke();
        }

        // Decide the exit for an item type, respecting fuel.
        // hasFuel == false -> always A (drop-through), per the design.
        public SortExit Decide(Type itemType, bool hasFuel)
        {
            if (!hasFuel) return SortExit.A;

            var rule = GetRule(itemType);
            if (rule == null) return SortExit.A;

            return UnityEngine.Random.value < rule.ratioToB ? SortExit.B : SortExit.A;
        }
    }

    public enum SortExit { A, B }
}