// ------------------------------------------------------------
// File     : BossBattleController.cs
// Summary  : ボス戦全体の状態と現在フェーズを管理する中央制御クラス
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ボス戦全体の状態と現在フェーズを管理する。
// - 棘1本の処理はBossThornへ任せる。
// - 複数の棘の抽選や復活はBossThornGroupControllerへ任せる。
// - アニメーション再生はBossAnimationControllerへ任せる。
// ------------------------------------------------------------
using System;
using System.Collections;
using Game.Core.Events;
using Game.Data.Enemy.Boss;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボス戦全体の進行を管理する中央制御コンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・ボス個体IDの初期化
    /// ・現在のボス戦状態の管理
    /// ・現在のフェーズ番号とフェーズデータの管理
    /// ・BossThornGroupControllerへのフェーズ開始通知
    /// ・各ボス用コンポーネントへの初期化通知
    /// ・イバラタックルの順番制御
    /// ・アングリバイトの成功／失敗制御
    /// ・ダウン状態と次フェーズへの移行
    /// ・最終フェーズ終了後の勝利通知
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossBattleController : MonoBehaviour
    {
        [Header("--- ボス戦データ ---")]

        [SerializeField]
        [Tooltip("3フェーズ分のボス戦設定を保持するScriptableObject")]
        private BossBattleDataSO _battleData;


        [Header("--- ボス制御コンポーネント ---")]

        [SerializeField]
        [Tooltip("ボスのアニメーションと開始姿勢を管理するコンポーネント")]
        private BossAnimationController _animationController;

        [SerializeField]
        [Tooltip("ボスのダウンアニメーション、エフェクト、落下演出を管理するコンポーネント")]
        private BossDownPresentationController _downPresentationController;

        [SerializeField]
        [Tooltip("ボスの開幕アニメーションとボス戦中の引きカメラを管理するコンポーネント")]
        private BossIntroPresentationController _introPresentationController;

        [SerializeField]
        [Tooltip("複数の棘の抽選・復活・全破壊判定を管理するコンポーネント")]
        private BossThornGroupController _thornGroupController;

        [SerializeField]
        [Tooltip("アングリバイト中の口のHPを管理するコンポーネント")]
        private BossMouthHealth _mouthHealth;

        [SerializeField]
        [Tooltip("口へ入った落とし物のヒット判定を管理するコンポーネント")]
        private BossMouthHitReceiver _mouthHitReceiver;

        [SerializeField]
        [Tooltip("死体蹴り用のヒット受け口（複数可）")]
        private BossCorpseHitReceiver[] _corpseHitReceivers;


        [Header("--- アングリバイト失敗時のカメラシェイク ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("バリアを噛んだときの揺れの継続時間(秒)")]
        private float _biteImpactShakeDuration = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("揺れの位置の強さ(単位: メートル)")]
        private float _biteImpactShakePositionStrength = 0.32f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("揺れの回転の強さ(単位: 度)")]
        private float _biteImpactShakeRotationStrength = 4.5f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("揺れの振動数(単位: Hz)")]
        private float _biteImpactShakeFrequency = 26f;


        [Header("--- イバラタックル警告の画面内判定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("ボスが「画面外」とみなすMotionRootのローカルXの絶対値。これより内側へ来たら「見えた」扱い(待機位置は±35)")]
        private float _bossHiddenLocalX = 25f;

        [SerializeField]
        [Tooltip("ボスが見えたとみなすRootMotionのローカルY")]
        private float _bossVisibleLocalY = -12f;



        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// Wave内でボス個体を一意に識別するためのID
        /// </summary>
        private string _bossInstanceId = string.Empty;

        /// <summary>
        /// 現在のボス戦状態。
        /// </summary>
        private BossBattleState _currentState = BossBattleState.Inactive;

        /// <summary>
        /// 現在実行しているフェーズ番号
        ///
        /// 0 : 第1フェーズ
        /// 1 : 第2フェーズ
        /// 2 : 第3フェーズ
        /// </summary>
        private int _currentPhaseIndex = -1;

        /// <summary>
        /// 現在実行しているフェーズの設定データ
        /// </summary>
        private BossPhaseData _currentPhaseData;

        /// <summary>
        /// ボス個体IDを使用した初期化が完了しているかどうか
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// 現在ボス戦が実行中かどうか
        /// </summary>
        private bool _isBattleRunning;

        /// <summary>
        /// 現在実行しているボス行動のコルーチン
        /// </summary>
        private Coroutine _stateRoutine;

        /// <summary>
        /// 現在実行しているイバラタックルの段階番号
        /// </summary>
        private int _currentThornAttackStepIndex = -1;

        /// <summary>
        /// 次のイバラタックルでボスが登場する方向
        /// </summary>
        private BossAttackSide _nextAttackSide = BossAttackSide.Left;

        /// <summary>
        /// 有効な棘がすべて破壊され、アングリバイトへ移行する必要があるか
        /// </summary>
        private bool _shouldEnterAngryBite;

        /// <summary>
        /// 今回のアングリバイトで、制限時間内に口のHPを0にできたか
        /// </summary>
        private bool _didCurrentAngryBiteSucceed;

        /// <summary>
        /// アングリバイト阻止に成功した回数
        /// </summary>
        private int _successfulDownCount;

        /// <summary>
        /// EnemyDefeatedEventをすでに発行済みかどうか
        /// </summary>
        private bool _hasPublishedDefeatedEvent;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// Wave内で一意に設定されたボス個体ID
        /// </summary>
        public string BossInstanceId => _bossInstanceId;

        /// <summary>
        /// 現在のボス戦状態
        /// </summary>
        public BossBattleState CurrentState => _currentState;

        /// <summary>
        /// 現在のフェーズ番号
        /// </summary>
        public int CurrentPhaseIndex => _currentPhaseIndex;

        /// <summary>
        /// 現在使用しているフェーズデータ
        /// </summary>
        public BossPhaseData CurrentPhaseData => _currentPhaseData;

        /// <summary>
        /// ボス個体IDの初期化が完了しているかどうか
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 現在ボス戦が実行中かどうか
        /// </summary>
        public bool IsBattleRunning => _isBattleRunning;

        /// <summary>
        /// 現在実行しているイバラタックルの段階番号。
        ///
        /// イバラタックル中以外は-1。
        /// </summary>
        public int CurrentThornAttackStepIndex => _currentThornAttackStepIndex;

        /// <summary>
        /// 今回のアングリバイトに成功したかどうか。
        /// </summary>
        public bool DidCurrentAngryBiteSucceed => _didCurrentAngryBiteSucceed;

        /// <summary>
        /// 現在までにアングリバイト阻止に成功した回数
        /// </summary>
        public int SuccessfulDownCount => _successfulDownCount;


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// ボス戦の状態が変更されたときに通知されるイベント
        ///
        /// 引数:
        /// 1. 変更前のボス戦状態
        /// 2. 変更後のボス戦状態
        /// </summary>
        public event Action<BossBattleState, BossBattleState> StateChanged;


        /// <summary>
        /// 新しいフェーズが開始されたときに通知されるイベント
        ///
        /// 引数:
        /// 1. 開始したフェーズ番号
        /// 2. 開始したフェーズの設定データ
        /// </summary>
        public event Action<int, BossPhaseData> PhaseStarted;

        /// <summary>
        /// イバラタックルの各段階が開始されたときに通知されるイベント
        ///
        /// 引数:
        /// 1. 現在のフェーズ番号
        /// 2. イバラタックルの段階番号
        /// 3. ボスが登場する方向
        /// 4. 使用している攻撃段階データ
        /// </summary>
        public event Action<int, int, BossAttackSide, BossThornAttackStepData> ThornAttackStepStarted;


        /// アングリバイト関連のイベント
        /// 引数:
        /// 1. 現在のフェーズ番号
        /// 2. 使用するアングリバイト設定

        /// <summary>
        /// アングリバイトが開始されたときに通知されるイベント
        /// </summary>
        public event Action<int, BossAngryBiteData> AngryBiteStarted;

        /// <summary>
        /// アングリバイトが終了したときに通知されるイベント
        /// </summary>
        public event Action<int, BossAngryBiteData> AngryBiteSucceeded;

        /// <summary>
        /// 制限時間内に口のHPを0にできず、アングリバイトが失敗したときに通知されるイベント
        /// </summary>
        public event Action<int, BossAngryBiteData> AngryBiteFailed;

        /// <summary>
        /// アングリバイトに成功し、ボスがダウン状態に入ったときに通知されるイベント
        ///
        /// 引数:
        /// 1. 現在のフェーズ番号
        /// 2. 使用するアングリバイト設定
        /// 3. 使用するダウン状態の設定
        /// 4. ダウン状態かどうか
        /// </summary>
        public event Action<int, BossAngryBiteData, BossDownPresentationData, bool> DownStarted;

        /// <summary>
        /// ボス戦がすべてのフェーズを終了し、勝利条件を満たしたときに通知されるイベント
        /// </summary>
        public event Action<string> BattleCompleted;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時に、ボス制御に必要なコンポーネントを自動取得する
        /// </summary>
        private void Reset()
        {
            FindReferences();
        }


        /// <summary>
        /// Inspector上で値が変更された際に、未設定のコンポーネントを自動取得する
        /// </summary>
        private void OnValidate()
        {
            FindReferences();
        }


        /// <summary>
        /// 実行開始時に、ボス制御に必要なコンポーネントを取得する
        /// </summary>
        private void Awake()
        {
            FindReferences();
        }


        private void OnEnable()
        {
            FindReferences();

            // 多重購読を防ぐため、いったん解除してから購読する
            if (_thornGroupController != null)
            {
                _thornGroupController.AllActiveThornsBroken -= HandleAllActiveThornsBroken;

                _thornGroupController.AllActiveThornsBroken += HandleAllActiveThornsBroken;
            }
        }


        /// <summary>
        /// コンポーネントが無効化された場合に、実行中のボス戦を停止する
        /// </summary>
        private void OnDisable()
        {
            if(_thornGroupController != null)
            {
                _thornGroupController.AllActiveThornsBroken -= HandleAllActiveThornsBroken;
            }

            if (_isBattleRunning)
            {
                StopBattle();
            }
        }


        // 参照の取得
        // ------------------------------------------------------------

        /// <summary>
        /// ボス制御に必要なコンポーネントを取得
        /// </summary>
        private void FindReferences()
        {
            if (_animationController == null)
            {
                _animationController = GetComponent<BossAnimationController>();
            }

            if (_downPresentationController == null)
            {
                _downPresentationController = GetComponent<BossDownPresentationController>();
            }

            if (_thornGroupController == null)
            {
                _thornGroupController = GetComponent<BossThornGroupController>();
            }

            if (_mouthHealth == null)
            {
                _mouthHealth = GetComponentInChildren<BossMouthHealth>(true);
            }

            if (_mouthHitReceiver == null)
            {
                _mouthHitReceiver = GetComponentInChildren<BossMouthHitReceiver>(true);
            }

            if (_corpseHitReceivers == null || _corpseHitReceivers.Length == 0)
            {
                _corpseHitReceivers = GetComponentsInChildren<BossCorpseHitReceiver>(true);
            }

            if (_introPresentationController == null)
            {
                _introPresentationController = GetComponentInChildren<BossIntroPresentationController>(true);
            }
        }


        // 棘の破壊イベント
        // ------------------------------------------------------------

        /// <summary>
        /// 現在有効な棘がすべて破壊されたとき、イバラタックルを終了するためのフラグを立てる
        /// </summary>
        private void HandleAllActiveThornsBroken()
        {
            if (!_isBattleRunning)
            {
                return;
            }

            if (_currentState != BossBattleState.ThornAttack)
            {
                return;
            }

            _shouldEnterAngryBite = true;
        }


        // 初期化
        // ------------------------------------------------------------

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="bossInstanceId">Wave内でボスを一意に識別するためのID</param>
        /// <returns>成功したらtrue</returns>
        public bool Initialize(string bossInstanceId)
        {
            if (_isBattleRunning)
            {
                Debug.LogWarning($"[{nameof(BossBattleController)}] ボス戦はすでに実行中です。初期化できません。");
                return false;
            }

            if (string.IsNullOrEmpty(bossInstanceId))
            {
                Debug.LogWarning($"[{nameof(BossBattleController)}] ボス個体IDが空です。初期化できません。");
                return false;
            }

            FindReferences();

            if (_thornGroupController == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossBattleControllerコンポーネントが設定されていません。初期化できません。");
                return false;
            }

            if (_mouthHitReceiver == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossBattleControllerコンポーネントが設定されていません。初期化できません。");
                return false;
            }

            _bossInstanceId = bossInstanceId;

            // 棘への攻撃時に、落とし物固有効果を実行する
            _thornGroupController.Initialize(_bossInstanceId);

            _mouthHitReceiver.Initialize(_bossInstanceId);

            foreach (BossCorpseHitReceiver corpseHitReceiver in _corpseHitReceivers)
            {
                if (corpseHitReceiver != null)
                {
                    corpseHitReceiver.Initialize(_bossInstanceId);
                }
            }

            _isInitialized = true;

            return true;
        }


        // ボス戦開始！
        // ------------------------------------------------------------

        /// <summary>
        /// 初期化とボス戦開始をまとめて行う
        /// </summary>
        /// <param name="bossInstanceId">Wave内でボスを一意に識別するためのID</param>
        /// <returns>始められたらtrue</returns>
        public bool StartBattle(string bossInstanceId)
        {
            if (!Initialize(bossInstanceId))
            {
                return false;
            }

            return BeginBattle();
        }


        /// <summary>
        /// 第1フェーズからボス戦を開始する
        /// </summary>
        /// <returns>ボス戦を開始できたらtrue</returns>
        public bool BeginBattle()
        {
            if (_isBattleRunning)
            {
                Debug.LogWarning($"[{nameof(BossBattleController)}] ボス戦はすでに実行中です。");
                return false;
            }

            if (!_isInitialized)
            {
                Debug.LogWarning($"[{nameof(BossBattleController)}] ボス戦は初期化されていません。");
                return false;
            }

            if (!CanBeginBattle())
            {
                Debug.LogWarning($"[{nameof(BossBattleController)}] ボス戦を開始するための条件が揃っていません。");
                return false;
            }

            // --- ボス戦全体の進行状態を初期化 ---
            _successfulDownCount = 0;
            _hasPublishedDefeatedEvent = false;

            _currentPhaseIndex = -1;
            _currentPhaseData = null;
            _currentThornAttackStepIndex = -1;

            _shouldEnterAngryBite = false;
            _didCurrentAngryBiteSucceed = false;

            _isBattleRunning = true;

            // 開幕演出中は口への攻撃を受け付けない
            _mouthHealth.CancelChallenge();

            // 開幕演出中は棘からバリアへダメージを与えない
            _thornGroupController.EndAttackStep();

            ChangeState(BossBattleState.Intro);

            // 開幕演出終了後に第1フェーズを開始する
            _stateRoutine = StartCoroutine(PlayIntroSequence());

            return true;
        }


        /// <summary>
        /// ボス戦を開始するために必要な設定が揃っているかどうかをチェックする
        /// </summary>
        /// <returns>できてたらtrue!</returns>
        private bool CanBeginBattle()
        {
            FindReferences();

            if (_battleData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] ボス戦データが設定されていません。");
                return false;
            }

            if (_battleData.PhaseCount != BossBattleDataSO.RequiredPhaseCount)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] ボス戦データのフェーズ数が{BossBattleDataSO.RequiredPhaseCount}ではありません。");
                return false;
            }

            if (_battleData.IntroPresentationData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 開幕演出設定がありません。");

                return false;
            }

            if (!_battleData.TryGetPhaseData( 0, out BossPhaseData firstPhaseData))
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 第1フェーズの設定を取得できません。");

                return false;
            }

            if (firstPhaseData.ThornAttackSteps == null ||
                firstPhaseData.ThornAttackSteps.Count <= 0 ||
                firstPhaseData.ThornAttackSteps[0] == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 開幕演出の基準にする第1フェーズ最初の攻撃設定がありません。");

                return false;
            }

            if (_animationController == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] ボスアニメーション制御コンポーネントが設定されていません。");

                return false;
            }

            if (_introPresentationController == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossIntroPresentationControllerが設定されていません。");

                return false;
            }

            if (_thornGroupController == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossThornGroupControllerコンポーネントが設定されていません。");
                return false;
            }

            if (_mouthHealth == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossMouthHealthコンポーネントが設定されていません。");
                return false;
            }

            if (_mouthHitReceiver == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] BossMouthHitReceiverコンポーネントが設定されていません。");
                return false;
            }

            return true;
        }


        // フェーズ開始
        // ------------------------------------------------------------

        /// <summary>
        /// 指定されたフェーズを開始する
        /// </summary>
        /// <param name="phaseIndex">開始するフェーズ番号</param>
        /// <returns>フェーズを開始できた場合はtrue</returns>
        private bool BeginPhase(int phaseIndex)
        {
            if (_battleData == null)
            {
                return false;
            }

            if (!_battleData.TryGetPhaseData(phaseIndex, out BossPhaseData phaseData))
            {
                return false;
            }

            _animationController.ResetForPhaseStart();

            // 新しいフェーズように有効な棘を抽選し、各棘のHPを設定する
            if (!_thornGroupController.BeginPhase(phaseData))
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{phaseIndex + 1}の開始に失敗しました。棘の抽選に失敗しています。");
                return false;
            }

            // 前回のアングリバイト受付状態が残らないようにする
            _mouthHealth.CancelChallenge();

            // 前回のアニメーション監視と再生が残らないようにする
            _animationController.CancelCurrentAnimation();

            _currentPhaseIndex = phaseIndex;
            _currentPhaseData = phaseData;
            _didCurrentAngryBiteSucceed = false;

            ChangeState(BossBattleState.ThornAttack);

            PhaseStarted?.Invoke(_currentPhaseIndex, _currentPhaseData);

            Debug.Log($"[{nameof(BossBattleController)}] フェーズ{phaseIndex + 1}を開始しました。");

            StartThornAttackSequence();

            return true;
        }

        private IEnumerator PlayIntroSequence()
        {
            if (_battleData == null || _battleData.IntroPresentationData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 開幕演出の設定がありません。");
                _stateRoutine = null;
                StopBattle();
                yield break;
            }

            if (!_battleData.TryGetPhaseData(0, out BossPhaseData firstPhaseData))
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 第1フェーズの設定を取得できません。");
                _stateRoutine = null;
                StopBattle();
                yield break;
            }

            if (firstPhaseData.ThornAttackSteps == null || firstPhaseData.ThornAttackSteps.Count <= 0 || firstPhaseData.ThornAttackSteps[0] == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 開幕演出の基準にする第1フェーズ最初の攻撃設定がありません。");
                _stateRoutine = null;
                StopBattle();
                yield break;
            }

            BossIntroPresentationData introData = _battleData.IntroPresentationData;

            BossThornAttackStepData baseStepData = firstPhaseData.ThornAttackSteps[0];

            yield return _introPresentationController.PlayPresentation(baseStepData, introData);

            // 演出中にボス戦が停止された場合はフェーズを開始しない
            if (!_isBattleRunning)
            {
                yield break;
            }

            _stateRoutine = null;

            // 開幕演出終了後、第1フェーズを開始する
            if (!BeginPhase(0))
            {
                Debug.LogError($"[{nameof(BossBattleController)}] 開幕演出後の第1フェーズ開始に失敗しました。", this);

                StopBattle();
            }
        }


        // イバラタックル
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のフェーズに設定されているイバラタックルの順番に従って、棘を順番に登場させる
        /// </summary>
        private void StartThornAttackSequence()
        {
            // 前の行動コルーチンが残っている場合は停止する
            if (_stateRoutine != null)
            {
                StopCoroutine(_stateRoutine);
                _stateRoutine = null;
            }

            _shouldEnterAngryBite = false;
            _currentThornAttackStepIndex = -1;

            _nextAttackSide = UnityEngine.Random.value < 0.5f ? BossAttackSide.Left : BossAttackSide.Right;

            _stateRoutine = StartCoroutine(PlayThornAttackSequence());
        }

        private IEnumerator PlayThornAttackSequence()
        {
            if (_currentPhaseData == null || _currentPhaseData.ThornAttackSteps == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のイバラタックルの順番が設定されていません。");

                EntryAngryBiteState();
                yield break;
            }

            int attackStepCount = _currentPhaseData.ThornAttackSteps.Count;

            for (int stepIndex = 0; stepIndex < attackStepCount; stepIndex++)
            {
                // 棘が全て破壊された場合は残りの攻撃段階を実行しない
                if (_shouldEnterAngryBite)
                {
                    break;
                }

                BossThornAttackStepData stepData = _currentPhaseData.ThornAttackSteps[stepIndex];

                if (stepData == null)
                {
                    Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のイバラタックルの段階{stepIndex + 1}の設定がありません。");
                    continue;
                }

                _currentThornAttackStepIndex = stepIndex;

                // 左右を交互に切り替える
                BossAttackSide attackSide = GetNextAttackSide();

                // この段階でバリアへ攻撃できるかどうかを3本全ての棘を設定する
                _thornGroupController.BeginAttackStep(_currentPhaseData.BarrierDamagePerThorn, stepData.CanDamageBarrier);

                bool didStartAnimation = _animationController.PlayThornAttack(stepData, attackSide);

                if (!didStartAnimation)
                {
                    // 再生に失敗した場合も攻撃判定を残さない
                    _thornGroupController.EndAttackStep();

                    Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のイバラタックルの段階{stepIndex + 1}のアニメーション再生に失敗しました。");

                    continue;
                }

                ThornAttackStepStarted?.Invoke(_currentPhaseIndex, _currentThornAttackStepIndex, attackSide, stepData);

                // イバラタックル接近中イベントを発火する
                EventBus.Publish(new BossThornWarningStartedEvent());
                bool isWarningActive = true;

                try
                {
                    yield return new WaitForFixedUpdate();

                    while (!_animationController.IsCurrentAnimationFinished())
                    {
                        // ボスが画面内に入ったら警告演出を終了する
                        if (isWarningActive && IsBossVisibleOnScreen())
                        {
                            EventBus.Publish(new BossThornWarningEndedEvent());
                            isWarningActive = false;
                        }

                        yield return null;
                    }
                }
                finally
                {
                    // イバラタックルが終了する前にボスが画面内に入らなかった場合は、警告演出を終了する
                    if (isWarningActive)
                    {
                        EventBus.Publish(new BossThornWarningEndedEvent());
                    }
                }

                // 1段階の攻撃が終了したら、棘の攻撃判定を終了する
                _thornGroupController.EndAttackStep();

                if (_shouldEnterAngryBite)
                {
                    break;
                }
            }

            EntryAngryBiteState();
        }


        /// <summary>
        /// ボスの頭(口の当たり判定)がカメラの画面内へ入ったかどうかを判定する
        /// </summary>
        private bool IsBossVisibleOnScreen()
        {
            Camera mainCamera = Camera.main;
            Transform headTransform = _mouthHitReceiver != null ? _mouthHitReceiver.transform : null;

            if (mainCamera == null || headTransform == null)
            {
                return true; // 判定できない場合は演出を出しっぱなしにしない
            }

            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(headTransform.position);


            // 頭が画面(下端より上・左右の範囲内)に入ったら「見えた」とみなす
            return viewportPoint.z > 0f
                && viewportPoint.x >= 0f && viewportPoint.x <= 1f
                && viewportPoint.y >= 0f;
        }


        /// <summary>
        /// 次のイバラタックルでボスが登場する方向を取得する
        /// </summary>
        /// <returns>今回のイバラタックルでボスが登場する方向</returns>
        private BossAttackSide GetNextAttackSide()
        {
            BossAttackSide currentSide = _nextAttackSide;

            _nextAttackSide = currentSide == BossAttackSide.Left ? BossAttackSide.Right : BossAttackSide.Left;

            return currentSide;
        }

        /// <summary>
        /// イバラタックルを終了し、アングリバイト状態へ移行する
        /// </summary>
        private void EntryAngryBiteState()
        {
            // イバラタックルのバリア攻撃判定を終了する
            _thornGroupController.EndAttackStep();

            // アングリバイト中は棘への攻撃を受け付けない
            _thornGroupController.DisableAllThorns();

            // イバラタックルのアニメーション監視を終了する
            _animationController.CancelCurrentAnimation();

            _currentThornAttackStepIndex = -1;
            _shouldEnterAngryBite = false;
            _didCurrentAngryBiteSucceed = false;

            ChangeState(BossBattleState.AngryBite);

            Debug.Log($"[{nameof(BossBattleController)}] アングリバイト状態へ移行しました。", this);

            // 口を開けて制限時間を計る処理を開始する
            _stateRoutine = StartCoroutine(PlayAngryBiteChallenge());
        }


        // アングリバイト
        // ------------------------------------------------------------

        /// <summary>
        /// 口を開けた状態でバリアへ迫りながら、制限時間内に口のHPを0にできるかどうかを監視する
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayAngryBiteChallenge()
        {
            if (_currentPhaseData == null || _currentPhaseData.AngryBiteData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイトの設定がありません。");

                _stateRoutine = null;
                yield break;
            }

            BossAngryBiteData biteData = _currentPhaseData.AngryBiteData;

            _didCurrentAngryBiteSucceed = false;

            // 口のHPを設定し、落とし物のヒット判定を有効化する
            _mouthHealth.BeginChallenge(biteData.MouthMaxHp);

            // 開始位置と向きを設定し、口を開けるアニメーションを再生する
            bool didStartAnimation = _animationController.PlayAngryBiteOpen(biteData);

            if (!didStartAnimation)
            {
                _mouthHealth.CancelChallenge();

                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイトの口を開けるアニメーション再生に失敗しました。");

                _stateRoutine = null;
                yield break;
            }

            AngryBiteStarted?.Invoke(_currentPhaseIndex, biteData);

            float elapsedTime = 0f;
            float challengeDuration = MathF.Max(0.01f, biteData.MouthOpenDuration);

            // アングリバイト開始時は、必ず上昇開始位置へ合わせる
            _animationController.UpdateAngryBiteRisePosition(biteData, 0f);

            while (elapsedTime < challengeDuration && !_mouthHealth.IsDepleted)
            {
                elapsedTime += Time.deltaTime;

                // Mouth Open Duration に対する 0 - 1 の進行度を計算する
                float riseProgress = Mathf.Clamp01(elapsedTime / challengeDuration);

                // 口の上昇位置を計算し、アニメーションコントローラーに反映する
                _animationController.UpdateAngryBiteRisePosition(biteData, riseProgress);

                yield return null;
            }

            _didCurrentAngryBiteSucceed = _mouthHealth.IsDepleted;

            // 判定が終了したため、これ以降は口のダメージを受け付けないようにする
            _mouthHealth.CancelChallenge();

            if (_didCurrentAngryBiteSucceed)
            {
                // 成功時は口開けアニメーションの監視を終了する
                _animationController.CancelCurrentAnimation();

                ChangeState(BossBattleState.Down);

                AngryBiteSucceeded?.Invoke(_currentPhaseIndex, biteData);

                Debug.Log( $"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイト阻止に成功したぜよ", this);

                // ダウン状態の演出を開始する
                yield return PlayDownSequence(biteData);

                yield break;
            }

            // 失敗時はバリアを噛むため、口を閉じるアニメーションを再生する
            bool didStartCloseAnimation = _animationController.PlayAngryBiteClose(biteData);

            if (didStartCloseAnimation)
            {
                // 口閉じアニメーション開始時に、ボスを防衛バリア付近の頂点へ固定する
                _animationController.BeginAngryBiteClosePositionLock();

                // 位置を固定したまま、口閉じアニメーションが終了するまで待機する
                while (!_animationController.IsCurrentAnimationFinished())
                {
                    yield return null;
                }
            }

            AngryBiteFailed?.Invoke(_currentPhaseIndex, biteData);

            Debug.Log($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイト阻止に失敗して無事死亡ｗ", this);

            // バリアダメージ、下降、イバラタックルへの復帰が
            // すべて完了するまで待機する
            yield return HandleAngryBiteFailure(biteData);
        }


        /// <summary>
        /// アングリバイト失敗時にバリアへダメージを与え、
        /// ボスの下降完了後に棘を復活させてイバラタックルへ戻す
        /// </summary>
        /// <param name="biteData">失敗したアングリバイトの設定</param>
        /// <returns></returns>
        private IEnumerator HandleAngryBiteFailure(BossAngryBiteData biteData)
        {
            if (biteData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイトの設定がありません。");

                _stateRoutine = null;
                StopBattle();

                yield break;
            }

            // バリアを噛んだ瞬間に、防衛バリアへダメージを与える
            float failureBarrierDamage = MathF.Max(0f, biteData.FailureBarrierDamage);

            if (failureBarrierDamage > 0f)
            {
                EventBus.Publish(new RuleBarrierAttackEvent(
                    failureBarrierDamage,
                    _mouthHitReceiver.transform.position));

                // バリア攻撃の衝撃をカメラシェイク
                EventBus.Publish(new CameraShakeRequestedEvent(
                    _biteImpactShakeDuration,
                    _biteImpactShakePositionStrength,
                    _biteImpactShakeRotationStrength,
                    _biteImpactShakeFrequency));
            }

            float failureHoldDuration = MathF.Max(0f, biteData.FailureHoldDuration);

            if (failureHoldDuration > 0f)
            {
                yield return new WaitForSeconds(failureHoldDuration);
            }

            _animationController.EndAngryBiteClosePositionLock();

            Transform motionRoot = _animationController.MotionRoot;

            if (motionRoot == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] アングリバイト下降用のMotionRootが設定されていません。",this);

                _stateRoutine = null;
                StopBattle();

                yield break;
            }

            Vector3 retreatStartLocalPosition = motionRoot.localPosition;

            float retreatDuration = MathF.Max(0.01f, biteData.FailureRetreatDuration);

            float retreatElapsedTime = 0f;

            // 下降開始時は、保存した現在位置をそのまま維持する
            _animationController.UpdateAngryBiteFailureRetreatPosition(biteData, retreatStartLocalPosition, 0f);

            while (retreatElapsedTime < retreatDuration)
            {
                retreatElapsedTime += Time.deltaTime;

                float retreatProgress = Mathf.Clamp01( retreatElapsedTime / retreatDuration);

                _animationController.UpdateAngryBiteFailureRetreatPosition(biteData, retreatStartLocalPosition, retreatProgress);

                yield return null;
            }

            // 誤差が残らないよう、最後に開始位置へ正確に合わせる
            _animationController.UpdateAngryBiteFailureRetreatPosition(biteData, retreatStartLocalPosition, 1f);

            if (_currentPhaseData == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}の設定がありません。");

                _stateRoutine = null;
                StopBattle();

                yield break;
            }

            // ボスが画面下へ到達してから、破壊された棘を復活させる
            bool didRestoreThorns = _thornGroupController.RestoreForRetry(_currentPhaseData);

            if (!didRestoreThorns)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイト失敗後の棘の復活に失敗しました。");

                _stateRoutine = null;
                StopBattle();

                yield break;
            }

            // 現在のアングリバイト処理を終了してから、新しいイバラタックル用コルーチンを開始する
            _stateRoutine = null;

            ChangeState(BossBattleState.ThornAttack);

            Debug.Log($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のアングリバイト失敗後、下降を完了してイバラタックル状態へ戻しました。", this);

            StartThornAttackSequence();
        }


        // ダウン・次フェーズ・勝利
        // ------------------------------------------------------------

        /// <summary>
        /// アングリバイトに成功し、ボスがダウン状態に入ったときの演出を再生する
        /// </summary>
        /// <param name="biteData"></param>
        /// <returns></returns>
        private IEnumerator PlayDownSequence(BossAngryBiteData biteData)
        {
            if (biteData == null || _currentPhaseData == null || _currentPhaseData.DownPresentationData == null || _downPresentationController == null)
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のダウン状態の演出に必要な設定がありません。");

                _stateRoutine = null;
                StopBattle();

                yield break;
            }

            BossDownPresentationData downPresentationData = _currentPhaseData.DownPresentationData;

            _successfulDownCount++;

            bool isFinalPhase = _battleData.IsFinalPhase(_currentPhaseIndex);

            // ダウン演出クラスを作成し、演出を開始する
            DownStarted?.Invoke(_currentPhaseIndex, biteData, downPresentationData, isFinalPhase);

            Debug.Log($"[{nameof(BossBattleController)}] フェーズ{_currentPhaseIndex + 1}のダウン演出を開始しました。", this);

            yield return _downPresentationController.PlayPresentation(biteData, downPresentationData);

            if (isFinalPhase)
            {
                CompleteBattle();
                yield break;
            }

            int nextPhaseIndex = _currentPhaseIndex + 1;

            // 現在のコルーチン参照を解除してから次のフェーズを開始する
            _stateRoutine = null;

            if (!BeginPhase(nextPhaseIndex))
            {
                Debug.LogError($"[{nameof(BossBattleController)}] フェーズ{nextPhaseIndex + 1}の開始に失敗しました。ボス戦を終了します。");
                StopBattle();
            }
        }


        /// <summary>
        /// ボス戦をすべてのフェーズをクリアして勝利したときの処理
        /// </summary>
        private void CompleteBattle()
        {
            _stateRoutine = null;
            _isBattleRunning = false;

            if (_mouthHealth != null)
            {
                _mouthHealth.CancelChallenge();
            }

            if (_thornGroupController != null)
            {
                _thornGroupController.ClearBattleState();
            }

            if (_animationController != null)
            {
                _animationController.CancelCurrentAnimation();
            }

            if (_introPresentationController != null)
            {
                _introPresentationController.ReleaseCameraForBattleCompletion();
            }

            ChangeState(BossBattleState.Defeated);

            if (!_hasPublishedDefeatedEvent && !string.IsNullOrEmpty(_bossInstanceId))
            {
                _hasPublishedDefeatedEvent = true;

                EventBus.Publish(new EnemyDefeatedEvent(_bossInstanceId));
            }

            BattleCompleted?.Invoke(_bossInstanceId);

            Debug.Log($"[{nameof(BossBattleController)}] ボス戦をすべてのフェーズをクリアして勝利したぜよ！", this);
        }


        // ボス戦停止
        // ------------------------------------------------------------

        /// <summary>
        /// 実行中のボス戦を停止し、各ボス用コンポーネントを初期状態に戻す
        /// </summary>
        public void StopBattle()
        {
            // 実行中のボス行動を停止する
            if (_stateRoutine != null)
            {
                StopCoroutine(_stateRoutine);
                _stateRoutine = null;
            }

            if (_introPresentationController != null)
            {
                _introPresentationController.CancelPresentationAndRestoreCamera();
            }

            // ダウン中にボス戦が停止された場合、生成したエフェクトも破棄
            if (_downPresentationController != null)
            {
                _downPresentationController.CancelPresentation();
            }

            _currentThornAttackStepIndex = -1;
            _shouldEnterAngryBite = false;
            _didCurrentAngryBiteSucceed = false;

            _successfulDownCount = 0;
            _hasPublishedDefeatedEvent = false;

            if (_thornGroupController != null)
            {
                _thornGroupController.ClearBattleState();
            }

            if (_mouthHealth != null)
            {
                _mouthHealth.CancelChallenge();
            }

            if (_animationController != null)
            {
                _animationController.CancelCurrentAnimation();
            }

            _isBattleRunning = false;

            _currentPhaseIndex = -1;
            _currentPhaseData = null;

            ChangeState(BossBattleState.Inactive);
        }



        // 状態変更
        // ------------------------------------------------------------

        /// <summary>
        /// ボス戦状態を変更し、状態が変わった場合のみ、イベントを通知
        /// </summary>
        /// <param name="nextState"></param>
        private void ChangeState(BossBattleState nextState)
        {
            if (_currentState == nextState)
            {
                return;
            }

            // ダウン中と撃破後だけ死体蹴りを受け付ける
            bool isCorpseKickActive = nextState == BossBattleState.Down || nextState == BossBattleState.Defeated;

            foreach (BossCorpseHitReceiver corpseHitReceiver in _corpseHitReceivers)
            {
                if (corpseHitReceiver != null)
                {
                    corpseHitReceiver.gameObject.SetActive(isCorpseKickActive);
                }
            }

            BossBattleState previousState = _currentState;

            _currentState = nextState;

            StateChanged?.Invoke(previousState, _currentState);
        }
    }
}
