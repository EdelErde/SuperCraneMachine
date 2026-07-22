using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(WorldInteractionController))]
    public class DragTensionLines : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float width = 0.05f;
        [SerializeField] private Color safeColor = Color.green;
        [SerializeField] private Color breakColor = Color.red;
        [Tooltip("Sorting order so the lines draw above items.")]
        [SerializeField] private int sortingOrder = 100;

        private WorldInteractionController _controller;
        private readonly List<LineRenderer> _pool = new List<LineRenderer>();

        private void Awake() => _controller = GetComponent<WorldInteractionController>();

        private void LateUpdate()
        {
            var held = _controller.Held;
            EnsurePool(held.Count);

            Vector3 cursor = _controller.PointerWorldPosition;

            for (int i = 0; i < _pool.Count; i++)
            {
                var line = _pool[i];

                if (i >= held.Count || held[i].Transform == null || !held[i].IsDragging)
                {
                    line.enabled = false;
                    continue;
                }

                var item = held[i];
                Color c = Color.Lerp(safeColor, breakColor, item.Strain);

                line.enabled = true;
                line.startColor = c;
                line.endColor = c;
                line.SetPosition(0, item.Transform.position);
                line.SetPosition(1, cursor);
            }
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
            {
                var go = new GameObject($"TensionLine_{_pool.Count}");
                go.transform.SetParent(transform, false);

                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.startWidth = width;
                line.endWidth = width;
                line.numCapVertices = 2;
                line.textureMode = LineTextureMode.Stretch;
                line.sortingOrder = sortingOrder;
                if (lineMaterial != null)
                    line.material = lineMaterial;
                else
                    line.material = new Material(Shader.Find("Sprites/Default"));

                _pool.Add(line);
            }
        }
    }
}