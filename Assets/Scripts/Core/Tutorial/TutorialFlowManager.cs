using Game.Core.Enemy;
using Game.Core.Events;
using Game.Core.Management;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Shop;
using Game.WaveSystem;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialFlowManager : GameProgressionManagerBase
{
    [Header("チュートリアルデータ")]
    [SerializeField] List<TutorialWave> _tutorialDatas;

    [SerializeField] private InputActionAsset _actionAssetRef;
    private InputActionMap _actionMap;

    [SerializeField] private InputAction _clickAction;
    [SerializeField] private InputAction _submitAction;

    // UI管理
    GameUIController _gameUI;

    public override IEnumerator PrepareFirstWaveRoutine()
    {
        Debug.Log("[Tutorial]参照取得が開始されました。");

        if (_isFirstWavePrepared || _preparationFailed)
        {
            yield break;
        }

        if (_isPreparingFirstWave)
        {
            yield return new WaitUntil(() => _isFirstWavePrepared || _preparationFailed);
            yield break;
        }

        _isPreparingFirstWave = true;

        // 初期化
        _currentWaveIndex = 0;
        _currentState = GameProgressionState.Setup;

        // 参照取得
        StageSceneContext stageContext = null;
        PlayerFacade player = null;

        // 参照が揃うまで待機
        while (_enemySpawner == null || stageContext == null || player == null || _gameUIController == null)
        {
            if (_enemySpawner == null) _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (stageContext == null) stageContext = Object.FindFirstObjectByType<StageSceneContext>();
            if (player == null) player = Object.FindFirstObjectByType<PlayerFacade>();
            if (_gameUIController == null) _gameUIController = Object.FindFirstObjectByType<GameUIController>();


            yield return null;
        }

        _actionMap = _actionAssetRef.FindActionMap("Roguelike", throwIfNotFound: false);

        _clickAction = _actionMap.FindAction("Click");
        _submitAction = _actionMap.FindAction("Submit");

        if (_clickAction == null || _submitAction == null)
        {
            _preparationFailed = true;
            _isPreparingFirstWave = false;
            Debug.Log("[Tutorial]入力アクションが取得できませんでした");
            yield break;

        }

        // WaveRunnerの参照がまだなら取得
        if (_waveRunner == null) _waveRunner = GetComponent<WaveRunner>();

        if (_waveRunner == null)
        {
            Debug.LogError("[Tutorial] WaveRunnerがGameProgressionManagerと同じGameObjectにありません。");
            _preparationFailed = true;
            _isPreparingFirstWave = false;
            yield break;
        }


        if (stageContext.ManualTestMode)
        {
            Debug.Log("[Tutorial] 手動テストモードのため、自動Wave進行をスキップします");
            _isFirstWavePrepared = true;
            _isPreparingFirstWave = false;
            yield break;
        }

        // Bootシーンはこの後アンロードされるので、Stage進行に必要な情報を控えておく
        _currentStageData = stageContext.StageData;
        _baseSeed = stageContext.CreateSeed();
        _stageIndex = 0;


        _submitAction.Enable();
        _clickAction.Enable();

        _isFirstWavePrepared = true;
        _isPreparingFirstWave = false;


        Debug.Log("[Tutorial]参照取得が終了しました。");
    }

    public override void BeginPreparedGame()
    {
        Debug.Log("[Tutorial]チュートリアルを開始します。");
        StartCoroutine(StartTutorial());
    }

    protected override GameResultSummary CreateSummry(bool isClear)
    {
        return new GameResultSummary(isClear, _currentWaveIndex, 0.0f);
    }

    public override void RequestNextStage()
    {
        _currentWaveIndex++;
    }

    protected override void HandleWaveClear()
    {
    }

    protected override void OnDefenseLineBroken(DefLineBreakReactionEvent ev)
    {

    }


    protected override void RoguelikeToComeback()
    {

    }




    public IEnumerator StartTutorial()
    {
        yield return StartCoroutine(DrawTutoeialText("チュートリアル開始だよ！"));
        // 全てのWaveを回す
        while (_currentWaveIndex < _tutorialDatas.Count)
        {
            // 開始し、終了まで待機
            yield return StartCoroutine(PlayTutorial(_tutorialDatas[_currentWaveIndex]));

            // ウェーブ数を加算
            _currentWaveIndex++;
        }

        yield return StartCoroutine(DrawTutoeialText("チュートリアルはこれで終わり！健闘を祈る！"));
        Debug.Log("全てのチュートリアルが終了しました！");

        SoundManager.instance?.StopBGM();
        GoToResult(true);
    }

    IEnumerator PlayTutorial(TutorialWave wave)
    {
        // 事前処理
        yield return StartCoroutine(StartChangeState(wave));
        if (wave.UseStartText) yield return StartCoroutine(DrawTutoeialText(wave.StartText));
        TutorialStart(wave);


        // チュートリアルが終了しているか確認
        yield return TutorialMain(wave);

        // 終了処理
        TutorialEnd(wave);
        if (wave.UseEndText) yield return StartCoroutine(DrawTutoeialText(wave.EndText));
        yield return StartCoroutine(EndChangeState(wave));
    }


    // 参照取得
    IEnumerator GetRef()
    {

        // 参照が揃うまで待機
        while (_enemySpawner == null || _gameUI == null)
        {
            if (_enemySpawner == null) _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (_gameUI == null) _gameUI = Object.FindFirstObjectByType<GameUIController>();

            yield return null;
        }

        while (_cameraRigController != null || _shopVehicleController != null || _shopCinematicCameraController != null)
        {
            if (_cameraRigController == null) _cameraRigController = Object.FindAnyObjectByType<CameraRigController>();
            if (_shopVehicleController == null) _shopVehicleController = Object.FindAnyObjectByType<ShopVehicleController>();
            if (_shopCinematicCameraController == null) _shopCinematicCameraController = Object.FindAnyObjectByType<ShopCinematicCameraController>();
            yield return null;
        }

        // WaveRunnerの参照がまだなら取得
        if (_waveRunner == null) _waveRunner = GetComponent<WaveRunner>();
    }

    void TutorialStart(TutorialWave wave)
    {
        Debug.Log(wave.name + "を開始します！");

        if (wave == null) { return; }

        if (wave.StartRoguelike)
        {
            _currentState = GameProgressionState.Roguelike;

            // バトル側のポーズ処理
            Time.timeScale = 0f;

            // マウスカーソル表示
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ローグライクシーンの加算ロード
            StartCoroutine(LoadSceneAdditiveRoutine(_roguelikeSceneName));
        }
        else if (wave.UseStartWave)
        {
            StartCoroutine(_waveRunner.PlayWave(wave.WaveData, _enemySpawner));
            _gameUIController.UIVisible(1.0f);
        }
        else if (wave.UseEnemySpawn)
        {
            // 敵を生成
            foreach (var spawnEnemy in wave.Enemies)
            {
                EnemyController _enemyController;
                _enemySpawner.TrySpawnEnemy(spawnEnemy, 1.0f, 1.0f, 0.5f, out _enemyController);
            }
        }
    }

    IEnumerator TutorialMain(TutorialWave wave)
    {
        switch (wave.clearConditions)
        {
            case TutorialWave.ClearConditions.EnemyKill:
                // 登録する
                {
                    // フラグ群を作成し、フラグを更新するラムダを生成
                    int spawnEnemyCount = wave.Enemies.Count;
                    TutorialEndFlag endflag = new TutorialEndFlag(true, spawnEnemyCount);
                    System.Action<EnemyDefeatedEvent> action = (EnemyDefeatedEvent ev) => { endflag._count--;};

                    // イベントにラムダを登録し、終了フラグが立つまで待機する
                    EventBus.Subscribe<EnemyDefeatedEvent>(action);
                    yield return EndWaitCoroutine(endflag);
                    EventBus.Unsubscribe<EnemyDefeatedEvent>(action);
                }
                break;

            // 上に同じ
            case TutorialWave.ClearConditions.WaveClear:
                {
                    TutorialEndFlag endflag = new TutorialEndFlag(false, 0);
                    System.Action<WaveEndEvent> action = (WaveEndEvent ev) => endflag._frag = true;

                    EventBus.Subscribe<WaveEndEvent>(action);
                    yield return EndWaitCoroutine(endflag);
                    EventBus.Unsubscribe<WaveEndEvent>(action);
                }
                break;

            case TutorialWave.ClearConditions.ShopEnd:
                {
                    TutorialEndFlag endflag = new TutorialEndFlag(false, 0);
                    System.Action<TutorialShopEndEvent> action = (TutorialShopEndEvent ev) => endflag._frag = true;

                    EventBus.Subscribe<TutorialShopEndEvent>(action);
                    yield return EndWaitCoroutine(endflag);
                    EventBus.Unsubscribe<TutorialShopEndEvent>(action);
                }
                break;

            case TutorialWave.ClearConditions.GetCollectible:
                {
                    TutorialEndFlag endflag = new TutorialEndFlag(true, 1);
                    System.Action<CrystalHitEvent> action = (CrystalHitEvent ev) => endflag._count--;


                    // イベントにラムダを登録し、終了フラグが立つまで待機する
                    EventBus.Subscribe<CrystalHitEvent>(action);
                    yield return EndWaitCoroutine(endflag);
                    EventBus.Unsubscribe<CrystalHitEvent>(action);
                }
                break;

            default:
                Debug.LogError("チュートリアルの終了条件が登録されていません、クリアしたことにして次に進みます");
                break;
        }
    }

    void TutorialEnd(TutorialWave wave)
    {
        Debug.Log(wave.name + "を終了します！");

        if (wave.UseStartWave)
        {
            _gameUIController.UIInvisible(1.0f);
        }

    }


    class TutorialEndFlag
    {
        public int _count;
        public bool _frag;


        public TutorialEndFlag(bool frag, int count)
        {
            _frag = frag;
            _count = count;
        }
    }

    IEnumerator DrawTutoeialText(string text)
    {
        yield return null;

        Debug.Log("TutorialTextを生成");
        EventBus.Publish(new TutorialTextEvent(text));
        Time.timeScale = 0.0f;
        while (!_clickAction.triggered && !_submitAction.triggered)
        {
            if (!_clickAction.enabled || !_submitAction.enabled) Debug.LogError("入力が効いてないぜ！！");
            if (!_clickAction.enabled) _clickAction.Enable();
            if (!_submitAction.enabled) _submitAction.Enable();
            yield return null;
        }
        Time.timeScale = 1.0f;
        EventBus.Publish(new TutorialTextResetEvent(text));
        Debug.Log("TutorialTextを終了");
        yield return null;
    }

    IEnumerator EndWaitCoroutine(TutorialEndFlag flag)
    {
        while (flag._count > 0 || !flag._frag)
        {
            yield return null;
        }
    }

    IEnumerator StartChangeState(TutorialWave wave)
    {
        if (wave.StartRoguelike) yield return StartCoroutine(ShopPresentationSequenceRoutine());
    }

    IEnumerator EndChangeState(TutorialWave wave)
    {
        if (wave.StartRoguelike) yield return StartCoroutine(UnloadRoguelikeAndAdvanceRoutine());
    }

    public override void CompleteRoguelikeSequence()
    {
        EventBus.Publish(new TutorialShopEndEvent());   
    }


#if UNITY_EDITOR

    /// <summary>
    /// デバッグ用。実行中のWaveを中断して、指定したWaveを演出なしで開始しまっす！
    /// </summary>
    /// <param name="waveIndex">開始したいWaveの番号(0始まり)</param>
    public override void DebugStartWaveAt(int waveIndex)
    {

    }

    /// <summary>
    /// デバッグ用。演出を飛ばして次のチュートリアルWaveへ進みます
    /// </summary>
    public override void DebugSkipToNextWave()
    {

    }

    /// <summary>
    /// デバッグ用。
    /// </summary>
    public override void DebugJumpToFinalWave()
    {

    }
#endif
}
