// ================================================================================
// File         : HitPopupPresenter.cs
// Author       : Yamauchi Akira & Iwai Shogo
//
// Description  : 敵毎の個別位置にコンボHit数ポップアップを表示するプレゼンテーター
// Updated      : 2026-07-16 (FeedbackScaleとの競合を乗算合成で解決、無駄なコメントの整理)
// ================================================================================

using UnityEngine;
using Game.Core.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Presentation.UI.Combo;

namespace Game.Presentation.UI
{
    public class HitPopupPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popupPrefab;
        [SerializeField] private Transform _popupContainer;

        [Header("--- 画面内クランプ設定 ---")]
        [SerializeField] private float _screenPadding = 60f;

        [Header("--- 文字被り自動回避設定 ---")]
        [SerializeField] private float _overlapOverlapThreshold = 75f;
        [SerializeField] private float _pushUpStrength = 45f;

        [Header("--- カートゥーン文字サイズ比率設定 ---")]
        [SerializeField] private int _numberSizePercent = 140;
        [SerializeField] private int _textSizePercent = 75;

        [Header("--- ヒット瞬間パンチ設定 ---")]
        [SerializeField] private float _hitPunchScaleAmount = 1.5f;
        [SerializeField] private float _hitPunchReturnSpeed = 12f;

        private readonly Dictionary<string, int> _enemyLocalComboTracker = new Dictionary<string, int>();
        private readonly Dictionary<string, GameObject> _activePopupsTracker = new Dictionary<string, GameObject>();

