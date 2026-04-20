//=========================================================================
// File: GameManager.cs
// Author: Terada
// Description: ゲーム全体のライフサイクル管理クラス
// Created: 2026-04-13
//
// Note:
// - 統合とテスト入力を作成し、動作確認を行いました (4/20 : 浅野)
// - しょーごに作ってもらったカードをGameManagerに組み込みました！ (4/20 : 浅野)
//=========================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatorKousien.Core;
using CreatorKousien.Battle;
using CreatorKousien.Data;
using CreatorKousien.UseCase;
using CreatorKousien.Field;
using CreatorKousien.Player;
using CreatorKousien.Enemy;
using CreatorKousien.Command;
using System.Collections;
using System.Threading;
using CreatorKousien.View;
using CreatorKousien.View.UI;
using CreatorKousien.View.Feedback;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// シーン状態
    /// </summary>
    private enum GameState { Title,Select,Game,Result}
    private GameState _currentState;// 現在のシーン状態

    [Header("ステージセットアップデータ設定")]
    [Tooltip("インスペクターで作成した BattleSetupData をアタッチしてください")]
    [SerializeField] private BattleSetupData _setupData;

    [Header("Field設定")]
    [Tooltip("ここにFieldViewのPrefabをアタッチ")]
    [SerializeField] private FieldView _fieldViewPrefab;

    [Header("UI設定")]
    [Tooltip("UIManagerがアタッチされた Canvas のプレファブをセット！")]
    [SerializeField] private UIManager _uiManagerPrefab;

    [Header("視覚情報")]
    [Tooltip("TestTimerViewのPrefabをアタッチ")]
    [SerializeField] private TestTimerView _timerPrefab;

    /// <summary>
    /// 固定のプレイヤーID
    /// </summary>
    private const int PlayerID = 1;

    // ==== Manager / Mediator ====
    private GameMediator _mediator;                 /// ゲーム全体の調整役
    private BattleManager _battleManager;           /// 戦闘のルールやダメージ計算を管理
    private StageManager _stageManager;             /// ステージの情報を管理
    private TurnManager _turnManager;               /// ターンの進行を管理

    // ==== Event / Command ====
    private GameEventBus _eventBus;                 /// ゲーム全体のイベントを管理するEventBus
    private CommandDispatcher _dispatcher;          /// コマンドの実行を管理するDispatcher

    // ==== Field ====
    private FieldView _fieldView;                   /// 盤面の状態を描画するViewクラス
    private FieldService _fieldService;             /// 盤面の状態を管理し、ルールに沿った操作を提供するサービスクラス
    private TileEffectSystem _tileEffect;           /// タイルエフェクトの管理クラス

    // ==== UI ====
    private UIManager _uiManager;                   /// UI全般を管理するクラス

    // ==== Player ====
    private PlayerSystem _player;                   /// プレイヤーの状態を管理するSystemクラス
    private PlayerView _playerView;                 /// プレイヤーの描画を管理するViewクラス

    // ==== Enemy ====
    private EnemySystem _enemy;                     /// 敵の状態を管理するSystemクラス
    private readonly Dictionary<int, EnemyView> _enemyViews = new Dictionary<int, EnemyView>();     /// 敵の描画を管理するViewクラスの辞書（ActorIdをキーにして管理）
    private ActionTelegraphSystem _actiontelegraph;                                                 /// 敵の行動予告を管理するクラス

    // ==== Card ====
    [Header("カード設定")]
    [SerializeField] private CardData _cardUp;
    [SerializeField] private CardData _cardDown;
    [SerializeField] private CardData _cardLeft;
    [SerializeField] private CardData _cardRight;

    private CardSystem _cardSystem;                 /// カードの状態を管理するSystemクラス
    private HandState _handState;                   /// プレイヤーの手札の状態を管理するクラス



    private void Awake()
    {
        // シーンをまたいで生存する設定
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }



    private void OnDestroy()
    {
        // イベントを解除（メモリリーク防止）
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }



    // Update is called once per frame
    void Update()
    {
        // 現在のシーン状態に応じて処理を変更(使わないかも)
        switch (_currentState)
        {
            case GameState.Title:
                UpdateTitleScene();
                break;
            case GameState.Select:
                UpdateSelectScene();
                break;
            case GameState.Game:
                UpdateGameScene();
                break;
            case GameState.Result:
                UpdateResultScene();
                break;
        }
    }


    /// <summary>
    /// シーンロード時の初期化振り分け
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene Loaded : {scene.name}");

        if (scene.name == "Title")
        {
            _currentState = GameState.Title;
            Debug.Log("[GameManager] TitleScene Start");
        }
        else if (scene.name == "Select")
        {
            _currentState = GameState.Select;
            InitializeSelectScene();
        }
        else if (scene.name == "Game")
        {
            _currentState = GameState.Game;
            InitializeGameScene();
        }
        else if (scene.name == "Result")
        {
            _currentState = GameState.Result;
            InitializeResultScene();
        }
    }


    void SceneChangeStart(string sceneName)
    {
        // 終了処理
        SceneManager.LoadScene(sceneName);
    }

    void InitializeSelectScene()
    {
        Debug.Log("[GameManager] SelectSceneStart");
        _stageManager = new StageManager();
        _stageManager.Initialize();

        // SelectSceneViewを取得
        var selectView = FindFirstObjectByType<SelectSceneView>();
        if(selectView == null)
        {
            Debug.LogError("[GameManager] SelectViewが見つかりません");
            return;
        }
        // Setup
        selectView.Setup(_stageManager, SceneChangeStart);

    }



    /// <summary>
    /// ゲームシーンの初期化処理を行う関数
    /// </summary>
    private void InitializeGameScene()
    {
        Debug.Log("[GameManager] GameSceneStart");

        ClearGameSceneReferences();

        if (_stageManager != null)
        {
            _setupData = _stageManager.GetSelectedBattleSetupData();
        }

        if (_setupData == null)
        {
            Debug.LogError("[GameManager] BattleSetupData が取得できません");
            return;
        }

        // 1. 基本システム生成
        // ------------------------------------------------------------
        _eventBus = new GameEventBus();
        _battleManager = new BattleManager();

        // カードシステムと手札の初期化
        _handState = new HandState();
        _cardSystem = new CardSystem(_handState);

        // インスペクターでセットしたカードをスロットに配置（テスト用）
        if (_cardUp) _cardSystem.SetCardToSlot(SlotPosition.Up, new CardRuntimeData(0, _cardUp));
        if (_cardDown) _cardSystem.SetCardToSlot(SlotPosition.Down, new CardRuntimeData(1, _cardDown));
        if (_cardLeft) _cardSystem.SetCardToSlot(SlotPosition.Left, new CardRuntimeData(2, _cardLeft));
        if (_cardRight) _cardSystem.SetCardToSlot(SlotPosition.Right, new CardRuntimeData(3, _cardRight));


        // 2. UI生成
        // ------------------------------------------------------------
        InitializeUI();
        UpdateCardUI(); // カードUIを初期状態に更新


        // 3. フィールド生成
        // ------------------------------------------------------------
        InitializeField();


        // 4. キャラクター
        // ------------------------------------------------------------
        InitializeCharacters();


        // 5. 演出系生成
        // ------------------------------------------------------------
        _tileEffect = new TileEffectSystem(_fieldService.State);
        // _actiontelegraph = new ActionTelegraphSystem(); ← ここは InitializeCharacters で生成済みなら不要


        // 6. EventBus配線
        // ------------------------------------------------------------
        InitEventBus();


        // 7. TurnManager生成
        // ------------------------------------------------------------
        _turnManager = GetComponent<TurnManager>();
        if (_turnManager == null)
        {
            _turnManager = gameObject.AddComponent<TurnManager>();
        }

        // 8. コマンドをセットアップしてTurnManagerに渡す
        // ------------------------------------------------------------
        _dispatcher = SetupCommandDispatcher();


        // 9. TurnManager初期化
        // ------------------------------------------------------------
        _turnManager.Initialize(_dispatcher, _eventBus, _handState);

        _turnManager.OnPlayerActionSubmitted += UpdateCardUI; // プレイヤーが行動を提出したらカードUIを更新する


        // 10. Mediator初期化
        // ------------------------------------------------------------
        _mediator = new GameMediator();
        _mediator.Initialize(_dispatcher, _eventBus, _turnManager);


        // 11. フィードバック演出
        // ------------------------------------------------------------
        InitializeFeedbackSystems();
    }

    void InitializeResultScene()
    {

    }

    void UpdateTitleScene()
    {

    }
    void UpdateSelectScene()
    {

    }

    void UpdateGameScene()
    {
    }

    void UpdateResultScene()
    {

    }

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        if (_uiManagerPrefab == null) return;

        _uiManager = Instantiate(_uiManagerPrefab);
        _uiManager.Initialize();

        var playerHpGauge = _uiManager.GetView<HpGaugeView>(ViewType.HpGauge);
        if (playerHpGauge != null)
        {
            playerHpGauge.Initialize(_eventBus, PlayerID, _setupData.PlayerData.MaxHp);
        }

        var turnDisplay = _uiManager.GetView<TurnDisplayView>(ViewType.TurnDisplay);
        if (turnDisplay != null)
        {
            turnDisplay.Initialize(_eventBus);
        }
    }



    /// <summary>
    /// カードUIの更新を行う関数
    /// </summary>
    private void UpdateCardUI()
    {
        if (_uiManager == null || _handState == null) return;

        var cardSlotView = _uiManager.GetView<CardSlotView>(ViewType.CardSlot);
        if (cardSlotView == null) return;

        List<UICardData> uiCardDataList = new List<UICardData>();
        SlotPosition[] slots = { SlotPosition.Up, SlotPosition.Down, SlotPosition.Left, SlotPosition.Right };

        foreach (var slot in slots)
        {
            var cardRuntime = _handState.GetCard(slot);
            if (cardRuntime != null)
            {
                var currentEffect   = cardRuntime.GetCurrentEffect();
                var uiData          = UICardConverter.ConvertToUICardData(cardRuntime.BaseData, currentEffect, cardRuntime.InstanceId.ToString());

                if (currentEffect != null && currentEffect.Type.ToString().Contains("Move"))
                {
                    switch (slot)
                    {
                        case SlotPosition.Up: uiData.Direction = UIDirection.Up; break;
                        case SlotPosition.Down: uiData.Direction = UIDirection.Down; break;
                        case SlotPosition.Left: uiData.Direction = UIDirection.Left; break;
                        case SlotPosition.Right: uiData.Direction = UIDirection.Right; break;
                    }
                }

                uiCardDataList.Add(uiData);
            }
            else uiCardDataList.Add(null);
        }
        cardSlotView.SetCardDataList(uiCardDataList);
    }



    /// <summary>
    /// フィールド初期化
    /// </summary>
    private void InitializeField()
    {
        _fieldView = Instantiate(_fieldViewPrefab);

        _fieldService = new FieldService();
        _fieldService.Initialize(_setupData.StageData);

        _fieldView.BuildView(_fieldService.State, _setupData.StageData);
    }



    /// <summary>
    /// EventBusの初期化を行う関数
    /// </summary>
    private void InitEventBus()
    {
        // Timer生成
        if (_timerPrefab != null)
        {
            Vector3 timerPosition = new Vector3(4.375f, 2.5f, 0f);
            var timerInstance = Instantiate(_timerPrefab, timerPosition, Quaternion.identity);
            timerInstance.Initialize(_eventBus);
        }

        // 移動要求
        _eventBus.OnActorMoveRequested += (actorId, targetGridPos) =>
        {
            Vector3 worldPos = _fieldView.GetCellWorldPosition(targetGridPos.x, targetGridPos.y);
            GridCellView cellView = _fieldView.GetCellView(targetGridPos);

            if (actorId == _player.RuntimeData.ActorId)
            {
                _playerView.UpdateTargetPosition(worldPos);
                _playerView.SetStandingTile(cellView);
                _fieldView.HighlightCell(targetGridPos.x, targetGridPos.y);
            }
            else if (_enemyViews.TryGetValue(actorId, out var eView))
            {
                eView.MoveTo(worldPos);
                eView.SetStandingTile(cellView);
            }
        };

        // 実移動成功
        _fieldService.OnActorMoved += (actorId, x, y) =>
        {
            Vector3 targetWorldPos = _fieldView.GetCellWorldPosition(x, y);
            GridCellView targetTile = _fieldView.GetCellView(new Vector2Int(x, y));

            if (actorId == _player.RuntimeData.ActorId)
            {
                _player.SyncPosition(new Vector2Int(x, y));
                _playerView.UpdateTargetPosition(targetWorldPos);
                _playerView.SetStandingTile(targetTile);
                _fieldView.HighlightCell(x, y);
            }
            else if (_enemyViews.TryGetValue(actorId, out var eView))
            {
                eView.MoveTo(targetWorldPos);
                eView.SetStandingTile(targetTile);
            }
        };

        // 移動失敗
        _eventBus.OnMoveFailed += (actorId, failX, failY) =>
        {
            float cellSize = _setupData.StageData.CellSize;

            if (actorId == _player.RuntimeData.ActorId)
            {
                Vector2Int currentPos = _player.RuntimeData.Position;
                Vector2Int delta = new Vector2Int(failX - currentPos.x, failY - currentPos.y);
                Vector3 offset = new Vector3(delta.x * cellSize, 0f, -delta.y * cellSize);
                _playerView.PlayMoveFailEffect(_playerView.transform.position + offset);
            }
            else if (_enemyViews.TryGetValue(actorId, out var eView))
            {
                var enemyData = _enemy.GetEnemyData(actorId);
                if (enemyData != null)
                {
                    Vector2Int currentPos = enemyData.Position;
                    Vector2Int delta = new Vector2Int(failX - currentPos.x, failY - currentPos.y);
                    Vector3 offset = new Vector3(delta.x * cellSize, 0f, -delta.y * cellSize);
                    eView.PlayMoveFailEffect(eView.transform.position + offset);
                }
            }
        };

        // ダメージ
        _eventBus.OnDamageTaken += (targetId, damage) =>
        {
            if (targetId == _player.RuntimeData.ActorId)
            {
                _playerView.PlayDamageEffect(damage);
            }
            else if (_enemyViews.TryGetValue(targetId, out var eView))
            {
                eView.PlayDamageEffect();
            }
        };

        // 死亡
        _eventBus.OnActorDeath += (targetId) =>
        {
            if (targetId == _player.RuntimeData.ActorId)
            {
                _playerView.PlayDeathEffect();
            }
            else if (_enemyViews.TryGetValue(targetId, out var eView))
            {
                eView.PlayDeathEffect();
                _enemyViews.Remove(targetId);
            }
        };

        // 攻撃範囲表示
        _eventBus.OnAttackAreaExecuted += (targetCells) =>
        {
            _fieldView.ShowAttackAreaWithAutoOff(targetCells, 0.3f);
        };

        // 攻撃ヒット
        _eventBus.OnAttackHit += (targetActorId) =>
        {
            Debug.Log($"<color=red>[View] ActorID:{targetActorId} に攻撃ヒットエフェクトを再生！</color>");
        };

        // 行動完了後
        _eventBus.OnActionLogicCompleted += (actorId) =>
        {
            StartCoroutine(WaitAnimationAndProceed());
        };

        // 戻って来た通知を受け取って画面を動かす
        _eventBus.OnTelegraphRequested += (targetCells, isWarning, stepOrder) =>
        {
            _fieldView.ShowTelegraph(targetCells, isWarning, stepOrder);
        };

        // 移動予告の通知を受け取って画面を動かす
        _eventBus.OnMoveTelegraphRequested += (targetCell, isWarning, stepOrder) =>
        {
            _fieldView.ShowMoveTelegraph(targetCell, isWarning, stepOrder);
        };

        // 予告のクリアの通知を受け取って画面を動かす
        _eventBus.OnClearAllTelegraphsRequested += () =>
        {
            _fieldView.ClearAllTelegraph();
        };
    }



    /// <summary>
    /// UseCaseとCommandを登録したCommandDispatcherを生成し返す
    /// </summary>
    /// <returns>生成したCommandDispatcher</returns>
    private CommandDispatcher SetupCommandDispatcher()
    {
        // Dispatcher生成
        var dispatcher = new CommandDispatcher();

        // 各種UseCaseの生成
        MoveUseCase moveUseCase = new MoveUseCase(_fieldService, _tileEffect, _eventBus);
        AttackUseCase attackUseCase = new AttackUseCase(_battleManager, _fieldService, _player, _enemy, dispatcher, _eventBus);
        EnemyActionUseCase enemyUseCase = new EnemyActionUseCase(_enemy, _fieldService, _player, _actiontelegraph, dispatcher, _eventBus);
        PlayerActionUseCase playerActionUseCase = new PlayerActionUseCase(_cardSystem, _turnManager, _player, _fieldService);

        // 各種Commandの登録
        dispatcher.Register<MoveCommand>(moveUseCase.Execute);
        dispatcher.Register<AttackCommand>(attackUseCase.Execute);
        dispatcher.Register<EnemyActionCommand>(enemyUseCase.Execute);
        dispatcher.Register<PlayerActionCommand>(playerActionUseCase.Execute);

        return dispatcher;
    }



    /// <summary>
    /// プレイヤーと敵の初期化
    /// </summary>
    private void InitializeCharacters()
    {
        // TelegraphをEnemySystem生成前に用意
        _actiontelegraph = new ActionTelegraphSystem();

        //==== Player ====
        _player = new PlayerSystem();

        Vector2Int pPos = _setupData.StageData.PlayerStartPosition;
        _player.Initialize(_setupData.PlayerData, pPos);                                                                // PlayerSystemの初期化と同時にPlayerRuntimeDataも生成されるため、以降は_player.RuntimeData.Positionで位置を参照可能
        _fieldService.UpdateOccupancy(PlayerID, -1, -1, pPos.x, pPos.y);                                                // フィールドの占有状態を更新

        Vector3 playerWorldPos = _fieldView.GetCellWorldPosition(pPos.x, pPos.y);                                       // PlayerViewの生成と初期化
        GameObject playerObj = Instantiate(_setupData.PlayerData.PlayerPrefab, playerWorldPos, Quaternion.identity);    // PlayerViewはPlayerPrefabにアタッチされている前提

        _playerView = playerObj.GetComponent<PlayerView>();                                                             // PlayerViewの初期化とフィールドへの配置
        _playerView.Initialize(playerWorldPos);
        _playerView.SetStandingTile(_fieldView.GetCellView(pPos));
        _fieldView.HighlightCell(pPos.x, pPos.y);

        //==== Enemy ====
        _enemy = new EnemySystem(_actiontelegraph);

        // 敵のスポーンと同時にEnemyViewも生成して配置。EnemySystemにはスポーン位置を渡していないため、EnemyViewの位置を参照してEnemyRuntimeDataの位置を更新する
        foreach (var enemyInfo in _setupData.Enemies)
        {
            _enemy.SpawnEnemy(enemyInfo.ActorId, enemyInfo.EnemyData, enemyInfo.SpawnPosition);
            _fieldService.UpdateOccupancy(enemyInfo.ActorId, -1, -1, enemyInfo.SpawnPosition.x, enemyInfo.SpawnPosition.y);

            Vector3 worldPos = _fieldView.GetCellWorldPosition(enemyInfo.SpawnPosition.x, enemyInfo.SpawnPosition.y);
            GameObject enemyObj = Instantiate(enemyInfo.EnemyData.EnemyPrefab, worldPos, Quaternion.identity);

            EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
            enemyView.Initialize(enemyInfo.ActorId, worldPos, _fieldView.GetCellView(enemyInfo.SpawnPosition));

            var enemyHpGauge = _uiManager.GetView<HpGaugeView>(ViewType.EnemyHpGauge);
            if (enemyHpGauge != null)
            {
                enemyHpGauge.Initialize(_eventBus,enemyInfo.ActorId, enemyInfo.EnemyData.MaxHp);
            }
            // EnemySystemのEnemyRuntimeDataの位置をEnemyViewの位置に合わせて更新
            _enemyViews.Add(enemyInfo.ActorId, enemyView);
        }
    }



    /// <summary>
    /// フィードバックシステムの初期化
    /// </summary>
    private void InitializeFeedbackSystems()
    {
        GameObject feedbackObj = new GameObject("FeedbackSystem");
        var feedbackCoordinator = feedbackObj.AddComponent<ActionFeedbackCoordinator>();
        feedbackCoordinator.Initialize(_eventBus);
    }



    /// <summary>
    /// Gameシーン再初期化前の参照クリア
    /// </summary>
    private void ClearGameSceneReferences()
    {
        _uiManager = null;
        _fieldView = null;
        _fieldService = null;
        _tileEffect = null;

        _player = null;
        _playerView = null;

        _enemy = null;
        _enemyViews.Clear();

        _eventBus = null;
        _dispatcher = null;
        _battleManager = null;
        _turnManager = null;
        _mediator = null;
    }



    /// <summary>
    /// アニメーション待機後にTurnManagerへ通知
    /// </summary>
    private IEnumerator WaitAnimationAndProceed()
    {
        yield return new WaitForSeconds(0.5f);
        _mediator.CompleteCurrentActionAnimation();
    }
}
