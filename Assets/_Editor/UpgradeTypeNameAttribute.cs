using UnityEngine;

namespace CraneMachine
{
    // Put on a string field that stores an IUpgrade class name (see
    // UpgradePurchasedCondition.upgradeTypeName). Draws as a searchable dropdown of
    // every concrete IUpgrade type instead of a free-typed string — see
    // UpgradeTypeNameDrawer in Assets/_Editor.
    public class UpgradeTypeNameAttribute : PropertyAttribute { }
}