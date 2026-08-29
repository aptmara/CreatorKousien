// ------------------------------------------------------------
// File		: EnemyCameraShakePresenter.cs
// Summary	: 敵に攻撃が当たったときのカメラシェイクを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-08
//
// Notes	:
// - 6/8 ベース作成
// ------------------------------------------------------------
using UnityEngine;
using Game.Core.Events;
using System.Collections;
using Unity.VisualScripting;

namespace Game.Presentation.CameraFeedback
{
    [DefaultExecutionOrder(100)] // 他のカメラフィードバックよりも後に実行されるようにする
    public sealed class EnemyCameraShakePresenter : MonoBehaviour
    {
        // シェイクの設定
        [System.Serializable]
        private sealed class ShakeSettings
        {
            [Header("シェイクのON / OFF")]
            [SerializeField, Tooltip("シェイクを有効にするかどうか")]           private bool _enable = true;

            [Header("揺れのパラメーター設定")]
            [SerializeField, Tooltip("シェイクの持続時間"),       Min(0.01f)]   private float _duration = 0.12f;
            [SerializeField, Tooltip("シェイクの強さ"),           Min(0f)]      private float _positionStrength = 0.08f;
            [SerializeField, Tooltip("シェイクの回転の強さ"),     Min(0f)]      private float _rotationStrength = 1.2f;
            [SerializeField, Tooltip("シェイクの振動数"),         Min(1f)]      private float _frequency = 35f;

            [Header("連続発火制御")]
            [SerializeField, Tooltip("シェイクのクールダウン"),   Min(0f)]      private float _cooldown = 0.03f;

            [Header("複数Hit補正")]
            [SerializeField, Tooltip("ヒットのカウントボーナス"), Min(0f)]      private float _hitCountBonus = 0.08f;
            [SerializeField, Tooltip("シェイクの最大倍率"),       Min(1f)]      private float _maxMultiplier = 1.8f;

            public ShakeSettings()
            {
            }

            public ShakeSettings(float duration, float positionStrength, float rotationStrength, float frequency)
            {
                _duration = duration;
                _positionStrength = positionStrength;
                _rotationStrength = rotationStrength;
                _frequency = frequency;
            }

            public bool Enable => _enable;
            public float Duration => _duration;
            public float PositionStrength => _positionStrength;
            public float RotationStrength => _rotationStrength;
            public float Frequency => _frequency;
            public float Cooldown => _cooldown;

            /// <summary>
            /// ヒットのカウントに応じたシェイクの倍率を計算する
            /// </summary>
            /// <param name="hitCount">ヒットのカウント</param>
            /// <returns>カメラシェイクの倍率</returns>
            public float GetMultiplier(int hitCount)
            {
                float multiplier = 1f + Mathf.Max(0, hitCount - 1) * _hitCountBonus;
                return Mathf.Min(multiplier, _maxMultiplier);
            }
        }


        [Header("参照")]
        [Tooltip("揺らす対象。未設定ならMainCameraを自動取得します。")]
        [SerializeField] private Transform _shakeTarget;

        [Header("敵ヒット時の揺れの設定")]
        [SerializeField] private ShakeSettings _enemyHitShake = new ShakeSettings();

        [Header("敵撃破時の揺れ")]
        [SerializeField] private ShakeSettings _enemyDefeatShake = new ShakeSettings();

        [Header("防衛ライン破壊時の強い揺れ")]
        [SerializeField] private ShakeSettings _defenseLineBreakPrimaryShake = new ShakeSettings(0.22f, 0.16f, 2.8f, 32.0f);

        [Header("防衛ライン破壊時の余震")]
        [SerializeField] private ShakeSettings _defenseLineBreakSecondaryShake = new ShakeSettings(0.18f, 0.06f, 1.3f, 22.0f);
        [SerializeField, Min(0.0f)] private float _defenseLineBreakSecondaryDelay = 0.16f;

