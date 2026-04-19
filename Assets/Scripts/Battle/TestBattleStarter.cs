// ------------------------------------------------------------
// File		: TestBattleStarter.cs
// Summary	: テストバトルのスタート地点。十字キーで移動コマンドを発行できるようにするだけのシンプルなクラス。
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - デバック用GameManagerのような役割を果たすクラスです。実際のゲームでは、GameManagerがこれらの配線を行うことになると思いますが、テストバトル用に簡略化してあります。
// - BattleSetupDataを使うように変更(4/18)
// ------------------------------------------------------------
using CreatorKousien.Battle;
using CreatorKousien.Command;
using CreatorKousien.Core;
using CreatorKousien.Data;
using CreatorKousien.Enemy;
using CreatorKousien.Field;
using CreatorKousien.Player;
using CreatorKousien.UseCase;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestBattleStarter : MonoBehaviour
{
    [Header("ステージセットアップデータ設定")]
    [Tooltip("インスペクターで作成した BattleSetupData をアタッチしてください")]
    [SerializeField] private BattleSetupData _setupData;

    [Header("Viewの参照")]
    [SerializeField] private FieldView _fieldView;

    [Header("UIプレファブ")]
    [Tooltip("TimerViewがアタッチされたプレファブをセットしてください")]
    [SerializeField] private CreatorKousien.View.TestTimerView _timerPrefab;

    private PlayerSystem _playerSystem;             /// PlayerSystemのインスタンスを保持する変数
    private GameMediator _mediator;                 /// GameMediatorのインスタンスを保持する変数
    private ActionTelegraphSystem telegraphSystem;  /// ActionTelegraphSystemのインスタンスを保持する変数
    private TurnManager _turnManager;               /// TurnManagerのインスタンスを保持する変数
    private GameEventBus _eventBus;

    private Dictionary<int, EnemyView> _enemyViews = new Dictionary<int, EnemyView>();

    private void Start()
    {
        // 0. BattleSetupDataが正しくアタッチされているか確認
        // ------------------------------------------------------------
        if (_setupData == null)
        {
            Debug.LogError("<color=red>[TestBattleStarter] BattleSetupData がセットされていません！インスペクターを確認してください！</color>");
            return;
        }



        // 1. システムの生成
        // ------------------------------------------------------------
        FieldService _fieldService = new FieldService();
        _fieldService.Initialize(_setupData.StageData);
        _fieldView.BuildView(_fieldService.State, _setupData.StageData);

        TileEffectSystem tileEffect = new TileEffectSystem(_fieldService.State);

        // バトルと敵管理のシステム
        BattleManager _battleManager = new BattleManager();
        telegraphSystem = new ActionTelegraphSystem();
        EnemySystem _enemySystem = new EnemySystem(telegraphSystem);



        // 2. 敵のシステムセットアップ
        // ------------------------------------------------------------
        foreach (var enemyInfo in _setupData.Enemies)
        {
            // システムに登録
            _enemySystem.SpawnEnemy(enemyInfo.ActorId, enemyInfo.EnemyData, enemyInfo.SpawnPosition);

            // 盤面のマスを占有状態にする
            _fieldService.UpdateOccupancy(enemyInfo.ActorId, -1, -1, enemyInfo.SpawnPosition.x, enemyInfo.SpawnPosition.y);

            // 視覚的なスポーン
            Vector3 worldPos = _fieldView.GetCellWorldPosition(enemyInfo.SpawnPosition.x, enemyInfo.SpawnPosition.y);

            // EnemyDataに設定されたPrefabを生成
            GameObject enemyObj = Instantiate(enemyInfo.EnemyData.EnemyPrefab, worldPos, Quaternion.identity);
            EnemyView view = enemyObj.GetComponent<EnemyView>();

            view.Initialize(enemyInfo.ActorId, worldPos, _fieldView.GetCellView(enemyInfo.SpawnPosition));
            _enemyViews.Add(enemyInfo.ActorId, view);
        }



        // 3. プレイヤーのシステム（裏側）と見た目（表側）の初期化
        // ------------------------------------------------------------
        _playerSystem = new PlayerSystem();
        Vector2Int pPos = _setupData.StageData.PlayerStartPosition;

        // 裏側のセットアップ
        _playerSystem.Initialize(_setupData.PlayerData, pPos);
        _fieldService.UpdateOccupancy(_playerSystem.RuntimeData.ActorId, -1, -1, pPos.x, pPos.y);

        // 表側のセットアップ
        Vector3 startWorldPos = _fieldView.GetCellWorldPosition(pPos.x, pPos.y);
        // PlayerDataに入っているPrefabを、初期座標に生成する！
        GameObject playerObj = Instantiate(_setupData.PlayerData.PlayerPrefab, startWorldPos, Quaternion.identity);
        // 生成したオブジェクトから PlayerView コンポーネントを取得する！
        PlayerView playerView = playerObj.GetComponent<PlayerView>();
        playerView.Initialize(startWorldPos);
        playerView.SetStandingTile(_fieldView.GetCellView(pPos));
        _fieldView.HighlightCell(pPos.x, pPos.y); // 初期位置をハイライトしておくとわかりやすいかも！



        // 4 イベントの配線
        // ------------------------------------------------------------
        _eventBus = new GameEventBus();

        // プレファブからタイマーを生成
        if (_timerPrefab != null)
        {
            // 画面上部に生成
            Vector3 timerPosition = new Vector3(4.375f, 3, 0);

            var timerInstance = Instantiate(_timerPrefab, timerPosition, Quaternion.identity);
            timerInstance.Initialize(_eventBus);
        }

        // 移動リクエストの集中管理
        _eventBus.OnActorMoveRequested += (actorId, targetGridPos) =>
        {
            // グリッド座標をワールド座標に変換する
            Vector3 worldPos = _fieldView.GetCellWorldPosition(targetGridPos.x, targetGridPos.y);
            GridCellView cellView = _fieldView.GetCellView(targetGridPos);

            // プレイヤーの場合
            if (actorId == _playerSystem.RuntimeData.ActorId)
            {
                playerView.UpdateTargetPosition(worldPos);
                playerView.SetStandingTile(cellView);
                _fieldView.HighlightCell(targetGridPos.x, targetGridPos.y);
            }
            // エネミーの場合
            else if (_enemyViews.TryGetValue(actorId, out var eView))
            {
                eView.MoveTo(worldPos);
                eView.SetStandingTile(cellView);
            }
        };


        // 被ダメージ通知を受け取ったら、PlayerViewの被弾エフェクトを鳴らす！！
        _eventBus.OnDamageTaken += (targetId, damage) =>
        {
            if (targetId == _playerSystem.RuntimeData.ActorId) playerView.PlayDamageEffect(damage);
            else if (_enemyViews.TryGetValue(targetId, out var eView)) eView.PlayDamageEffect();
        };

        // 死亡通知を受け取ったら、PlayerViewの死亡エフェクトを鳴らす！！
        _eventBus.OnActorDeath += (targetId) =>
        {
            if (targetId == _playerSystem.RuntimeData.ActorId) playerView.PlayDeathEffect();
            else if (_enemyViews.TryGetValue(targetId, out var eView))
            {
                eView.PlayDeathEffect();
                _enemyViews.Remove(targetId); // 辞書から削除
            }
        };


        // 攻撃範囲表示のリクエストを受け取ったら、FieldViewに盤面を光らせるように指示する！
        _eventBus.OnAttackAreaExecuted += (targetCells) =>
        {
            // 1. 盤面をオレンジ色に光らせる！
            _fieldView.ShowAttackArea(targetCells, true);

            // 2. 0.3秒後に消すコルーチンを回す
            StartCoroutine(HideAttackAreaRoutine(targetCells));
        };


        // 戻って来た通知を受け取って画面を動かす
        _eventBus.OnTelegraphRequested += (targetCells, isWarning) =>
        {
            _fieldView.ShowTelegraph(targetCells, isWarning);
        };
        _eventBus.OnAttackHit += (targetActorId) =>
        {
            Debug.Log($"<color=red>[View] ActorID:{targetActorId} に攻撃ヒットエフェクトを再生！</color>");
        };

        _fieldService.OnActorMoved += (actorId, x, y) =>
        {
            if (actorId == _playerSystem.RuntimeData.ActorId)
            {
                _playerSystem.SyncPosition(new Vector2Int(x, y));

                Vector3 targetWorldPos = _fieldView.GetCellWorldPosition(x, y);
                playerView.UpdateTargetPosition(targetWorldPos);
                playerView.SetStandingTile(_fieldView.GetCellView(new Vector2Int(x, y)));

                _fieldView.HighlightCell(x, y);
            }
        };

        _eventBus.OnActionLogicCompleted += (actorId) =>
        {
            StartCoroutine(WaitAnimationAndProceed());
        };

        // 5. UseCaseとDispatcherの紐づけ
        // ------------------------------------------------------------
        CommandDispatcher dispatcher = new CommandDispatcher();

        // コマンドディスパッチャーを生成して、移動コマンドを処理できるようにする！
        MoveUseCase moveUseCase = new MoveUseCase(_fieldService, tileEffect, _eventBus);
        AttackUseCase attackUseCase = new AttackUseCase(_battleManager, _fieldService, _playerSystem, _enemySystem, dispatcher, _eventBus);
        EnemyActionUseCase enemyUseCase = new EnemyActionUseCase(_enemySystem, _fieldService, _playerSystem, telegraphSystem,dispatcher);

        // UseCaseをDispatcherに登録
        dispatcher.Register<MoveCommand>(moveUseCase.Execute);
        dispatcher.Register<AttackCommand>(attackUseCase.Execute);
        dispatcher.Register<EnemyActionCommand>(enemyUseCase.Execute);


        // 6. TurnManagerの生成
        _turnManager = gameObject.AddComponent<TurnManager>();
        _turnManager.Initialize(dispatcher, _eventBus);

        // 7. Mediatorの生成と統合
        _mediator = new GameMediator();
        _mediator.Initialize(dispatcher, _eventBus, _turnManager);

        Debug.Log("テストバトルの配線完了！十字キーで移動コマンドを発行できます！");
    }

    private IEnumerator WaitAnimationAndProceed()
    {
        // 0.5秒のアニメーション待機をシミュレート
        yield return new WaitForSeconds(0.5f);

        // Mediator経由で安全にTurnManagerへ報告
        _mediator.CompleteCurrentActionAnimation();
    }

    private void Update()
    {
        if (_mediator == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int pId = _playerSystem.RuntimeData.ActorId; // プレイヤーのID(1)

        // 移動テスト
        if (keyboard.upArrowKey.wasPressedThisFrame)
            _mediator.SubmitPlayerAction(new ActionRuntimeData(pId, GridDirection.Up));
        if (keyboard.downArrowKey.wasPressedThisFrame)
            _mediator.SubmitPlayerAction(new ActionRuntimeData(pId, GridDirection.Down));
        if (keyboard.leftArrowKey.wasPressedThisFrame)
            _mediator.SubmitPlayerAction(new ActionRuntimeData(pId, GridDirection.Left));
        if (keyboard.rightArrowKey.wasPressedThisFrame)
            _mediator.SubmitPlayerAction(new ActionRuntimeData(pId, GridDirection.Right));


        //  Zキーで攻撃を発動
        if (keyboard.zKey.wasPressedThisFrame)
        {
            // 目の前のマスを攻撃すると仮定
            Vector2Int targetPos = _playerSystem.RuntimeData.Position + new Vector2Int(1, 0);
            List<Vector2Int> targets = new List<Vector2Int> { targetPos };

            // 攻撃データを定義
            AttackProperty attackProp = new AttackProperty
            {
                Type = AttackPatternType.Normal,
                DamageMultiplier = 1.5f, // 威力1.5倍！
                HitCount = 1
            };

            // 攻撃コマンドを送る
            _mediator.SubmitPlayerAction(new ActionRuntimeData(pId, attackProp, targets));
        }


        // Xキーを押すと30ダメージ受けるテスト（HP減少→被ダメージエフェクト→死亡エフェクトの一連の流れを確認するためのもの）
        if (keyboard.xKey.wasPressedThisFrame)
        {
            int damage = 30; // 30ダメージ受ける

            // 1. システムのHPを減らす（本当はUseCase経由ですが、今回はテスト用で直接）
            _playerSystem.ChangeHp(-damage);

            // 2. 画面にエフェクトを出すようにEventBusで通知！
            _mediator.EventBus.PublishDamageTaken(pId, damage);

            // 3. もしHPが0以下になっていたら、死亡通知も飛ばす！
            if (_playerSystem.RuntimeData.CurrentHp <= 0)
            {
                _mediator.EventBus.PublishActorDeath(pId);
            }
        }

        // Cキー: 敵の行動をテスト
        if (keyboard.cKey.wasPressedThisFrame)
        {
            // _mediator.SendCommand(new EnemyActionCommand(2));
        }

        // Vキー: ターン進行！
        if (keyboard.vKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[TestBattleStarter] ターン進行！敵が行動します！</color>");

            // 1. 予告のターンを減らす
            telegraphSystem.TickAll();

            // 2. 残りのターンが0以下になった予告を発動する
            var expiredTelegraphs = telegraphSystem.ExtractExpiredTelegraph();

            foreach (var t in expiredTelegraphs)
            {
                _mediator.SendCommand(new AttackCommand(t.SourceActorId, t.AttackInfo, t.TargetCells));
            }
        }
    }

    /// <summary>
    /// 盤面の光を消すコルーチン。攻撃エフェクトが残る時間を待ってから、FieldViewに光を消すように指示する。
    /// </summary>
    /// <param name="targetCells"></param>
    /// <returns></returns>
    private System.Collections.IEnumerator HideAttackAreaRoutine(List<Vector2Int> targetCells)
    {
        // 攻撃のエフェクトが残る時間
        yield return new WaitForSeconds(0.3f);

        // 盤面の光を消す
        _fieldView.ShowAttackArea(targetCells, false);
    }
}
