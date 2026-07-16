// ------------------------------------------------------------
// File     : BossDownPresentationController.cs
// Summary  : ボスのダウンアニメーション、エフェクト、落下演出を管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ダウン演出の設定値はBossDownPresentationDataから受け取る。
// - ダウン演出全体の時間はBossAngryBiteDataから受け取る。
// - ボス戦のフェーズ遷移はBossBattleControllerへ任せる。
// - このクラスはダウン演出が完了するまでIEnumeratorを返す。
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Enemy.Boss;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスがアングリバイトに失敗してダウンした際の演出を管理する。
    ///
    /// このクラスが担当するもの:
    /// ・ダウンアニメーションの再生
    /// ・ダウン、怒り、咆哮エフェクトの生成
    /// ・一定時間経過後のボス落下
    /// ・落下中のボスの傾き
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossDownPresentationController : MonoBehaviour
    {
        [Header("--- 参照 ---")]

        [SerializeField]
        [Tooltip("ボスのAnimatorとMotionRootを管理するコンポーネント")]
        private BossAnimationController _animationController;

        [SerializeField]
        [Tooltip("ダウンエフェクトを配置する基準Transform")]
        private Transform _effectAnchor;


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// 現在生成しているダウンエフェクトのGameObjectを保持するリスト
        /// </summary>
        private readonly List<GameObject> _spawnedEffects = new List<GameObject>();

        /// <summary>
        /// 現在のダウン演出に対してキャンセル要求が出ているかどうかを示すフラグ
        /// </summary>
        private bool _isCancellationRequested;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 現在ダウン演出が再生中かどうかを示すフラグ
        /// </summary>
        public bool IsPlaying { get; private set; }


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// このコンポーネントがResetされたときに参照を取得する
        /// </summary>
        private void Reset()
        {
            FindReferences();
        }

        /// <summary>
        /// このコンポーネントのInspector上で値が変更されたときに参照を取得する
        /// </summary>
        private void OnValidate()
        {
            FindReferences();
        }


        /// <summary>
        /// このコンポーネントがAwakeされたときに参照を取得する
        /// </summary>
        private void Awake()
        {
            FindReferences();
        }


        /// <summary>
        /// このコンポーネントが有効化されたときに参照を取得する
        /// </summary>
        private void OnDisable()
        {
            CancelPresentation();
        }


        // 参照取得
        // ------------------------------------------------------------

        /// <summary>
        /// ダウン演出に必要なコンポーネントとTransformを取得する
        /// </summary>
        private void FindReferences()
        {
            if (_animationController == null)
            {
                _animationController = GetComponent<BossAnimationController>();
            }

            if (_effectAnchor == null && _animationController != null)
            {
                _effectAnchor = _animationController.MotionRoot;
            }
        }


        // ダウン演出
        // ------------------------------------------------------------

        /// <summary>
        /// ダウンアニメーション、各エフェクト、落下演出を再生するコルーチン
        /// </summary>
        /// <param name="biteData">アングリバイトとダウン時間の設定</param>
        /// <param name="presentationData">ダウン演出の設定</param>
        /// <returns>ダウン演出が終了するまで待機するIEnumerator</returns>
        public IEnumerator PlayPresentation(BossAngryBiteData biteData, BossDownPresentationData presentationData)
        {
            if (biteData == null || presentationData == null)
            {
                Debug.LogWarning($"[{nameof(BossDownPresentationController)}] ダウン演出の設定がありません。");
                yield break;
            }

            if (_animationController == null || _animationController.MotionRoot == null)
            {
                Debug.LogWarning($"[{nameof(BossDownPresentationController)}] MotionRootが設定されていません。");
                yield break;
            }

            // 前回のダウン演出が残っている場合に備えて初期化する
            ClearSpawnedEffects();

            _isCancellationRequested = false;
            IsPlaying = true;

            // ダウン専用アニメーションを現在位置から再生する
            bool didStartDownAnimation = _animationController.PlayDown(biteData);

            if (!didStartDownAnimation)
            {
                // ダウンアニメーションが設定されていない場合は、ダウン演出をスキップする
                _animationController.PauseCurrentPose();
            }

            Transform motionRoot = _animationController.MotionRoot;

            // 落下開始時の位置と向きを保存する
            Vector3 fallStartLocalPosition = motionRoot.localPosition;
            Quaternion fallStartLocalRotation = motionRoot.localRotation;

            float totalDuration = Mathf.Max(0f, biteData.DownDuration);
            float fallStartTime = Mathf.Clamp(presentationData.FallStartDelay, 0f, totalDuration);

            bool didSpawnDownEffect = false;
            bool didSpawnAngerEffect = false;
            bool didSpawnRoarEffect = false;

            // カメラシェイク要求を複数回答発行しないようにするためのフラグ
            bool didRequestCameraShake = false;

            float elapsedTime = 0f;

            // ダウン演出の再生ループ
            while (elapsedTime < totalDuration && !_isCancellationRequested)
            {
                // 設定された開始時間へ到達したエフェクトを1回だけ生成する
                TrySpawnEffect(presentationData.DownEffectPrefab, presentationData.DownEffectLocalOffset, presentationData.DownEffectStartDelay, elapsedTime, ref didSpawnDownEffect);

                TrySpawnEffect(presentationData.AngerEffectPrefab, presentationData.AngerEffectLocalOffset, presentationData.AngerEffectStartDelay, elapsedTime, ref didSpawnAngerEffect);

                TrySpawnEffect(presentationData.RoarEffectPrefab, presentationData.RoarEffectLocalOffset, presentationData.RoarEffectStartDelay, elapsedTime, ref didSpawnRoarEffect);

                // SOで設定された開始時間に到達したら、カメラ側へシェイク要求を1回だけ送る
                TryRequestCameraShake(presentationData, elapsedTime, ref didRequestCameraShake);

                UpdateFallPose(motionRoot, fallStartLocalPosition, fallStartLocalRotation, presentationData, fallStartTime, totalDuration, elapsedTime);

                elapsedTime += Time.deltaTime;

                yield return null;
            }

            // 演出終了時に生成したエフェクトを破棄する
            if (!_isCancellationRequested)
            {
                TrySpawnEffect(presentationData.DownEffectPrefab, presentationData.DownEffectLocalOffset, presentationData.DownEffectStartDelay, elapsedTime, ref didSpawnDownEffect);

                TrySpawnEffect(presentationData.AngerEffectPrefab, presentationData.AngerEffectLocalOffset, presentationData.AngerEffectStartDelay, elapsedTime, ref didSpawnAngerEffect);

                TrySpawnEffect(presentationData.RoarEffectPrefab, presentationData.RoarEffectLocalOffset, presentationData.RoarEffectStartDelay, elapsedTime, ref didSpawnRoarEffect);

                UpdateFallPose(motionRoot, fallStartLocalPosition, fallStartLocalRotation, presentationData, fallStartTime, totalDuration, elapsedTime);
            }

            ClearSpawnedEffects();

            IsPlaying = false;
            _isCancellationRequested = false;

        }


        /// <summary>
        /// 設定時間へ到達したエフェクトを1回だけ生成する
        /// </summary>
        /// <param name="effectPrefab">エフェクトのプレファブ        </param>
        /// <param name="localOffset" >エフェクトのローカルオフセット</param>
        /// <param name="startDelay"  >生成開始までの遅延時間        </param>
        /// <param name="elapsedTime" >経過時間                      </param>
        /// <param name="didSpawn"    >生成済みフラグ                </param>
        private void TrySpawnEffect(GameObject effectPrefab, Vector3 localOffset, float startDelay, float elapsedTime, ref bool didSpawn)
        {
            if (didSpawn || elapsedTime < Mathf.Max(0f, startDelay))
            {
                return;
            }

            // Prefabが未設定の場合も、毎フレーム確認しないよう処理済みにする
            didSpawn = true;

            if (effectPrefab == null)
            {
                return;
            }

            Transform effectAnchor = _effectAnchor != null ? _effectAnchor : transform;

            // Prefab側の回転とScaleは維持し、生成位置だけ設定値を使用する
            GameObject effectInstance = Instantiate(effectPrefab, effectAnchor, false);
            effectInstance.transform.localPosition = localOffset;

            _spawnedEffects.Add(effectInstance);
        }


        private static void TryRequestCameraShake(BossDownPresentationData presentationData, float elapsedTime, ref bool didRequest)
        {
            if(didRequest || presentationData == null)
            {
                return;
            }

            // 無効設定の場合は、この演出中に再確認しない
            if (!presentationData.EnableCameraShake)
            {
                didRequest = true;
                return;
            }

            float startDelay = Mathf.Max(0f, presentationData.CameraShakeStartDelay);

            if (elapsedTime < startDelay)
            {
                return;
            }

            didRequest = true;

            // カメラの実装を直接参照せず、イベントを通じてシェイク要求を送る
            EventBus.Publish(new CameraShakeRequestedEvent(presentationData.CameraShakeDuration, presentationData.CameraShakePositionStrength, presentationData.CameraShakeRotationStrength, presentationData.CameraShakeFrequency));
        }



        /// <summary>
        /// Fall Start Delay経過後のボスの位置と傾きを更新する
        /// </summary>
        /// <param name="motionRoot">         モーションの支点</param>
        /// <param name="startLocalPosition"> 開始位置        </param>
        /// <param name="startLocalRotation"> 開始回転        </param>
        /// <param name="presentationData">   ダウン演出データ</param>
        /// <param name="fallStartTime">      落下開始時間    </param>
        /// <param name="totalDuration">      総演出時間      </param>
        /// <param name="elapsedTime">        経過時間        </param>
        private static void UpdateFallPose(Transform motionRoot, Vector3 startLocalPosition, Quaternion startLocalRotation, BossDownPresentationData presentationData, float fallStartTime, float totalDuration, float elapsedTime)
        {
            if (motionRoot == null || elapsedTime < fallStartTime)
            {
                return;
            }

            // 落下演出の進行度を計算
            float fallDuration = Mathf.Max(0.01f, totalDuration - fallStartTime);
            float normalizedTime = Mathf.Clamp01((elapsedTime - fallStartTime) / fallDuration);
            AnimationCurve fallCurve = presentationData.FallCurve;
            float fallProgress = fallCurve != null ? fallCurve.Evaluate(normalizedTime) : normalizedTime;


            // 開始位置からローカルYの下方向へ落下させる
            motionRoot.localPosition = startLocalPosition + Vector3.down * presentationData.FallDistance * fallProgress;

            // 落下の進行度に応じて少しずつ傾ける
            Vector3 fallEulerAngles = presentationData.FallEulerAngles * fallProgress;

            motionRoot.localRotation = startLocalRotation * Quaternion.Euler(fallEulerAngles);

            // MotionRootと一緒に動くColliderの位置を物理判定へ反映させる
            Physics.SyncTransforms();
        }


        // 演出処理
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のダウン演出をキャンセルし、生成したエフェクトを破棄
        /// </summary>
        public void CancelPresentation()
        {
            _isCancellationRequested = true;
            IsPlaying = false;

            ClearSpawnedEffects();
        }


        /// <summary>
        /// このクラスが生成したエフェクトをすべて破棄する
        /// </summary>
        private void ClearSpawnedEffects()
        {
            for (int i = 0; i < _spawnedEffects.Count; i++)
            {
                GameObject effectInstance = _spawnedEffects[i];

                if (effectInstance != null)
                {
                    Destroy(effectInstance);
                }
            }

            _spawnedEffects.Clear();
        }
    }
}
