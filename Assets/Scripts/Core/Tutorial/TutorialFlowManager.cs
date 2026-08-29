using Game.Core.Enemy;
using Game.Core.Events;
using Game.Core.Management;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Shop;
using Game.WaveSystem;
using System.Collections;
using System.Collections.Generic;

using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class TutorialFlowManager : MonoBehaviour
{
    [Header("チュートリアルデータ")]
    [SerializeField] List<TutorialWave> _tutorialDatas;

    [Header("コマンド用参照")]
    [SerializeField] WaveRunner _waveRunner;
    [SerializeField] EnemySpawner _spawner;


    // UI管理
    GameUIController _gameUI;

    [Header("ショップ演出関連の参照")]
    [SerializeField] private CameraRigController _cameraRigController;
    [SerializeField] private ShopVehicleController _shopVehicleController;
    [SerializeField] private ShopCinematicCameraController _shopCinematicCameraController;

    // 後戻りしたりもできるように数値で進行度を保持
    int currentWave;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 各参照を取得する
    }

    public IEnumerator StartTutorial()
    {
        // 全てのWaveを回す
        while (currentWave < _tutorialDatas.Count)
        {
            // 開始し、終了まで待機
            yield return StartCoroutine(PlayTutorial(_tutorialDatas[currentWave]));

            // ウェーブ数を加算
            currentWave++;
        }
    }

    IEnumerator PlayTutorial(TutorialWave wave)
    {
        // 事前処理
        TutorialStart(wave);

        // チュートリアルが終了しているか確認
        yield return TutorialMain(wave);

        // 終了処理
        TutorialEnd(wave);
    }


    // 参照取得
    IEnumerator GetRef()
    {

        // 参照が揃うまで待機
        while (_spawner == null || _gameUI == null)
        {
            if (_spawner == null) _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (_gameUI == null) _gameUI = Object.FindFirstObjectByType<GameUIController>();

            yield return null;
        }

        while(_cameraRigController != null || _shopVehicleController != null || _shopCinematicCameraController != null)
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


        TutorialStartRequest request = wave.waveStartRequest;
        if (request == null) { return; }

        if(request.UseStartWave)
        {
            _waveRunner.PlayWave(request.WaveData, _spawner);
        }
        else if(request.UseEnemySpawn)
        {
            // 敵を生成
            foreach(var spawnEnemy in request.Enemies)
            {
                EnemyController _enemyController;
                _spawner.TrySpawnEnemy(spawnEnemy, 1.0f, 1.0f, 0.5f, out _enemyController);
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
                    int spawnEnemyCount = wave.waveStartRequest.Enemies.Count;
                    TutorialEndFlag endflag = new TutorialEndFlag(true, spawnEnemyCount);
                    System.Action<EnemyDefeatedEvent> action = (EnemyDefeatedEvent ev) => endflag._count--;

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
                    yield return EndWaitCoroutine(endflag);

                    EventBus.Subscribe<WaveEndEvent>(action);
                    yield return EndWaitCoroutine(endflag);
                    EventBus.Unsubscribe<WaveEndEvent>(action);
                }
                break;

            //case TutorialWave.ClearConditions.GetCollectible:
            //    {
            //        TutorialEndFlag endflag = new TutorialEndFlag(true, 3);
            //        System.Action action = () => endflag._count--;

            //        yield return EndWaitCoroutine(endflag);
            //    }
            //    break;

            default:
                Debug.LogError("チュートリアルの終了条件が登録されていません、クリアしたことにして次に進みます");
            break;
        }
    }

    void TutorialEnd(TutorialWave wave)
    {
        Debug.Log(wave.name + "を終了します！");
        // TutorialEndRequest request = wave.waveEndRequest;

    }

    bool IsClearTutorial(TutorialWave wave)
    {
        switch(wave.clearConditions)
        {
            case TutorialWave.ClearConditions.EnemyKill:

                return true;

            case TutorialWave.ClearConditions.WaveClear:

                return true;

            case TutorialWave.ClearConditions.GetCollectible:

                return true;

                
        }
        Debug.LogError("チュートリアルの終了条件が登録されていません、クリアしたことにして次に進みます");
        return true;
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

    IEnumerator EndWaitCoroutine(TutorialEndFlag flag)
    {
        while(flag._count > 0 || flag._frag)
        {
            yield return null;
        }
    }
}
