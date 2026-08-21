using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // One routing rule: "send this item type to hole <exit>, always."
    // Replaces the old probabilistic ratioToB slider — the Sorter Dialog now assigns
    // each item type wholly to one side by dragging its icon into the A or B list.
    [Serializable]
    public class SortRule
    {
        [SerializeReference] public ItemType type = new Egg();
        public SortExit exit = SortExit.A;
    }

    // Per-machine sorting configuration the player edits via the world-space Sorter
    // Dialog. Any item type without a rule (or when out of fuel) routes to hole A.
    [Serializable]
    public class SortingConfig
    {
        // Starts empty -> every item type routes to hole A (the default/drop-through
        // exit) until the player drags it to hole B in the dialog.
        [SerializeField] private List<SortRule> rules = new List<SortRule>();

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

        // Assign an item type wholly to an exit.
        public void SetExit(ItemType type, SortExit exit)
        {
            if (type == null) return;

            var rule = GetRule(type.GetType());
            if (rule == null)
            {
                rule = new SortRule { type = type, exit = exit };
                rules.Add(rule);
            }
            else
            {
                rule.exit = exit;
            }
            OnChanged?.Invoke();
        }

        // Which exit an item type is currently assigned to (A if no rule yet).
        public SortExit ExitFor(Type itemType)
        {
            var rule = GetRule(itemType);
            return rule != null ? rule.exit : SortExit.A;
        }

        public void RemoveRule(Type itemType)
        {
            rules.RemoveAll(r => r.type != null && r.type.GetType() == itemType);
            OnChanged?.Invoke();
        }

        // Decide the exit for an item type, respecting fuel and whether exit C is unlocked.
        // hasFuel == false -> always A (drop-through), per the design.
        // exitCUnlocked == false -> a rule pointing at C falls back to A, so items can never
        // be routed into a locked third exit (even if a rule was authored for it earlier).
        public SortExit Decide(Type itemType, bool hasFuel, bool exitCUnlocked)
        {
            if (!hasFuel) return SortExit.A;
            var exit = ExitFor(itemType);
            if (exit == SortExit.C && !exitCUnlocked) return SortExit.A;
            return exit;
        }
    }

    public enum SortExit { A, B, C }
}