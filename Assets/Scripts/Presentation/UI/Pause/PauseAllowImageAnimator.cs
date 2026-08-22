using UnityEngine;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PauseAllowImageAnimator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private PauseSelectionOutline _selectionFeedback;

        [Header("Timing")]
        [SerializeField, Min(0.03f)] private float _loopDuration = 2f;
        [SerializeField, Min(0f)] private float _holdDuration = 0.75f;
        [SerializeField, Min(0.01f)] private float _exitDuration = 0.45f;

        [Header("Fade In")]
        [SerializeField] private AnimationCurve _fadeInAlphaCurve = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.55f, 1f),
            new Keyframe(1f, 1f));

        [Header("Exit")]
        [SerializeField] private Vector2 _exitOffset = new(-320f, -220f);
        [SerializeField] private AnimationCurve _exitMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _exitArcHeight = 60f;
        [SerializeField] private float _exitAngleOffset = 25f;
        [SerializeField] private AnimationCurve _exitRotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _exitAlphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _restPosition;
        private Quaternion _restRotation;
        private float _restAlpha;
        private float _elapsed;
        private bool _wasHighlighted;
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            RestoreRestState();
            _wasHighlighted = false;
        }

        private void OnDisable()
        {
            RestoreRestState();
            _wasHighlighted = false;
        }

        private void OnValidate()
        {
            _loopDuration = Mathf.Max(0.03f, _loopDuration);
            _holdDuration = Mathf.Clamp(_holdDuration, 0f, Mathf.Max(0f, _loopDuration - 0.02f));
            _exitDuration = Mathf.Clamp(_exitDuration, 0.01f, Mathf.Max(0.01f, _loopDuration - _holdDuration - 0.01f));
            _fadeInAlphaCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _exitMovementCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _exitRotationCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _exitAlphaCurve ??= AnimationCurve.Linear(0f, 1f, 1f, 0f);
        }

        private void Update()
        {
            bool highlighted = _selectionFeedback != null && _selectionFeedback.IsHighlighted;
            if (!highlighted)
            {
                if (_wasHighlighted)
                {
                    RestoreRestState();
                }

                _wasHighlighted = false;
                return;
            }

            if (!_wasHighlighted)
            {
                _elapsed = 0f;
                ApplyFadeInState(0f);
                _wasHighlighted = true;
            }

            _elapsed += Time.unscaledDeltaTime;
            float loopDuration = Mathf.Max(0.03f, _loopDuration);
            if (_elapsed >= loopDuration)
            {
                _elapsed %= loopDuration;
            }

            float holdDuration = Mathf.Clamp(_holdDuration, 0f, loopDuration - 0.02f);
            float exitDuration = Mathf.Clamp(_exitDuration, 0.01f, loopDuration - holdDuration - 0.01f);
            float fadeInDuration = Mathf.Max(0.01f, loopDuration - holdDuration - exitDuration);
            if (_elapsed < fadeInDuration)
            {
                ApplyFadeInState(_elapsed / fadeInDuration);
            }
            else if (_elapsed < fadeInDuration + holdDuration)
            {
                ApplyRestState();
            }
            else
            {
                ApplyExitState((_elapsed - fadeInDuration - holdDuration) / exitDuration);
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _rectTransform = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _restPosition = _rectTransform.anchoredPosition;
            _restRotation = _rectTransform.localRotation;
            _restAlpha = _canvasGroup.alpha;
            _initialized = true;
        }

        private void ApplyFadeInState(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            _rectTransform.anchoredPosition = _restPosition;
            _rectTransform.localRotation = _restRotation;
            _canvasGroup.alpha = _restAlpha * Mathf.Clamp01(_fadeInAlphaCurve.Evaluate(time));
        }

        private void ApplyRestState()
        {
            _rectTransform.anchoredPosition = _restPosition;
            _rectTransform.localRotation = _restRotation;
            _canvasGroup.alpha = _restAlpha;
        }

        private void ApplyExitState(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            float movement = _exitMovementCurve.Evaluate(time);
            Vector2 exitPosition = _restPosition + _exitOffset;
            Vector2 direction = exitPosition - _restPosition;
            Vector2 normal = direction.sqrMagnitude > 0f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.down;
            float arc = Mathf.Sin(time * Mathf.PI) * _exitArcHeight;

            _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_restPosition, exitPosition, movement) + normal * arc;
            float rotation = Mathf.LerpUnclamped(0f, _exitAngleOffset, _exitRotationCurve.Evaluate(time));
            _rectTransform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, rotation);
            _canvasGroup.alpha = _restAlpha * Mathf.Clamp01(_exitAlphaCurve.Evaluate(time));
        }

        private void RestoreRestState()
        {
            if (!_initialized)
            {
                return;
            }

            _elapsed = 0f;
            ApplyRestState();
        }
    }
}
