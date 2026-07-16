using System.Collections;
using UnityEngine;

namespace Game.Presentation.UI.Title
{
    /// <summary>
    /// タイトル画面の看板UIを、上から落下して展開する演出を制御する。
    /// </summary>
    public sealed class TitleSignboardAnimator : MonoBehaviour
    {
        private const float MinDuration = 0.01f;

        [SerializeField] private RectTransform _signRoot;
        [SerializeField] private RectTransform _scrollBody;
        [SerializeField] private RectTransform _leftRoll;
        [SerializeField] private RectTransform _rightRoll;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform[] _delayedUiParts;

        [Header("再生設定")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private bool _refreshFinalStateOnStart = true;
        [SerializeField] private bool _warnOptionalReferenceMissing = true;
        [SerializeField] private SignboardAnimationPattern _pattern = SignboardAnimationPattern.RattleScroll;
        [SerializeField] private float _startYOffset = 900f;
        [SerializeField] private float _introDelay = 0.15f;
        [SerializeField] private float _dropDuration = 0.75f;
        [SerializeField] private float _settleDuration = 0.28f;
        [SerializeField] private float _partsDelay = 0.08f;
        [SerializeField] private float _partsStagger = 0.08f;

        [Header("揺れ設定")]
        [SerializeField] private float _rattleAngle = 8f;
        [SerializeField] private float _rattleInterval = 0.045f;
        [SerializeField] private float _rattleX = 18f;
        [SerializeField] private float _rattleY = 8f;
        [SerializeField] private float _overshootY = 36f;
        [SerializeField] private float _impactSquash = 0.12f;

        [Header("落下設定")]
        [SerializeField] private float _rolledBodyScaleY = 0.08f;
        [SerializeField] private float _rollSlideDistance = 72f;
        [SerializeField] private float _rollRotation = 720f;
        [SerializeField] private float _unrollDelayRate = 0.18f;

        [Header("カーブ")]
        [SerializeField] private AnimationCurve _dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _settleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector2 _finalRootPosition;
        private Vector3 _finalRootScale;
        private Quaternion _finalRootRotation;
        private Vector3 _finalBodyScale;
        private Vector2 _finalLeftRollPosition;
        private Vector2 _finalRightRollPosition;
        private Quaternion _finalLeftRollRotation;
        private Quaternion _finalRightRollRotation;
        private Vector2[] _finalPartsPositions;
        private Vector3[] _finalPartsScales;
        private Coroutine _playCoroutine;
        private bool _hasCachedFinalState;
        private bool _hasRefreshedFinalStateOnEnable;
        private bool _hasWarnedOptionalReferences;

        /// <summary>
        /// 看板アニメーションの種類。
        /// </summary>
        public enum SignboardAnimationPattern
        {
            RattleScroll,
            BounceDrop,
            SwingDrop,
            SnapUnroll
        }

        private void Awake()
        {
            CacheFinalState();

            if (!_playOnStart)
            {
                ResetToStartState();
            }
        }

        private void OnEnable()
        {
            if (_playOnStart)
            {
                if (_refreshFinalStateOnStart && !_hasRefreshedFinalStateOnEnable)
                {
                    Canvas.ForceUpdateCanvases();
                    CacheFinalState();
                    _hasRefreshedFinalStateOnEnable = true;
                }

                Play();
            }
        }

        private void OnDisable()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        private void OnValidate()
        {
            _introDelay = Mathf.Max(0f, _introDelay);
            _dropDuration = Mathf.Max(MinDuration, _dropDuration);
            _settleDuration = Mathf.Max(MinDuration, _settleDuration);
            _partsDelay = Mathf.Max(0f, _partsDelay);
            _partsStagger = Mathf.Max(0f, _partsStagger);
            _rattleInterval = Mathf.Max(MinDuration, _rattleInterval);
            _rattleX = Mathf.Max(0f, _rattleX);
            _rattleY = Mathf.Max(0f, _rattleY);
            _impactSquash = Mathf.Clamp(_impactSquash, 0f, 0.35f);
            _rolledBodyScaleY = Mathf.Clamp(_rolledBodyScaleY, 0.01f, 1f);
            _unrollDelayRate = Mathf.Clamp01(_unrollDelayRate);
            _dropCurve = EnsureCurve(_dropCurve);
            _settleCurve = EnsureCurve(_settleCurve);
            _scaleCurve = EnsureCurve(_scaleCurve);
        }

        /// <summary>
        /// 現在のInspector設定で看板アニメーションを再生する。
        /// </summary>
        public void Play()
        {
            Play(false);
        }

        /// <summary>
        /// 現在のInspector設定で看板アニメーションを再生する。
        /// </summary>
        /// <param name="refreshFinalState">現在のRectTransform状態を完成状態として取り直すかどうか。</param>
        public void Play(bool refreshFinalState)
        {
            if (_signRoot == null)
            {
                Debug.LogWarning($"{nameof(TitleSignboardAnimator)}: 看板のRectTransformが設定されていません。", this);
                return;
            }

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
            }

            if (refreshFinalState)
            {
                CacheFinalState();
            }
            else
            {
                EnsureFinalStateCached();
            }

            WarnOptionalReferencesIfNeeded();
            ResetToStartState();
            _playCoroutine = StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// 看板を完成状態に反映する。
        /// </summary>
        public void CompleteImmediately()
        {
            if (_signRoot == null)
            {
                return;
            }

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            EnsureFinalStateCached();
            ApplyFinalState();
        }

        /// <summary>
        /// 看板を開始前の状態に戻す。
        /// </summary>
        public void ResetAnimation()
        {
            if (_signRoot == null)
            {
                return;
            }

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            EnsureFinalStateCached();
            ResetToStartState();
        }

        private IEnumerator PlayRoutine()
        {
            yield return WaitRoutine(_introDelay);

            switch (_pattern)
            {
                case SignboardAnimationPattern.BounceDrop:
                    yield return PlayBounceDropRoutine();
                    break;
                case SignboardAnimationPattern.SwingDrop:
                    yield return PlaySwingDropRoutine();
                    break;
                case SignboardAnimationPattern.SnapUnroll:
                    yield return PlaySnapUnrollRoutine();
                    break;
                case SignboardAnimationPattern.RattleScroll:
                default:
                    yield return PlayRattleScrollRoutine();
                    break;
            }

            yield return PlayDelayedPartsRoutine();
            ApplyFinalState();
            _playCoroutine = null;
        }

        private IEnumerator PlayRattleScrollRoutine()
        {
            yield return DropToPositionRoutine(_finalRootPosition + Vector2.up * _overshootY, _dropDuration, true, 0f);
            yield return SettleToFinalRoutine(_settleDuration, true);
        }

        private IEnumerator PlayBounceDropRoutine()
        {
            yield return DropToPositionRoutine(_finalRootPosition - Vector2.up * _overshootY, _dropDuration, false);
            yield return SettleToFinalRoutine(_settleDuration, false);
        }

        private IEnumerator PlaySwingDropRoutine()
        {
            yield return DropToPositionRoutine(_finalRootPosition, _dropDuration, true);
            yield return SwingSettleRoutine(_settleDuration);
        }

        private IEnumerator PlaySnapUnrollRoutine()
        {
            yield return DropToPositionRoutine(_finalRootPosition, _dropDuration * 0.72f, false, 0f);
            yield return UnrollBodyRoutine(_settleDuration * 1.35f, true);
        }

        private IEnumerator DropToPositionRoutine(Vector2 targetPosition, float duration, bool useRattle)
        {
            yield return DropToPositionRoutine(targetPosition, duration, useRattle, 1f);
        }

        private IEnumerator DropToPositionRoutine(Vector2 targetPosition, float duration, bool useRattle, float maxUnrollRate)
        {
            Vector2 startPosition = _finalRootPosition + Vector2.up * _startYOffset;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float rate = Mathf.Clamp01(elapsed / duration);
                float curveRate = _dropCurve.Evaluate(rate);
                Vector2 basePosition = Vector2.LerpUnclamped(startPosition, targetPosition, curveRate);
                float unrollRate = Mathf.Min(maxUnrollRate, GetDelayedUnrollRate(rate));

                _signRoot.anchoredPosition = basePosition + GetRattleOffset(rate, useRattle);
                _signRoot.localRotation = GetRattleRotation(rate, useRattle);
                _signRoot.localScale = GetImpactScale(0f);
                ApplyScrollUnroll(unrollRate);
                ApplyCanvasAlpha(rate);

                yield return null;
            }
        }

