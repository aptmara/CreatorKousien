// ------------------------------------------------------------
// File		: BocchaIntroPresenter.cs
// Summary	: キング・ボッチャの登場演出
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - 分身フェーズと同じく煙でボンボン移動する感じ！
// - 勝手に作ってるから没になる可能性あり(´;ω;｀)
// - ステップはリストにするから変更可能！
// - カメラの揺れ追加
// ------------------------------------------------------------
using System;
using System.Collections;
using Game.Gameplay.Stage;
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Cameras;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// キング・ボッチャの登場演出の1ステップ
    /// </summary>
    [Serializable]
    public class BocchaIntroStep
    {
        [SerializeField]
        [Tooltip("Inspectorで識別するための名前")]
        private string _stepName = "Step";

        [SerializeField]
        [Tooltip("フィールド中心からの横方向オフセット。マイナスで左")]
        private float _offsetX = -18.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("煙を出してから姿を見せるまでの時間")]
        private float _appearDelay = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("姿を見せてから消えるまでの時間")]
        private float _stayDuration = 0.6f;

        [SerializeField]
        [Tooltip("ONならここで留まる")]
        private bool _isFinal = false;

        [SerializeField]
        [Min(0f)]
        [Tooltip("出現した瞬間のカメラの揺れの強さ。0で揺らさない")]
        private float _appearShakeStrength = 0.2f;


        [SerializeField]
        [Min(0f)]
        [Tooltip("消えてから次のステップへ移るまでの待ち時間")]
        private float _hiddenDuration = 0.4f;


        // --- 公開プロパティ---
        public string StepName => _stepName;
        public float OffsetX => _offsetX;
        public float AppearDelay => _appearDelay;
        public float StayDuration => _stayDuration;
        public bool IsFinal => _isFinal;
        public float AppearShakeStrength => _appearShakeStrength;
        public float HiddenDuration => _hiddenDuration;
    }



    /// <summary>
    /// キング・ボッチャの登場演出
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaIntroPresenter : MonoBehaviour, IBossIntroPresentation
    {
        [Header("==== 手順 ====")]

        [SerializeField]
        [Tooltip("上から順に実行する。最後のステップのIs Finalを必ずONにすること")]
        private BocchaIntroStep[] _steps = new BocchaIntroStep[0];

        [SerializeField]
        [Min(0f)]
        [Tooltip("最初のステップに入る前の待ち時間")]
        private float _startDelay = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("最後に姿を見せてから戦闘開始までの間")]
        private float _finishDelay = 0.5f;


        [Header("==== 演出 ====")]

        [SerializeField]
        [Tooltip("出現・消滅で出す煙")]
        private GameObject _smokeVfxPrefab;

        [SerializeField]
        [Tooltip("VFXの位置補正。ルート原点と見た目のズレを吸収する")]
        private Vector3 _vfxOffset = new Vector3(0.0f, -4.89f, 0.0f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;

        [SerializeField]
        [Tooltip("最後に姿を見せた時のAnimatorトリガー名。空なら何もしない")]
        private string _appearAnimTrigger = string.Empty;

        [SerializeField]
        [Tooltip("アニメーター。未設定なら同じGameObjectから取得する")]
        private Animator _animator;



        [Header("==== カメラ ====")]

        [SerializeField]
        [Tooltip("動かすカメラ。未設定ならMainCameraを使う")]
        private Camera _targetCamera;

        [SerializeField]
        [Tooltip("カメラリグ。未設定なら自動で探す")]
        private CameraRigController _cameraRigController;

        [SerializeField]
        [Tooltip("登場中に寄せるカメラ設定")]
        private StaticCameraConfig _introCameraConfig;

        [SerializeField]
        [Tooltip("戦闘中のカメラ設定")]
        private StaticCameraConfig _battleCameraConfig;

        [SerializeField]
        [Tooltip("使用する投影モード")]
        private CameraRigController.ProjectionMode _cameraProjectionMode = CameraRigController.ProjectionMode.Perspective;

        [SerializeField]
        [Min(0f)]
        [Tooltip("登場カメラへ寄せる時間")]
        private float _introCameraBlendDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("戦闘カメラへ引く時間")]
        private float _battleCameraBlendDuration = 1.0f;

        [SerializeField]
        [Tooltip("カメラのブレンド曲線")]
        private AnimationCurve _cameraBlendCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [SerializeField]
        [Tooltip("登場中に画面のフチを赤くするかどうか")]
        private bool _playEdgeWarning = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("出現時の揺れの長さ")]
        private float _appearShakeDuration = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("出現時の揺れの周波数")]
        private float _appearShakeFrequency = 30.0f;


        // --- ランタイム変数 ---
        private Renderer[] _renderers;
        private Collider[] _colliders;

        private Vector3 _normalCameraPosition;
        private Quaternion _normalCameraRotation;
        private float _normalCameraFieldOfView;
        private float _normalCameraOrthographicSize;
        private bool _hasSavedNormalCameraPose;
        private BocchaFeverBody _feverBody;


        // 演出中に中断されても無敵が残らないようにする
        private void OnDisable() => _feverBody?.SetInvincible(false);


        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_cameraRigController == null && _targetCamera != null)
            {
                _cameraRigController = _targetCamera.GetComponentInParent<CameraRigController>();
            }

            if (_cameraRigController == null)
            {
                _cameraRigController = FindFirstObjectByType<CameraRigController>();
            }

            if (_feverBody == null)
            {
                _feverBody = GetComponent<BocchaFeverBody>();
            }
        }


        /// <summary>
        /// 登場演出を再生する
        /// </summary>
        public IEnumerator PlayIntro()
        {
            // 演出が終わるまで姿を隠す
            SetVisible(false);

            // 演出中に殴られてキャパが溜まらないようにする
            _feverBody?.SetInvincible(true);

            if (_playEdgeWarning)
            {
                EventBus.Publish(new BossIntroWarningStartedEvent());
            }

            // 通常のカメラ追従を止めて、元の状態を覚えておく
            _cameraRigController?.SetCinematicModeActive(true);

            SaveNormalCameraPose();

            // 登場用の画角へ寄せる
            yield return StartCoroutine(BlendCameraTo(_introCameraConfig, _introCameraBlendDuration));

            if (_startDelay > 0.0f)
            {
                yield return new WaitForSeconds(_startDelay);
            }

            foreach (BocchaIntroStep step in _steps)
            {
                if (step == null)
                {
                    continue;
                }

                yield return StartCoroutine(PlayStep(step));
            }

            // ステップが1つもない場合の保険
            SetVisible(true);

            if (_finishDelay > 0.0f)
            {
                yield return new WaitForSeconds(_finishDelay);
            }

            if (_playEdgeWarning)
            {
                EventBus.Publish(new BossIntroWarningEndedEvent());
            }

            FinishCamera();

            // ここから戦闘開始なので、無敵を解除する
            _feverBody?.SetInvincible(false);



        }



        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// ステップを順に実行する
        /// </summary>
        /// <param name="step">オープニングの演出</param>
        private IEnumerator PlayStep(BocchaIntroStep step)
        {
            // ステップの位置を求める
            Vector3 stepPosition = ResolveStepPosition(step.OffsetX);

            transform.position = stepPosition;

            // 煙を出す
            PlayVfx(stepPosition);


            // 出現の瞬間にカメラを揺らす
            PlayAppearShake(step.AppearShakeStrength);

            if (step.AppearDelay > 0.0f)
            {
                yield return new WaitForSeconds(step.AppearDelay);
            }

            // 姿を見せる
            SetVisible(true);



            // 最後のステップならアニメーターにトリガーを送る
            if (step.IsFinal && _animator != null && !string.IsNullOrEmpty(_appearAnimTrigger))
            {
                _animator.SetTrigger(_appearAnimTrigger);
            }

            if (step.StayDuration > 0.0f)
            {
                yield return new WaitForSeconds(step.StayDuration);
            }

            // 最後のステップはそのまま残る
            if (step.IsFinal)
                yield break;

            PlayVfx(transform.position);

            SetVisible(false);



            // 何もない時間を挟んでから次へ
            if (step.HiddenDuration > 0.0f)
            {
                yield return new WaitForSeconds(step.HiddenDuration);
            }
        }


        /// <summary>
        /// フィールド中心を基準にステップの位置を求める
        /// </summary>
        /// <param name="offsetX">中心からの横方向オフセット</param>
        /// <returns>与えを返す</returns>
        private Vector3 ResolveStepPosition(float offsetX)
        {
            Vector3 right = FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;

            // 高さは今の値を保つ
            Vector3 center = FieldContext.IsReady ? new Vector3(FieldContext.Center.x, transform.position.y, transform.position.z) : transform.position;

            return center + right * offsetX;
        }


        /// <summary>
        /// 姿と当たり判定をまとめて切り替える
        /// </summary>
        /// <param name="isVisible">表示するかどうか</param>
        private void SetVisible(bool isVisible)
        {
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            _colliders ??= GetComponentsInChildren<Collider>(true);

            // レンダラーをON/OFFして姿を見せる/隠す
            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = isVisible;
                }
            }

            // 演出中に殴られてキャパが溜まらないようにする
            foreach (Collider targetCollider in _colliders)
            {
                if (targetCollider != null)
                {
                    targetCollider.enabled = isVisible;
                }
            }
        }


        /// <summary>
        /// 姿を見せるか隠すか
        /// </summary>
        /// <param name="position">生成位置</param>
        private void PlayVfx(Vector3 position)
        {
            if (_smokeVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(_smokeVfxPrefab, position + _vfxOffset, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }



        // --- カメラ関連 ---

        /// <summary>
        /// 通常のカメラの位置・回転・画角を保存する
        /// </summary>
        private void SaveNormalCameraPose()
        {
            if (_hasSavedNormalCameraPose || _targetCamera == null)
            {
                return;
            }

            Transform cameraTransform = _targetCamera.transform;

            // カメラの位置・回転・画角を保存しておく
            _normalCameraPosition = cameraTransform.position;
            _normalCameraRotation = cameraTransform.rotation;
            _normalCameraFieldOfView = _targetCamera.fieldOfView;
            _normalCameraOrthographicSize = _targetCamera.orthographicSize;
            _hasSavedNormalCameraPose = true;
        }


        /// <summary>
        /// 指定した設定へカメラをブレンドさせる
        /// </summary>
        /// <param name="config">目標のカメラ設定</param>
        /// <param name="duration">ブレンドにかける時間</param>
        private IEnumerator BlendCameraTo(StaticCameraConfig config, float duration)
        {
            if (config == null || _targetCamera == null)
            {
                yield break;
            }


            StaticCameraConfig.ProjectionSettings settings = config.GetSettings(_cameraProjectionMode);
            Transform cameraTransform = _targetCamera.transform;

            // 現在のカメラの位置・回転・画角を保存しておく
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float startFieldOfView = _targetCamera.fieldOfView;
            float startOrthographicSize = _targetCamera.orthographicSize;

            Vector3 targetPosition = settings.Position;
            Quaternion targetRotation = Quaternion.Euler(settings.Rotation);

            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float t = _cameraBlendCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

                // LerpUnclampedを使うことで、曲線の値が0未満や1を超える場合でも補間できる
                cameraTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                cameraTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, t);
                _targetCamera.fieldOfView = Mathf.LerpUnclamped(startFieldOfView, settings.FieldOfView, t);
                _targetCamera.orthographicSize = Mathf.LerpUnclamped(startOrthographicSize, settings.OrthographicSize, t);

                elapsed += Time.deltaTime;

                yield return null;
            }

            // 最終的にターゲットの値を確実に設定する
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
            _targetCamera.fieldOfView = settings.FieldOfView;
            _targetCamera.orthographicSize = settings.OrthographicSize;
        }


        /// <summary>
        /// カメラの後始末
        /// </summary>
        private void FinishCamera()
        {
            // 戦闘用の画角をしている間は、リグの追従を戻さず固定したままにする
            if (_battleCameraConfig != null)
            {
                return;
            }

            if (_targetCamera != null && _hasSavedNormalCameraPose)
            {
                Transform cameraTransform = _targetCamera.transform;

                cameraTransform.position = _normalCameraPosition;
                cameraTransform.rotation = _normalCameraRotation;
                _targetCamera.fieldOfView = _normalCameraFieldOfView;
                _targetCamera.orthographicSize = _normalCameraOrthographicSize;
            }

            _cameraRigController?.SetCinematicModeActive(false);

            _hasSavedNormalCameraPose = false;
        }


        /// <summary>
        /// 出現時のカメラの揺れを再生する
        /// </summary>
        /// <param name="strength">強さ</param>
        private void PlayAppearShake(float strength)
        {
            if (strength <= 0.0f || _appearShakeDuration <= 0.0f)
            {
                return;
            }

            EventBus.Publish(new CameraShakeRequestedEvent(_appearShakeDuration, strength, strength, _appearShakeFrequency));
        }
    }
}
