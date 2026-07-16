// ------------------------------------------------------------
// File     : BossAnimationController.cs
// Summary  : ボスの開始姿勢とAnimator再生を管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 戦闘の状態遷移はBossBattleControllerへ任せる。
// - このクラスは開始位置・向き・再生速度・アニメーション再生だけを担当する。
// - イバラタックルの弧を描く移動はアニメーション側で行う。
// - InspectorにはAnimatorステートのフルパス名を設定する。
// ------------------------------------------------------------
using Game.Data.Enemy.Boss;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスのAnimatorと、アニメーション開始時の姿勢を管理する
    ///
    /// このクラスが担当するもの:
    /// ・左右に応じたイバラタックル開始位置の設定
    /// ・左右に応じたイバラタックル開始方向の設定
    /// ・アングリバイト開始位置と方向の設定
    /// ・Animatorステートの再生
    /// ・Animator再生速度の変更
    /// ・再生中アニメーションの完了判定
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossAnimationController : MonoBehaviour
    {
        [Header("--- 参照 ---")]
        [SerializeField]
        [Tooltip("Animatorコンポーネント")]
        private Animator _animator;

        [SerializeField]
        [Tooltip("開始位置と向きを変更するためのTransform")]
        private Transform _motionRoot;

        [SerializeField]
        [Tooltip("Animatorと同じGameObjectにある、Root Motionを物理座標へ反映するRigidbody")]
        private Rigidbody _animatorRigidbody;


        [Header("--- Animatorレイヤー ---")]

        [SerializeField]
        [Min(0)]
        [Tooltip("ボスアニメーションを再生するAnimatorレイヤー番号")]
        private int _baseLayerIndex;


        [Header("--- Animatorステート名---")]

        [SerializeField]
        [Tooltip("イバラタックル開始アニメーションのフルパス名")]
        private string _thornAttackStateName = "Base Layer.ThornAttack";

        [SerializeField]
        [Tooltip("口を開けるアニメーションのフルパス名")]
        private string _angryBiteOpenStateName = "Base Layer.AngryBiteOpen";

        [SerializeField]
        [Tooltip("口を閉じるアニメーションのフルパス名")]
        private string _angryBiteCloseStateName = "Base Layer.AngryBiteClose";

        [SerializeField]
        [Tooltip("ダウンアニメーションのフルパス名")]
        private string _downStateName = "Base Layer.Down";


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// イバラタックルのAnimatorステートのハッシュ値
        /// </summary>
        private int _thornAttackStateHash;

        /// <summary>
        /// 口を開けるアニメーションのAnimatorステートのハッシュ値
        /// </summary>
        private int _angryBiteOpenStateHash;

        /// <summary>
        /// 口を閉じるアニメーションのAnimatorステートのハッシュ値
        /// </summary>
        private int _angryBiteCloseStateHash;

        /// <summary>
        /// ダウンアニメーションのAnimatorステートのハッシュ値
        /// </summary>
        private int _downStateHash;

        /// <summary>
        /// 現在再生中のAnimatorステートのハッシュ値
        /// </summary>
        private int _currentStateHash;

        /// <summary>
        /// 現在アニメーションが再生中かどうか
        /// </summary>
        private bool _isPlaying;

        /// <summary>
        /// Root Motionが適用される前のAnimatorの初期ローカル位置
        /// </summary>
        private Vector3 _animatorInitialLocalPosition;

        /// <summary>
        /// Root Motionが適用される前のAnimatorの初期ローカル回転
        /// </summary>
        private Quaternion _animatorInitialLocalRotation;

        /// <summary>
        /// Animatorの初期姿勢を保存済みかどうか
        /// </summary>
        private bool _hasCachedAnimatorInitialPose;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 現在アニメーションが再生中かどうかを取得する
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Animatorコンポーネントを取得する
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// 開始位置と向きを変更するためのTransformを取得する
        /// </summary>
        public Transform MotionRoot => _motionRoot;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時にAnimatorコンポーネントと開始位置変更用Transformを取得して、Animatorステート名からハッシュ値をキャッシュする
        /// </summary>
        private void Reset()
        {
            FindReferences();
            CacheStateHashes();
        }


        /// <summary>
        /// Inspectorで設定された値を検証し、Animatorコンポーネントと開始位置変更用Transformを取得して、Animatorステート名からハッシュ値をキャッシュする
        /// </summary>
        private void OnValidate()
        {
            _baseLayerIndex = Mathf.Max(0, _baseLayerIndex);

            FindReferences();
            CacheStateHashes();
        }


        /// <summary>
        /// Animatorコンポーネントと開始位置変更用Transformを取得し、Animatorステート名からハッシュ値をキャッシュする
        /// </summary>
        private void Awake()
        {
            FindReferences();
            CacheStateHashes();

            if (_animator == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] Animatorコンポーネントが設定されていません。");

                enabled = false;
                return;
            }

            if (_motionRoot == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] 開始位置変更用Transformが設定されていません。");

                enabled = false;
                return;
            }

            // Root Motionで移動する前のAnimatorの初期姿勢を保存する
            CacheAnimatorInitialPose();
        }


        /// <summary>
        /// コンポーネントが無効化されたときに、現在のアニメーション監視を解除する
        /// </summary>
        private void OnDisable()
        {
            CancelCurrentAnimation();
        }

        // 参照とHash値のキャッシュ
        // ------------------------------------------------------------

        /// <summary>
        /// Animatorコンポーネントと開始位置変更用Transformを取得する
        /// </summary>
        private void FindReferences()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_motionRoot == null && _animator != null)
            {
                _motionRoot = _animator.transform;
            }

            if (_animatorRigidbody == null && _animator != null)
            {
                _animatorRigidbody = _animator.GetComponent<Rigidbody>();
            }
        }


        /// <summary>
        /// Animatorステート名からハッシュ値を取得してキャッシュする
        /// </summary>
        private void CacheStateHashes()
        {
            _thornAttackStateHash = GetStateHash(_thornAttackStateName);

            _angryBiteOpenStateHash = GetStateHash(_angryBiteOpenStateName);

            _angryBiteCloseStateHash = GetStateHash(_angryBiteCloseStateName);

            _downStateHash = GetStateHash(_downStateName);
        }


        /// <summary>
        /// Animatorステート名からハッシュ値を取得する
        /// </summary>
        /// <param name="stateName">ステート名</param>
        /// <returns>アニメーションハッシュ値</returns>
        private static int GetStateHash(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return 0;
            }

            return UnityEngine.Animator.StringToHash(stateName);
        }


        // イバラタックル再生
        // ------------------------------------------------------------

        /// <summary>
        /// イバラタックルの開始位置を設定して、アニメーションを再生する
        /// </summary>
        /// <param name="stepData">イバラタックルの段階設定</param>
        /// <param name="attackSide">ボスが登場する方向</param>
        /// <returns>再生を開始できた場合はtrue</returns>
        public bool PlayThornAttack(BossThornAttackStepData stepData, BossAttackSide attackSide)
        {
            // --- 引数チェック ---
            if (stepData == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] PlayThornAttack: stepData が null です。");
                return false;
            }

            // --- 開始位置を設定 ---
            Vector3 startLocalPosition = attackSide == BossAttackSide.Left ? stepData.LeftStartLocalPosition : stepData.RightStartLocalPosition;

            Vector3 startEulerAngles = attackSide == BossAttackSide.Left ? stepData.LeftStartEulerAngles : stepData.RightStartEulerAngles;

            // --- アニメーション再生 ---
            return TryPlayAnimation(_thornAttackStateHash, _thornAttackStateName, stepData.AnimationSpeed, true, startLocalPosition, startEulerAngles);
        }



        // アングリバイト再生
        // ------------------------------------------------------------

        /// <summary>
        /// アングリバイトの開始位置を設定して、口を開けるアニメーションを再生する
        /// </summary>
        /// <param name="biteData">アングリバイト設定</param>
        /// <returns>再生を開始出来たらtrue</returns>
        public bool PlayAngryBiteOpen(BossAngryBiteData biteData)
        {
            if (biteData == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] PlayAngryBiteOpen: biteData が null です。");
                return false;
            }

            return TryPlayAnimation(_angryBiteOpenStateHash, _angryBiteOpenStateName, biteData.OpenAnimationSpeed, true, biteData.StartLocalPosition, biteData.StartEulerAngles);
        }


        /// <summary>
        /// アングリバイトの口を上昇させる位置を更新する
        /// </summary>
        /// <param name="biteData">現在のアングリバイト設定</param>
        /// <param name="normalizedTime">Mouth Open Durationに対する 0 - 1 の経過割合</param>
        public void UpdateAngryBiteRisePosition(BossAngryBiteData biteData, float normalizedTime)
        {
            if (biteData == null || _motionRoot == null)
            {
                return;
            }

            // カーブへ渡す時間は0～1に制限する
            float safeNormalizedTime = Mathf.Clamp01(normalizedTime);

            AnimationCurve riseCurve = biteData.RiseCurve;

            // カーブが未設定の場合は、一定速度で上昇させる
            float moveProgress = riseCurve != null ? riseCurve.Evaluate(safeNormalizedTime) : safeNormalizedTime;

            // 開始位置から防衛バリア付近の位置まで補間する
            _motionRoot.localPosition = Vector3.LerpUnclamped(biteData.StartLocalPosition, biteData.BarrierReachLocalPosition, moveProgress);

            // 動いている口Colliderの位置も物理判定へ反映する
            Physics.SyncTransforms();
        }

        /// <summary>
        /// アングリバイト失敗時の下降位置を更新する
        /// </summary>
        /// <param name="biteData">現在のアングリバイト設定</param>
        /// <param name="normalizedTime">Mouth Open Durationに対する 0 - 1 の経過割合</param>
        public void UpdateAngryBiteFailureRetreatPosition(BossAngryBiteData biteData, float normalizedTime)
        {
            if (biteData == null)
            {
                return;
            }

            // 下降の進行度を0～1に制限する
            float retreatProgress = Mathf.Clamp01(normalizedTime);

            // 上昇処理へ逆向き進行度を渡す
            float reversedRiseProgress = 1f - retreatProgress;

            UpdateAngryBiteRisePosition(biteData, reversedRiseProgress);
        }


        /// <summary>
        /// アングリバイトの口を閉じるアニメーションを再生する
        /// </summary>
        /// <param name="biteData">アングリバイト設定</param>
        /// <returns>再生を開始出来たらtrue</returns>
        public bool PlayAngryBiteClose(BossAngryBiteData biteData)
        {
            if (biteData == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] PlayAngryBiteClose: biteData が null です。");
                return false;
            }

            // 口を閉じるアニメーションは現在位置を維持する！！
            return TryPlayAnimation(_angryBiteCloseStateHash, _angryBiteCloseStateName, biteData.CloseAnimationSpeed, false, Vector3.zero, Vector3.zero);
        }


        /// <summary>
        /// ボスがダウンするアニメーションを再生する
        /// </summary>
        /// <param name="biteData">アングリバイト設定</param>
        /// <returns>再生を開始出来たらtrue</returns>
        public bool PlayDown(BossAngryBiteData biteData)
        {
            if (biteData == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] PlayDown: biteData が null です。");
                return false;
            }

            return TryPlayAnimation(_downStateHash, _downStateName, biteData.DownAnimationSpeed, false, Vector3.zero, Vector3.zero);
        }


        // アニメーション再生
        // ------------------------------------------------------------

        /// <summary>
        /// Animatorステートを検証し、開始姿勢と再生速度を設定してアニメーションを再生する
        /// </summary>
        /// <param name="stateHash">再生するステートのハッシュ値</param>
        /// <param name="stateName">再生するステートの名前</param>
        /// <param name="playbackSpeed">再生速度</param>
        /// <param name="applyStartPose">開始姿勢を適用するかどうか</param>
        /// <param name="startLocalPosition">開始ローカル位置</param>
        /// <param name="startEulerAngles">開始ローカル回転</param>
        /// <returns>再生出来たらtrueを返すぜよ^^</returns>
        private bool TryPlayAnimation(int stateHash, string stateName, float playbackSpeed, bool applyStartPose, Vector3 startLocalPosition, Vector3 startEulerAngles)
        {
            if (!CanPlayState(stateHash, stateName))
            {
                return false;
            }

            // --- 姿勢を適用する ---
            if (applyStartPose)
            {
                ApplyStartPose(startLocalPosition, startEulerAngles);
            }

            // --- 再生速度を設定する ---
            float safePlaybackSpeed = Mathf.Max(0.01f, playbackSpeed);

            _animator.speed = safePlaybackSpeed;

            _currentStateHash = stateHash;
            _isPlaying = true;

            // --- Animatorステートを再生する ---
            _animator.Play(stateHash, _baseLayerIndex, 0.0f);

            return true;
        }


        /// <summary>
        /// Animatorステートを再生できるか確認する
        /// </summary>
        /// <param name="stateHash">確認するステートのハッシュ値</param>
        /// <param name="stateName">確認するステートの名前</param>
        /// <returns></returns>
        private bool CanPlayState(int stateHash, string stateName)
        {
            // --- それぞれの条件を満たしているか確認する ---

            if (_animator == null)
            {
                return false;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] RuntimeAnimatorController が設定されていません。");

                return false;
            }

            if (_baseLayerIndex < 0 || _baseLayerIndex >= _animator.layerCount)
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] ボスアニメーション用のレイヤーが設定されていません。");

                return false;
            }

            if (stateHash == 0 || string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] Animatorステート名が設定されていません。");
                return false;
            }

            if (!_animator.HasState(_baseLayerIndex, stateHash))
            {
                Debug.LogWarning($"[{nameof(BossAnimationController)}] Animatorステートが存在しません。ステート名: {stateName}");
                return false;
            }

            return true;
        }

        // 開始姿勢
        // ------------------------------------------------------------


        public void ResetForPhaseStart()
        {
            // 現在のアニメーションをキャンセルする
            CancelCurrentAnimation();

            if (_animator == null || _motionRoot == null)
            {
                return;
            }

            // Animator内部のステート、ボーン姿勢、Root Motionによる位置・回転の変化をリセットする
            _animator.Rebind();

            // Rebindした初期姿勢を保存する
            _animator.Update(0f);

            // Animator本体へ残っているRoot Motionの位置・回転の変化をリセットする
            ResetAnimatorRootMotionOffset();

            // 開始位置を設定する親Transformも初期状態へ戻す
            _motionRoot.localPosition = Vector3.zero;
            _motionRoot.localRotation = Quaternion.identity;

            ResetAnimatorRigidbodyPose();
        }


        /// <summary>
        /// Animatorの初期姿勢をキャッシュする
        /// </summary>
        private void CacheAnimatorInitialPose()
        {
            if (_animator == null)
            {
                return;
            }

            Transform animatorTransform = _animator.transform;

            _animatorInitialLocalPosition = animatorTransform.localPosition;
            _animatorInitialLocalRotation = animatorTransform.localRotation;
            _hasCachedAnimatorInitialPose = true;
        }


        /// <summary>
        /// AnimatorのRoot Motionによる位置と回転の変化をリセットして、初期姿勢に戻す
        /// </summary>
        private void ResetAnimatorRootMotionOffset()
        {
            if (_animator == null || !_hasCachedAnimatorInitialPose)
            {
                return;
            }

            Transform animatorTransform = _animator.transform;

            animatorTransform.localPosition = _animatorInitialLocalPosition;
            animatorTransform.localRotation = _animatorInitialLocalRotation;
        }


        private void ResetAnimatorRigidbodyPose()
        {
            if (_animator == null)
            {
                return;
            }

            // Rigidbodyがない構成でも
            if (_animatorRigidbody == null)
            {
                Physics.SyncTransforms();
                return;
            }

            Transform animatorTransform = _animator.transform;

            // Interpolateが前回の物理姿勢を表示しないよう、一度補間を解除する
            RigidbodyInterpolation previousInterpolation = _animatorRigidbody.interpolation;
            _animatorRigidbody.interpolation = RigidbodyInterpolation.None;

            // リセット済みTransformのワールド座標をRigidbodyへ反映する
            _animatorRigidbody.position = animatorTransform.position;
            _animatorRigidbody.rotation = animatorTransform.rotation;

            Physics.SyncTransforms();

            // Inspectorで設定されていた補間モードを復元する
            _animatorRigidbody.interpolation = previousInterpolation;
        }


        /// <summary>
        /// アニメーション再生前にボスモデルの開始位置と向きを設定する
        /// </summary>
        /// <param name="localPosition">開始ローカル位置</param>
        /// <param name="eulerAngles">開始ローカル回転</param>
        private void ApplyStartPose(Vector3 localPosition, Vector3 eulerAngles)
        {
            if(_motionRoot == null)
            {
                return;
            }

            ResetAnimatorRootMotionOffset();

            _motionRoot.localPosition = localPosition;
            _motionRoot.localRotation = Quaternion.Euler(eulerAngles);

            ResetAnimatorRigidbodyPose();

            Physics.SyncTransforms();
        }



        // 完了判定
        // ------------------------------------------------------------

        /// <summary>
        /// 現在監視しているアニメーションが1回分終了したか確認する
        /// </summary>
        /// <returns>終了済み、または監視中でない場合はtrue</returns>
        public bool IsCurrentAnimationFinished()
        {
            if (!_isPlaying)
            {
                return true;
            }

            if (_animator == null)
            {
                _isPlaying = false;
                return true;
            }

            if (_animator.IsInTransition(_baseLayerIndex))
            {
                return false;
            }

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_baseLayerIndex);

            // Animator.Playの反映前、または別ステートの場合はまだ完了扱いにしない
            if (stateInfo.fullPathHash != _currentStateHash)
            {
                return false;
            }

            // normalizedTimeが1.0以上になったら完了扱いにする
            if (stateInfo.normalizedTime < 1.0f)
            {
                return false;
            }

            _isPlaying = false;
            return true;
        }


        /// <summary>
        /// 現在のアニメーション完了監視を解除する
        /// </summary>
        public void CancelCurrentAnimation()
        {
            _isPlaying = false;
            _currentStateHash = 0;

            if (_animator != null)
            {
                _animator.speed = 1.0f;
            }
        }


        /// <summary>
        /// 現在のアニメーションを停止して、口HPを削り切った瞬間の姿勢と位置で停止する
        /// </summary>
        public void PauseCurrentPose()
        {
            _isPlaying = false;
            _currentStateHash = 0;

            if (_animator == null)
            {
                return;
            }

            // 口HPを削り切った瞬間の姿勢と位置で停止する
            _animator.speed = 0.0f;
        }
    }
}
