using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(WorldInteractionController))]
    public class CursorRadiusIndicator : MonoBehaviour
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int sortingOrder = 90;
        [Tooltip("Extra scale applied on top of the radius. Use if the sprite has padding.")]
        [SerializeField] private float scaleMultiplier = 1f;
        [SerializeField] private bool hideWhenDragging;

        private WorldInteractionController _controller;
        private SpriteRenderer _renderer;

        private static float DragRadius =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.DragRadius) : 0.75f;

        private void Awake()
        {
            _controller = GetComponent<WorldInteractionController>();
            Build();
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;

            bool visible = sprite != null && !(hideWhenDragging && _controller.Held.Count > 0);
            _renderer.enabled = visible;
            if (!visible) return;

            if (_renderer.sprite != sprite) _renderer.sprite = sprite;

            float radius = Mathf.Max(0.05f, DragRadius);

            float spriteWidth = sprite.bounds.size.x;
            float scale = spriteWidth > 0.0001f
                ? (radius * 2f) / spriteWidth
                : radius * 2f;

            _renderer.transform.position = _controller.PointerWorldPosition;
            _renderer.transform.localScale = Vector3.one * (scale * scaleMultiplier);
            _renderer.color = color;
        }

        private void Build()
        {
            var go = new GameObject("CursorRadius");
            go.transform.SetParent(transform, false);

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = color;
            _renderer.sortingLayerName = sortingLayer;
            _renderer.sortingOrder = sortingOrder;
        }
    }
}