        private float _currentGlobalDurationRatio = 1f;
        private Canvas _cachedCanvas;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnDefeated);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnDefeated);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void OnHitBatch(EnemyHitBatchEvent ev)
        {
            if (_popupPrefab == null || _popupContainer == null || string.IsNullOrEmpty(ev.EnemyId)) return;

            if (!_enemyLocalComboTracker.ContainsKey(ev.EnemyId)) _enemyLocalComboTracker[ev.EnemyId] = 0;
            _enemyLocalComboTracker[ev.EnemyId] += ev.HitCount;
            int localHitCount = _enemyLocalComboTracker[ev.EnemyId];

            if (_cachedCanvas == null) _cachedCanvas = _popupContainer.GetComponentInParent<Canvas>();

            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.6f), Random.Range(1.3f, 1.7f), Random.Range(-0.2f, 0.2f));
            Vector3 targetWorldSpacePos = ev.HitPosition + randomOffset;
            Vector3 finalCalculatedPosition = targetWorldSpacePos;

            if (_cachedCanvas != null && _cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay && Camera.main != null)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetWorldSpacePos);
                screenPoint.x = Mathf.Clamp(screenPoint.x, _screenPadding, Screen.width - _screenPadding);
                screenPoint.y = Mathf.Clamp(screenPoint.y, _screenPadding, Screen.height - _screenPadding);
                finalCalculatedPosition = screenPoint;
            }

            ResolveUiOverlaps(finalCalculatedPosition);

            GameObject popupObj = null;
            bool isNewSpawn = false;

            if (_activePopupsTracker.TryGetValue(ev.EnemyId, out GameObject existingPopup) && existingPopup != null)
            {
                popupObj = existingPopup;
                popupObj.transform.position = finalCalculatedPosition;

                if (popupObj.TryGetComponent<ActivePopupFeedbackBridge>(out var oldBridge))
                {
                    oldBridge.ResetLifetime();
                    oldBridge.ApplyScalePunch(_hitPunchScaleAmount, _hitPunchReturnSpeed);
                }
            }
            else
            {
                _popupPrefab.SetActive(false);
                popupObj = Instantiate(_popupPrefab, _popupContainer);
                _popupPrefab.SetActive(true);

                popupObj.transform.position = finalCalculatedPosition;
                popupObj.SetActive(true);

                _activePopupsTracker[ev.EnemyId] = popupObj;
                isNewSpawn = true;
            }

            RectTransform rectTransform = popupObj.GetComponent<RectTransform>();
            TMPro.TMP_Text tmpText = popupObj.GetComponentInChildren<TMPro.TMP_Text>();
            Text unityText = popupObj.GetComponentInChildren<Text>();

            if (tmpText != null) tmpText.text = $"<size={_numberSizePercent}%>{localHitCount}</size> <size={_textSizePercent}%>Hits!</size>";
            else if (unityText != null) unityText.text = $"{localHitCount} Hits!";

            var feedbacks = popupObj.GetComponentsInChildren<IComboFeedback>(true);
            foreach (var fb in feedbacks)
            {
                fb.Initialize(rectTransform, tmpText);
                fb.OnUpdate(localHitCount, _currentGlobalDurationRatio);
            }

            if (!popupObj.TryGetComponent<ActivePopupFeedbackBridge>(out var bridge))
            {
                bridge = popupObj.AddComponent<ActivePopupFeedbackBridge>();
            }

            bridge.Setup(feedbacks, localHitCount, ev.EnemyId, this, 1.5f);

            if (isNewSpawn) bridge.ApplyScalePunch(_hitPunchScaleAmount, _hitPunchReturnSpeed);
        }

        private void ResolveUiOverlaps(Vector3 newPosition)
        {
            foreach (var kvp in _activePopupsTracker)
            {
                GameObject existingPopup = kvp.Value;
                if (existingPopup == null) continue;

                if (Vector3.Distance(newPosition, existingPopup.transform.position) < _overlapOverlapThreshold)
                {
                    Vector3 currentPos = existingPopup.transform.position;
                    currentPos.y += _pushUpStrength;
                    currentPos.x += Random.Range(-15f, 15f);

                    if (_cachedCanvas != null && _cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        currentPos.y = Mathf.Clamp(currentPos.y, _screenPadding, Screen.height - _screenPadding);
                        currentPos.x = Mathf.Clamp(currentPos.x, _screenPadding, Screen.width - _screenPadding);
                    }
                    existingPopup.transform.position = currentPos;
                }
            }
        }

        private void OnDefeated(EnemyDefeatedEvent ev)
        {
            if (string.IsNullOrEmpty(ev.EnemyId)) return;

            if (_enemyLocalComboTracker.ContainsKey(ev.EnemyId)) _enemyLocalComboTracker.Remove(ev.EnemyId);

            if (_activePopupsTracker.TryGetValue(ev.EnemyId, out GameObject existingPopup) && existingPopup != null)
            {
                if (existingPopup.TryGetComponent<ActivePopupFeedbackBridge>(out var bridge))
                {
                    bridge.DisconnectTargetOnDeath();
                }
                _activePopupsTracker.Remove(ev.EnemyId);
            }
        }

        private void OnComboChanged(ComboChangedEvent ev)
        {
            _currentGlobalDurationRatio = ev.DurationRatio;
            if (ev.CurrentCombo == 0)
            {
                _enemyLocalComboTracker.Clear();
                _activePopupsTracker.Clear();
            }
        }

        public void NotifyPopupDestroyed(string enemyId)
        {
            if (_activePopupsTracker.ContainsKey(enemyId)) _activePopupsTracker.Remove(enemyId);
        }

        public float GetCurrentDurationRatio() => _currentGlobalDurationRatio;
    }

    public class ActivePopupFeedbackBridge : MonoBehaviour
    {
        private IComboFeedback[] _myFeedbacks;
        private int _myHitCount;
        private string _myEnemyId;
        private HitPopupPresenter _presenter;
        private float _maxLifetime;
        private float _currentLifetimeTimer;
        private bool _isTargetDead = false;

        private float _currentPunchScale = 1.0f;
        private float _punchReturnSpeed = 12f;

        public float CurrentPunchScaleMultiplier => _currentPunchScale;

        public void Setup(IComboFeedback[] feedbacks, int hitCount, string enemyId, HitPopupPresenter presenter, float lifetime)
        {
            _myFeedbacks = feedbacks;
            _myHitCount = hitCount;
            _myEnemyId = enemyId;
            _presenter = presenter;
            _maxLifetime = lifetime;
            _isTargetDead = false;
            ResetLifetime();
        }

        public void ResetLifetime() => _currentLifetimeTimer = _maxLifetime;

        public void ApplyScalePunch(float punchAmount, float returnSpeed)
        {
            _currentPunchScale = punchAmount;
            _punchReturnSpeed = returnSpeed;
        }

        public void DisconnectTargetOnDeath() => _isTargetDead = true;

        private void Update()
        {
            if (_presenter == null) return;

            _currentLifetimeTimer -= Time.unscaledDeltaTime;
            if (_currentLifetimeTimer <= 0f)
            {
                _presenter.NotifyPopupDestroyed(_myEnemyId);
                Destroy(gameObject);
                return;
            }

            // スケールラープ減算のみを行い、ルート側の絶対値上書きを廃止
            if (_currentPunchScale > 1.0f)
            {
                _currentPunchScale = Mathf.Lerp(_currentPunchScale, 1.0f, Time.unscaledDeltaTime * _punchReturnSpeed);
            }

            if (_myFeedbacks == null) return;
            float currentRatio = _presenter.GetCurrentDurationRatio();

            foreach (var fb in _myFeedbacks)
            {
                if (fb != null) fb.OnUpdate(_myHitCount, currentRatio);
            }
        }
    }
}