        private float _remainingTime;                                       // シェイクの残り時間
        private float _totalTime;                                           // シェイクの総時間
        private float _positionStrength;                                    // シェイクの位置の強さ
        private float _rotationStrength;                                    // シェイクの回転の強さ
        private float _frequency;                                           // シェイクの振動数
        private float _noiseSeed;                                           // シェイクのノイズシード

        private float _nextHitShakeTime;                                    // 次のヒットシェイクが発生可能な時間
        private float _nextDefeatShakeTime;                                 // 次の撃破シェイクが発生可能な時間
        private float _nextDefenseLineBreakPrimaryShakeTime;
        private float _nextDefenseLineBreakSecondaryShakeTime;
        private Coroutine _defenseLineBreakCoroutine;

        private bool _hasAppliedOffset;                                     // シェイクのオフセットが適用されたかどうか
        private Vector3 _lastBaseLocalPosition;                             // シェイクのオフセットを適用する前のローカル位置
        private Quaternion _lastBaseLocalRotation = Quaternion.identity;    // シェイクのオフセットを適用する前のローカル回転
        private Vector3 _lastPositionOffset;                                // 前フレームの位置のオフセット
        private Quaternion _lastRotationOffset = Quaternion.identity;       // 前フレームの回転のオフセット

        /// <summary>
        /// 初期化処理。シェイクの対象が未設定の場合はMainCameraを自動取得します。
        /// </summary>
        private void Awake()
        {
            if (_shakeTarget == null && Camera.main != null)
            {
                _shakeTarget = Camera.main.transform;
            }

            _noiseSeed = Random.value * 1000f; // ランダムなノイズシードを生成
        }

        /// <summary>
        /// 敵に攻撃が当たったときのイベントを購読
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<DefLineBreakReactionEvent>(OnDefenseLineBreak);

            EventBus.Subscribe<CameraShakeRequestedEvent>(OnCameraShakeRequested);
        }

