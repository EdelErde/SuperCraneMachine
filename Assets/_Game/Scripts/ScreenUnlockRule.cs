using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // One screen's unlock rule: which screen, and every condition that must be true
    // (AND) for it to unlock. Add more ScreenUnlockCondition subclasses to support OR
    // groups or other combinations later if needed — kept simple (AND-only) for now.
    [Serializable]
    public class ScreenUnlockRule
    {
        public ScreenId screen;

        [SerializeReference]
        public List<ScreenUnlockCondition> conditions = new List<ScreenUnlockCondition>();

        public bool IsMet()
        {
            if (conditions == null || conditions.Count == 0) return false;

            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null || !c.IsMet()) return false;
            }
            return true;
        }
    }
}