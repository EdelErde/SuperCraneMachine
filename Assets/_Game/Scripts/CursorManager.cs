using UnityEngine;

namespace CraneMachine
{
    public enum CursorState { Default, Hover, Drag }

    public class CursorManager : MonoBehaviour
    {
        [System.Serializable]
        public struct CursorSprite
        {
            public Texture2D texture;
            public Vector2 hotspot;
        }

        [SerializeField] private CursorSprite defaultCursor;
        [SerializeField] private CursorSprite hoverCursor;
        [SerializeField] private CursorSprite dragCursor;

        private CursorState _state = CursorState.Default;

        private void Awake() => ServiceLocator.CursorManager = this;

        private void Start() => Apply(CursorState.Default);

        public void Set(CursorState state)
        {
            if (state == _state) return;
            _state = state;
            Apply(state);
        }

        private void Apply(CursorState state)
        {
            var c = state switch
            {
                CursorState.Drag => dragCursor,
                CursorState.Hover => hoverCursor,
                _ => defaultCursor,
            };

            if (c.texture != null)
                Cursor.SetCursor(c.texture, c.hotspot, CursorMode.Auto);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}