        /// <summary>
        /// 敵に攻撃が当たったときのイベントを購読解除
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnDefenseLineBreak);

            EventBus.Unsubscribe<CameraShakeRequestedEvent>(OnCameraShakeRequested);

            if (_defenseLineBreakCoroutine != null)
            {
                StopCoroutine(_defenseLineBreakCoroutine);
                _defenseLineBreakCoroutine = null;
            }

            RestoreOffsetIfNeeded();
        }


        /// <summary>
        /// シェイクの状態を更新し、シェイクのオフセットを適用
        /// </summary>
        private void LateUpdate()
        {
            if (_shakeTarget == null)
                return;

            ReadBaseLocalTransform(out Vector3 basePosition, out Quaternion baseRotation);

            if (_remainingTime <= 0f)
            {
                ApplyOffset(basePosition, baseRotation, Vector3.zero, Quaternion.identity);
                _hasAppliedOffset = false;
                return;
            }

            _remainingTime -= Time.deltaTime;

            // 後半ほど揺れを弱くする
            float t = 1f - Mathf.Clamp01(_remainingTime / _totalTime);
            float power = 1f - t;
            power *= power;

            // ノイズを使用して位置と回転のオフセットを計算
            float noiseTime = Time.time * _frequency;

            Vector3 positionOffset = new Vector3(Noise(noiseTime, 0f), Noise(noiseTime, 10f), 0f ) * (_positionStrength * power);

            Quaternion rotationOffset = Quaternion.Euler(
                Noise(noiseTime, 20f) * _rotationStrength * power,
                Noise(noiseTime, 30f) * _rotationStrength * power,
                Noise(noiseTime, 40f) * _rotationStrength * power
            );

            // 計算したオフセットを適用
            ApplyOffset(basePosition, baseRotation, positionOffset, rotationOffset);
        }


        /// <summary>
        /// 敵に攻撃が当たったときのイベントハンドラー。シェイクを開始します。
        /// </summary>
        /// <param name="ev"></param>
        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            float multiplier = _enemyHitShake.GetMultiplier(ev.HitCount);
            TryStartShake(_enemyHitShake, multiplier, ref _nextHitShakeTime);
        }


        /// <summary>
        /// 敵が撃破されたときのイベントハンドラー。シェイクを開始します。
        /// </summary>
        /// <param name="ev">イベント</param>
        private void OnEnemyDefeated(EnemyDefeatedEvent ev)
        {
            TryStartShake(_enemyDefeatShake, 1f, ref _nextDefeatShakeTime);
        }


        private void OnCameraShakeRequested(CameraShakeRequestedEvent ev)
        {
            StartShake(ev.Duration, ev.PositionStrength, ev.RotationStrength, ev.Frequency);
        }


        private void OnDefenseLineBreak(DefLineBreakReactionEvent ev)
        {
            if (_defenseLineBreakCoroutine != null)
            {
                StopCoroutine(_defenseLineBreakCoroutine);
            }

            _defenseLineBreakCoroutine = StartCoroutine(PlayDefenseLineBreakShake());
        }

        private IEnumerator PlayDefenseLineBreakShake()
        {
            TryStartShake(
                _defenseLineBreakPrimaryShake,
                1.0f,
                ref _nextDefenseLineBreakPrimaryShakeTime);

            if (_defenseLineBreakSecondaryDelay > 0.0f)
            {
                yield return new WaitForSeconds(_defenseLineBreakSecondaryDelay);
            }

            TryStartShake(
                _defenseLineBreakSecondaryShake,
                1.0f,
                ref _nextDefenseLineBreakSecondaryShakeTime);
            _defenseLineBreakCoroutine = null;
        }


        /// <summary>
        /// シェイクを開始するための共通処理。シェイクの設定を引数に取ります。
        /// </summary>
        /// <param name="duration">継続時間</param>
        /// <param name="positionStrength">位置の強さ</param>
        /// <param name="rotationStrength">回転の強さ</param>
        /// <param name="frequency">周波数</param>
        private void StartShake(float duration, float positionStrength, float rotationStrength, float frequency)
        {
            // durationが0以下の場合は最小値を設定してクラッシュを防ぐ
            float safeDuration = Mathf.Max(0.01f, duration);

            _totalTime = safeDuration;
            _remainingTime = safeDuration;
            _positionStrength = Mathf.Max(0f, positionStrength);
            _rotationStrength = Mathf.Max(0f, rotationStrength);
            _frequency = Mathf.Max(1f, frequency);
        }


        /// <summary>
        /// シェイクを開始するための共通処理。シェイクの設定と倍率、シェイクが発生可能な時間を引数に取ります。
        /// </summary>
        /// <param name="settings">シェイクの設定</param>
        /// <param name="multiplier">シェイクの倍率</param>
        /// <param name="nextTime">次にシェイクが発生可能な時間</param>
        private void TryStartShake(ShakeSettings settings, float multiplier, ref float nextTime)
        {
            if (!settings.Enable || Time.time < nextTime)
                return;

            nextTime = Time.time + settings.Cooldown;

            StartShake(settings.Duration, settings.PositionStrength * multiplier, settings.RotationStrength * multiplier, settings.Frequency);
        }


        /// <summary>
        /// ノイズ関数を使用してシェイクのオフセットを計算します。
        /// </summary>
        /// <param name="time">時間</param>
        /// <param name="offset">オフセット</param>
        /// <returns>ノイズ値</returns>
        private float Noise(float time, float offset)
        {
            return Mathf.PerlinNoise(_noiseSeed + offset, time) * 2f - 1f; // -1から1の範囲に変換
        }


        /// <summary>
        /// ベースのローカル位置とローカル回転を読み取る
        /// </summary>
        /// <param name="basePosition">ベースの位置</param>
        /// <param name="baseRotation">ベースの回転</param>
        private void ReadBaseLocalTransform(out Vector3 basePosition, out Quaternion baseRotation)
        {
            basePosition = _shakeTarget.localPosition;
            baseRotation = _shakeTarget.localRotation;

            if (!_hasAppliedOffset)
                return;

            Vector3 expectedPosition = _lastBaseLocalPosition + _lastPositionOffset;
            Quaternion expectedRotation = _lastBaseLocalRotation * _lastRotationOffset;

            if ((basePosition - expectedPosition).sqrMagnitude < 0.0001f)
            {
                basePosition = _lastBaseLocalPosition;
            }

            if (Quaternion.Angle(baseRotation, expectedRotation) < 0.1f)
            {
                baseRotation = _lastBaseLocalRotation;
            }
        }


        /// <summary>
        /// シェイクの状態を更新し、シェイクのオフセットを適用します。
        /// </summary>
        /// <param name="basePosition">基準位置</param>
        /// <param name="baseRotation">基準回転</param>
        /// <param name="positionOffset">位置オフセット</param>
        /// <param name="rotationOffset">回転オフセット</param>
        private void ApplyOffset(Vector3 basePosition, Quaternion baseRotation, Vector3 positionOffset, Quaternion rotationOffset)
        {
            // Nan/Infinityチェック
            if (!IsFinite(positionOffset) || !IsFinite(rotationOffset))
            {
                _remainingTime = 0f; // シェイクを終了させる
                return;
            }

            _shakeTarget.localPosition = basePosition + positionOffset;
            _shakeTarget.localRotation = baseRotation * rotationOffset;

            _lastBaseLocalPosition = basePosition;
            _lastBaseLocalRotation = baseRotation;
            _lastPositionOffset = positionOffset;
            _lastRotationOffset = rotationOffset;
            _hasAppliedOffset = true;
        }


        /// <summary>
        /// Vector3が有限かどうかを確認します。NaNやInfinityが含まれていないかをチェックします。
        /// </summary>
        /// <param name="v">ベクター</param>
        /// <returns>有限かどうか</returns>
        private static bool IsFinite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
                   !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
                   !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        }


        /// <summary>
        /// Quaternionが有限かどうかを確認します。NaNやInfinityが含まれていないかをチェックします。
        /// </summary>
        /// <param name="q">クオータニオン</param>
        /// <returns>有限かどうか</returns>
        private static bool IsFinite(Quaternion q)
        {
            return !float.IsNaN(q.x) && !float.IsInfinity(q.x) &&
                   !float.IsNaN(q.y) && !float.IsInfinity(q.y) &&
                   !float.IsNaN(q.z) && !float.IsInfinity(q.z) &&
                   !float.IsNaN(q.w) && !float.IsInfinity(q.w);
        }


        /// <summary>
        /// シェイクの状態を元に戻す必要があるかどうかを確認し、必要であればシェイクのオフセットを元に戻します。
        /// </summary>
        private void RestoreOffsetIfNeeded()
        {
            if (_shakeTarget == null || !_hasAppliedOffset)
                return;

            _shakeTarget.localPosition = _lastBaseLocalPosition;
            _shakeTarget.localRotation = _lastBaseLocalRotation;
            _hasAppliedOffset = false;
        }


        /// <summary>
        /// 敵に攻撃が当たったときのイベントハンドラー。シェイクを開始します。
        /// </summary>
        [ContextMenu("Test Enemy Hit Shake")]
        private void TestEnemyHitShake()
        {
            TryStartShake(_enemyHitShake, 1f, ref _nextHitShakeTime);
        }


        /// <summary>
        /// 敵が撃破されたときのイベントハンドラー。シェイクを開始します。
        /// </summary>
        [ContextMenu("Test Enemy Defeat Shake")]
        private void TestEnemyDefeatShake()
        {
            TryStartShake(_enemyDefeatShake, 1f, ref _nextDefeatShakeTime);
        }
    }

}
