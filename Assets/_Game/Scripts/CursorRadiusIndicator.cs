using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(WorldInteractionController))]
    public class CursorRadiusIndicator : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private float width = 0.03f;
        [SerializeField] private int segments = 48;
        [SerializeField] private int sortingOrder = 90;

        private WorldInteractionController _controller;
        private LineRenderer _line;

        private static float DragRadius =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.DragRadius) : 0.75f;

        private void Awake()
        {
            _controller = GetComponent<WorldInteractionController>();
            BuildLine();
        }

        private void LateUpdate()
        {
            float radius = Mathf.Max(0.05f, DragRadius);
            Vector3 center = _controller.PointerWorldPosition;

            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                _line.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            }

            _line.startColor = color;
            _line.endColor = color;
        }

        private void BuildLine()
        {
            var go = new GameObject("CursorRadius");
            go.transform.SetParent(transform, false);

            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.positionCount = segments + 1;
            _line.startWidth = width;
            _line.endWidth = width;
            _line.sortingOrder = sortingOrder;
            _line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        }
    }
}