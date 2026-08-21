using UnityEngine;

namespace CraneMachine
{
    // A labelled drop point a drone flies items to. Drop this component on any object
    // (usually near an input mouth like a FuelFunnel or FuelFilter entry). The drone
    // releases the carried item at DropPoint (this object's position + a local x/y
    // offset), then lets physics carry it the last few units into whatever collider
    // is there — exactly like the player dropping an item over that mouth by hand.
    //
    // A destination is just a point in space; it is NOT tied to a specific machine.
    // That keeps it stupid-simple: point the offset at the mouth, done. The gizmo shows
    // exactly where items will be released so you can line it up in the Scene view.
    //
    // Each destination has a stable Id (its name by default) so the route config can
    // reference it even though the config lives on the fab, not the destination.
    public class DroneDestination : MonoBehaviour
    {
        [Tooltip("Shown in the drone setup window. Defaults to the GameObject name if left blank.")]
        [SerializeField] private string displayName = "";

        [Tooltip("Release position relative to this object, in LOCAL space. The drone drops " +
                 "the item here; physics carries it the rest of the way into the mouth below.")]
        [SerializeField] private Vector2 dropOffset = Vector2.zero;

        [Tooltip("How close (world units) the drone's carried item must get to the drop point " +
                 "before it releases. Larger = drops from further out (item arcs in).")]
        [SerializeField] private float releaseRadius = 0.25f;

        [Header("Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private bool alwaysShowGizmo = true;

        // Stable identifier used by DroneRouteConfig to remember assignments. We use the
        // GameObject name because these are hand-placed scene objects the designer names;
        // it survives play-mode and serialization without needing GUIDs.
        public string Id => gameObject.name;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

        public float ReleaseRadius => Mathf.Max(0.02f, releaseRadius);

        // World-space point where the drone releases the item.
        public Vector2 DropPoint => transform.TransformPoint(dropOffset);

        // ---- Registry so the drone/setup window can find all destinations without
        // wiring every one into the fab by hand (mirrors SceneRef's static registry). ----
        private static readonly System.Collections.Generic.List<DroneDestination> _all
            = new System.Collections.Generic.List<DroneDestination>();

        public static System.Collections.Generic.IReadOnlyList<DroneDestination> All => _all;

        private void OnEnable() { if (!_all.Contains(this)) _all.Add(this); }
        private void OnDisable() { _all.Remove(this); }

        public static DroneDestination Find(string id)
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null && _all[i].Id == id) return _all[i];
            return null;
        }

        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo) DrawGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysShowGizmo) DrawGizmo();
        }

        private void DrawGizmo()
        {
            Vector3 p = DropPoint;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(p, ReleaseRadius);
            // Little tick from the object origin to the drop point so the offset is visible.
            Gizmos.DrawLine(transform.position, p);
        }
    }
}