using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Container that shows one row per active fuel consumer (Leaf Blower, Sorting
    // Machine, ...), generating them at runtime the way ProductionView generates cards.
    // Each row displays that machine's current fuel usage per second and reflects
    // whether it's on or off. New fuel-burning machines need no extra UI wiring:
    // implement IFuelConsumer + register, and a row appears here.
    //
    // The FuelConsumerRegistry has no change event, so we watch its list cheaply each
    // frame and rebuild only when the set of machines actually changes. Per-second draw
    // values update inside each row's own Update.
    public class FuelConsumerView : MonoBehaviour
    {
        [Header("Generation")]
        [Tooltip("Row prefab (a FuelConsumerRow) spawned once per consumer.")]
        [SerializeField] private FuelConsumerRow rowPrefab;
        [Tooltip("Parent the rows spawn under (give it a vertical/grid layout group).")]
        [SerializeField] private RectTransform rowParent;

        [Tooltip("Optional: hide this view entirely while there are no consumers.")]
        [SerializeField] private bool hideWhenEmpty = false;
        [SerializeField] private GameObject viewRoot;

        private readonly List<FuelConsumerRow> _rows = new List<FuelConsumerRow>();
        private readonly List<IFuelConsumer> _bound = new List<IFuelConsumer>();

        private void OnEnable() => Rebuild();

        private void Update()
        {
            if (ConsumersChanged()) Rebuild();
        }

        // Cheap identity check: rebuild only when the registry's list differs from what
        // we currently have rows for.
        private bool ConsumersChanged()
        {
            var reg = ServiceLocator.FuelConsumers;
            var consumers = reg != null ? reg.Consumers : null;

            int count = consumers != null ? consumers.Count : 0;
            if (count != _bound.Count) return true;

            for (int i = 0; i < count; i++)
                if (!ReferenceEquals(consumers[i], _bound[i])) return true;

            return false;
        }

        private void Rebuild()
        {
            ClearRows();

            var reg = ServiceLocator.FuelConsumers;
            var consumers = reg != null ? reg.Consumers : null;

            if (rowPrefab != null && rowParent != null && consumers != null)
            {
                for (int i = 0; i < consumers.Count; i++)
                {
                    var consumer = consumers[i];
                    if (consumer == null) continue;

                    var row = Instantiate(rowPrefab, rowParent);
                    row.Bind(consumer);
                    _rows.Add(row);
                    _bound.Add(consumer);
                }
            }

            if (hideWhenEmpty && viewRoot != null)
                viewRoot.SetActive(_rows.Count > 0);
        }

        private void ClearRows()
        {
            foreach (var r in _rows)
                if (r != null) Destroy(r.gameObject);
            _rows.Clear();
            _bound.Clear();
        }
    }
}