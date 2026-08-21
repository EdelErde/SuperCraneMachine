#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CraneMachine
{
    // Tools > Drone Fab menu. Builds a minimal, wired scene rig you then dress with art:
    //   - a Drone Fab object (with entry mouth + exit + idle points as children)
    //   - a starter Drone prefab in the scene (you turn it into a prefab / assign a sprite)
    //   - one example DroneDestination you can duplicate and place at each input mouth
    //
    // Deliberately light: unlike the liquid tool it generates no shaders/RTs/materials —
    // the drone & fab are ordinary sprites/rigidbodies you author. It just spares you the
    // wiring (relays, point transforms, config references) that's easy to get wrong.
    public static class DroneSetupEditor
    {
        [MenuItem("Tools/Drone Fab/Create Drone Fab Setup")]
        public static void CreateSetup()
        {
            var log = new System.Text.StringBuilder("Drone Fab setup:\n");

            // ---- Fab root ----
            var fabGO = new GameObject("Drone Fab");
            Undo.RegisterCreatedObjectUndo(fabGO, "Create Drone Fab");
            var fab = fabGO.AddComponent<DroneFab>();

            // Entry mouth (right side) — separate collider so the relay path is exercised
            // and you can move the mouth independently of the fab body.
            var entry = new GameObject("Entry (tin can in)");
            entry.transform.SetParent(fabGO.transform, false);
            entry.transform.localPosition = new Vector3(0.6f, -0.3f, 0f);
            var entryCol = entry.AddComponent<BoxCollider2D>();
            entryCol.isTrigger = true;

            // Exit (left side) — where drones pop out.
            var exit = new GameObject("Exit (drone out)");
            exit.transform.SetParent(fabGO.transform, false);
            exit.transform.localPosition = new Vector3(-0.6f, -0.3f, 0f);

            // Idle loiter point.
            var idle = new GameObject("Idle point");
            idle.transform.SetParent(fabGO.transform, false);
            idle.transform.localPosition = new Vector3(-0.6f, -1.0f, 0f);

            WireSerialized(fab, entryCol, exit.transform, idle.transform, log);

            // ---- Starter drone (in-scene; you make it a prefab + add art) ----
            var droneGO = new GameObject("Drone (make me a prefab)");
            Undo.RegisterCreatedObjectUndo(droneGO, "Create Drone");
            var rb = droneGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            droneGO.AddComponent<Drone>();
            droneGO.transform.position = exit.transform.position;
            log.AppendLine("- Created a starter 'Drone' object. Add a sprite, make it a prefab, " +
                           "then assign that prefab to the fab's 'Drone Prefab' field.");

            // ---- Example destination ----
            var destGO = new GameObject("Drone Destination (duplicate me)");
            Undo.RegisterCreatedObjectUndo(destGO, "Create Drone Destination");
            destGO.AddComponent<DroneDestination>();
            destGO.transform.position = new Vector3(2f, -2f, 0f);
            log.AppendLine("- Created an example 'Drone Destination'. Duplicate it next to each " +
                           "input mouth (funnel/filter), rename each uniquely, and set its Drop Offset.");

            log.AppendLine();
            log.AppendLine("REMAINING MANUAL STEPS:");
            log.AppendLine("1. Give the fab body + drone a sprite; assign the drone prefab to the fab.");
            log.AppendLine("2. Place a Drone Destination at each mouth; the drop offset gizmo shows where items land.");
            log.AppendLine("3. Build the Drone Setup window (world-space canvas) and point its DroneFab field at this fab.");
            log.AppendLine("4. Tag the fab with a SceneRef (target = DroneFab) if you want it unlock-gated.");
            log.AppendLine("5. Add the drone GameStat entries + upgrades (see DroneStats.cs).");

            Debug.Log(log.ToString());
            Selection.activeGameObject = fabGO;
        }

        // Assign the fab's private serialized fields via SerializedObject so we don't need
        // public setters (matches how the liquid tool wires private fields).
        private static void WireSerialized(DroneFab fab, Collider2D entry, Transform exit, Transform idle,
                                           System.Text.StringBuilder log)
        {
            var so = new SerializedObject(fab);
            SetRef(so, "entry", entry);
            SetRef(so, "exit", exit);
            SetRef(so, "idlePoint", idle);
            so.ApplyModifiedProperties();
            log.AppendLine("- Wired fab entry/exit/idle points.");
        }

        private static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}
#endif