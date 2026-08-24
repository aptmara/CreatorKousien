using TMPro;
using UnityEngine;

namespace Game.Presentation.UI.Loading
{
    public sealed class LoadingBlinkFrames : ScriptableObject
    {
        [SerializeField] private Sprite _open;
        [SerializeField] private Sprite _halfClosed;
        [SerializeField] private Sprite _closed;
        [SerializeField] private TMP_FontAsset _font;

        public Sprite Open => _open;
        public Sprite HalfClosed => _halfClosed;
        public Sprite Closed => _closed;
        public TMP_FontAsset Font => _font;
    }
}