        private IEnumerator SettleToFinalRoutine(float duration, bool useRattle)
        {
            Vector2 startPosition = _signRoot.anchoredPosition;
            Quaternion startRotation = _signRoot.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float rate = Mathf.Clamp01(elapsed / duration);
                float curveRate = _settleCurve.Evaluate(rate);
                float damping = 1f - curveRate;

                _signRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, _finalRootPosition, curveRate) + GetLandingRattleOffset(rate, damping, useRattle);
                if (useRattle)
                {
                    _signRoot.localRotation = GetRattleRotation(1f + rate, useRattle);
                }
                else
                {
                    _signRoot.localRotation = Quaternion.Lerp(startRotation, _finalRootRotation, curveRate);
                }
                _signRoot.localScale = GetImpactScale(Mathf.Sin(rate * Mathf.PI) * damping);
                ApplyScrollUnroll(GetDelayedUnrollRate(rate));
                ApplyCanvasAlpha(1f);

                yield return null;
            }
        }

        private IEnumerator SwingSettleRoutine(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float rate = Mathf.Clamp01(elapsed / duration);
                float damping = 1f - rate;
                float angle = Mathf.Sin(rate * Mathf.PI * 4f) * _rattleAngle * damping;

                _signRoot.anchoredPosition = _finalRootPosition + GetLandingRattleOffset(rate, damping, true);
                _signRoot.localRotation = _finalRootRotation * Quaternion.Euler(0f, 0f, angle);
                _signRoot.localScale = GetImpactScale(Mathf.Sin(rate * Mathf.PI) * damping);
                ApplyScrollUnroll(GetDelayedUnrollRate(rate));

                yield return null;
            }
        }

        private IEnumerator UnrollBodyRoutine(float duration, bool rotateRolls)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float rate = Mathf.Clamp01(elapsed / duration);
                float curveRate = _scaleCurve.Evaluate(rate);

                ApplyScrollUnroll(curveRate, rotateRolls);
                ApplyCanvasAlpha(1f);

                yield return null;
            }
        }

        private IEnumerator PlayDelayedPartsRoutine()
        {
            if (_delayedUiParts == null || _delayedUiParts.Length == 0)
            {
                yield break;
            }

            yield return WaitRoutine(_partsDelay);

            for (int i = 0; i < _delayedUiParts.Length; i++)
            {
                RectTransform part = _delayedUiParts[i];
                if (part != null)
                {
                    bool waitsAfterPop = i < _delayedUiParts.Length - 1;
                    yield return PopPartRoutine(part, i, waitsAfterPop);
                }
            }
        }

        private IEnumerator PopPartRoutine(RectTransform part, int index, bool waitsAfterPop)
        {
            Vector2 finalPosition = GetFinalPartPosition(index);
            Vector3 finalScale = GetFinalPartScale(index);
            Vector2 startPosition = finalPosition + Vector2.up * 24f;
            float duration = Mathf.Max(MinDuration, _settleDuration * 0.55f);
            float elapsed = 0f;

            part.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float rate = Mathf.Clamp01(elapsed / duration);
                float curveRate = _settleCurve.Evaluate(rate);
                float scaleRate = Mathf.Sin(rate * Mathf.PI);

                part.anchoredPosition = Vector2.LerpUnclamped(startPosition, finalPosition, curveRate);
                part.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, curveRate) + Vector3.one * (scaleRate * 0.08f);

                yield return null;
            }

            part.anchoredPosition = finalPosition;
            part.localScale = finalScale;

            if (waitsAfterPop)
            {
                yield return WaitRoutine(_partsStagger);
            }
        }

        private void CacheFinalState()
        {
            if (_signRoot == null)
            {
                return;
            }

            _finalRootPosition = _signRoot.anchoredPosition;
            _finalRootScale = _signRoot.localScale;
            _finalRootRotation = _signRoot.localRotation;
            if (_scrollBody != null)
            {
                _finalBodyScale = _scrollBody.localScale;
            }
            else
            {
                _finalBodyScale = Vector3.one;
            }

            if (_leftRoll != null)
            {
                _finalLeftRollPosition = _leftRoll.anchoredPosition;
                _finalLeftRollRotation = _leftRoll.localRotation;
            }
            else
            {
                _finalLeftRollPosition = Vector2.zero;
                _finalLeftRollRotation = Quaternion.identity;
            }

            if (_rightRoll != null)
            {
                _finalRightRollPosition = _rightRoll.anchoredPosition;
                _finalRightRollRotation = _rightRoll.localRotation;
            }
            else
            {
                _finalRightRollPosition = Vector2.zero;
                _finalRightRollRotation = Quaternion.identity;
            }
            CacheDelayedPartsState();
            _hasCachedFinalState = true;
        }

        private void EnsureFinalStateCached()
        {
            if (!_hasCachedFinalState)
            {
                CacheFinalState();
            }
        }

        private void CacheDelayedPartsState()
        {
            int count = 0;
            if (_delayedUiParts != null)
            {
                count = _delayedUiParts.Length;
            }

            _finalPartsPositions = new Vector2[count];
            _finalPartsScales = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                RectTransform part = _delayedUiParts[i];
                if (part != null)
                {
                    _finalPartsPositions[i] = part.anchoredPosition;
                    _finalPartsScales[i] = part.localScale;
                }
                else
                {
                    _finalPartsPositions[i] = Vector2.zero;
                    _finalPartsScales[i] = Vector3.one;
                }
            }
        }

        private void ResetToStartState()
        {
            if (_signRoot == null)
            {
                return;
            }

            _signRoot.anchoredPosition = _finalRootPosition + Vector2.up * _startYOffset;
            _signRoot.localScale = _finalRootScale;
            _signRoot.localRotation = _finalRootRotation;
            ApplyScrollUnroll(0f, true);
            ApplyCanvasAlpha(0f);
            ResetDelayedParts();
        }

        private void ResetDelayedParts()
        {
            if (_delayedUiParts == null)
            {
                return;
            }

            for (int i = 0; i < _delayedUiParts.Length; i++)
            {
                RectTransform part = _delayedUiParts[i];
                if (part == null)
                {
                    continue;
                }

                part.anchoredPosition = GetFinalPartPosition(i) + Vector2.up * 24f;
                part.localScale = Vector3.zero;
                part.gameObject.SetActive(false);
            }
        }

        private void ApplyFinalState()
        {
            _signRoot.anchoredPosition = _finalRootPosition;
            _signRoot.localScale = _finalRootScale;
            _signRoot.localRotation = _finalRootRotation;

            if (_scrollBody != null)
            {
                _scrollBody.localScale = _finalBodyScale;
            }

            if (_leftRoll != null)
            {
                _leftRoll.anchoredPosition = _finalLeftRollPosition;
                _leftRoll.localRotation = _finalLeftRollRotation;
            }

            if (_rightRoll != null)
            {
                _rightRoll.anchoredPosition = _finalRightRollPosition;
                _rightRoll.localRotation = _finalRightRollRotation;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            ApplyFinalPartsState();
        }

        private void ApplyFinalPartsState()
        {
            if (_delayedUiParts == null)
            {
                return;
            }

            for (int i = 0; i < _delayedUiParts.Length; i++)
            {
                RectTransform part = _delayedUiParts[i];
                if (part == null)
                {
                    continue;
                }

                part.gameObject.SetActive(true);
                part.anchoredPosition = GetFinalPartPosition(i);
                part.localScale = GetFinalPartScale(i);
            }
        }

        private void ApplyScrollUnroll(float rate, bool rotateRolls = true)
        {
            float clampedRate = Mathf.Clamp01(rate);
            float scaleRate = _scaleCurve.Evaluate(clampedRate);

            if (_scrollBody != null)
            {
                float scaleY = Mathf.Lerp(_rolledBodyScaleY, _finalBodyScale.y, scaleRate);
                _scrollBody.localScale = new Vector3(_finalBodyScale.x, scaleY, _finalBodyScale.z);
            }

            if (_leftRoll != null)
            {
                _leftRoll.anchoredPosition = _finalLeftRollPosition + Vector2.right * (_rollSlideDistance * (1f - scaleRate));
                if (rotateRolls)
                {
                    _leftRoll.localRotation = _finalLeftRollRotation * Quaternion.Euler(0f, 0f, _rollRotation * (1f - scaleRate));
                }
                else
                {
                    _leftRoll.localRotation = _finalLeftRollRotation;
                }
            }

            if (_rightRoll != null)
            {
                _rightRoll.anchoredPosition = _finalRightRollPosition + Vector2.left * (_rollSlideDistance * (1f - scaleRate));
                if (rotateRolls)
                {
                    _rightRoll.localRotation = _finalRightRollRotation * Quaternion.Euler(0f, 0f, -_rollRotation * (1f - scaleRate));
                }
                else
                {
                    _rightRoll.localRotation = _finalRightRollRotation;
                }
            }
        }

        private Vector2 GetRattleOffset(float rate, bool enabled)
        {
            if (!enabled)
            {
                return Vector2.zero;
            }

            float step = Mathf.Floor(rate * _dropDuration / _rattleInterval);
            float xSign = 1f;
            if (step % 2f < 1f)
            {
                xSign = -1f;
            }

            float ySign = 1f;
            if (step % 3f < 1f)
            {
                ySign = -1f;
            }
            float damping = 1f - Mathf.Clamp01(rate * 0.65f);
            return new Vector2(xSign * _rattleX, ySign * _rattleY) * damping;
        }

        private Vector2 GetLandingRattleOffset(float rate, float damping, bool enabled)
        {
            if (!enabled)
            {
                return Vector2.zero;
            }

            float shake = Mathf.Sin(rate * Mathf.PI * 10f);
            return new Vector2(shake * _rattleX * 0.35f, -Mathf.Abs(shake) * _rattleY * 0.25f) * damping;
        }

        private Vector3 GetImpactScale(float impactRate)
        {
            float clampedImpactRate = Mathf.Clamp01(impactRate);
            float squash = _impactSquash * clampedImpactRate;
            return new Vector3(
                _finalRootScale.x * (1f + squash),
                _finalRootScale.y * (1f - squash),
                _finalRootScale.z);
        }

        private float GetDelayedUnrollRate(float rate)
        {
            float clampedRate = Mathf.Clamp01(rate);
            if (_unrollDelayRate <= 0f)
            {
                return clampedRate;
            }

            return Mathf.InverseLerp(_unrollDelayRate, 1f, clampedRate);
        }

        private void ApplyCanvasAlpha(float rate)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            float clampedRate = Mathf.Clamp01(rate);
            if (clampedRate <= 0f)
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                _canvasGroup.alpha = Mathf.Lerp(0.72f, 1f, clampedRate);
            }
            _canvasGroup.interactable = rate >= 1f;
            _canvasGroup.blocksRaycasts = rate >= 1f;
        }

        private void WarnOptionalReferencesIfNeeded()
        {
            if (!_warnOptionalReferenceMissing || _hasWarnedOptionalReferences)
            {
                return;
            }

            if (_scrollBody == null)
            {
                Debug.LogWarning($"{nameof(TitleSignboardAnimator)}: 本体が未設定", this);
            }

            if (_leftRoll == null || _rightRoll == null)
            {
                Debug.LogWarning($"{nameof(TitleSignboardAnimator)}: 左右ロールのどちらかが未設定", this);
            }

            _hasWarnedOptionalReferences = true;
        }

        private Quaternion GetRattleRotation(float rate, bool enabled)
        {
            if (!enabled)
            {
                return _finalRootRotation;
            }

            float step = Mathf.Floor(rate * _dropDuration / _rattleInterval);
            float sign = 1f;
            if (step % 2f < 1f)
            {
                sign = -1f;
            }
            float damping = Mathf.Clamp01(1f - Mathf.Abs(rate - 1f));
            return _finalRootRotation * Quaternion.Euler(0f, 0f, sign * _rattleAngle * damping);
        }

        private float GetDeltaTime()
        {
            if (_useUnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }

            return Time.deltaTime;
        }

        private IEnumerator WaitRoutine(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            if (_useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(seconds);
            }
            else
            {
                yield return new WaitForSeconds(seconds);
            }
        }

        private Vector2 GetFinalPartPosition(int index)
        {
            if (_finalPartsPositions == null || index < 0 || index >= _finalPartsPositions.Length)
            {
                return Vector2.zero;
            }

            return _finalPartsPositions[index];
        }

        private Vector3 GetFinalPartScale(int index)
        {
            if (_finalPartsScales == null || index < 0 || index >= _finalPartsScales.Length)
            {
                return Vector3.one;
            }

            return _finalPartsScales[index];
        }

        private static AnimationCurve EnsureCurve(AnimationCurve curve)
        {
            if (curve != null && curve.length > 0)
            {
                return curve;
            }

            return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }
}
