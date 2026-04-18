// ------------------------------------------------------------
// File		: TestBattleStarter.cs
// Summary	: テストバトルのスタート地点。十字キーで移動コマンドを発行できるようにするだけのシンプルなクラス。
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - デバック用GameManagerのような役割を果たすクラスです。実際のゲームでは、GameManagerがこれらの配線を行うことになると思いますが、テストバトル用に簡略化してあります。
// ------------------------------------------------------------
using CreatorKousien.Command;
using CreatorKousien.Core;
using CreatorKousien.Data;
using CreatorKousien.Field;
using CreatorKousien.Player;
using CreatorKousien.UseCase;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CreatorKousien.Battle;

public class TestBattleStarter : MonoBehaviour
{
    [Header("ステージ設定")]
    [SerializeField] private StageData _testStageData;
    [SerializeField] private FieldView _fieldView;

    [Header("プレイヤー設定")]
    [SerializeField] private PlayerData _testPlayerData;

    private PlayerSystem _playerSystem;         /// PlayerSystemのインスタンスを保持する変数
    private GameMediator _mediator;             /// GameMediatorのインスタンスを保持する変数

    private void Start()
    {
        // ----- 1. システムを生成 -----
        FieldService fieldService = new FieldService();
        fieldService.Initialize(_testStageData);
        _fieldView.BuildView(fieldService.State, _testStageData);

        TileEffectSystem tileEffect = new TileEffectSystem(fieldService.State);

        // BattleManagerの生成
        // BattleManager battleManager = new BattleManager();



        // ----- 2. プレイヤーのシステム（裏側）と見た目（表側）の初期化 -----
        _playerSystem = new PlayerSystem();
        Vector2Int startGridPos = _testStageData.PlayerStartPosition;

        // 裏側のセットアップ
        _playerSystem.Initialize(_testPlayerData, startGridPos);
        fieldService.UpdateOccupancy(_playerSystem.RuntimeData.ActorId, -1, -1, startGridPos.x, startGridPos.y);

        // 表側のセットアップ
        Vector3 startWorldPos = _fieldView.GetCellWorldPosition(startGridPos.x, startGridPos.y);
        // PlayerDataに入っているPrefabを、初期座標に生成する！
        GameObject playerObj = Instantiate(_testPlayerData.PlayerPrefab, startWorldPos, Quaternion.identity);
        // 生成したオブジェクトから PlayerView コンポーネントを取得する！
        PlayerView playerView = playerObj.GetComponent<PlayerView>();
        playerView.Initialize(startWorldPos);
        playerView.SetStandingTile(_fieldView.GetCellView(startGridPos));
        _fieldView.HighlightCell(startGridPos.x, startGridPos.y); // 初期位置をハイライトしておくとわかりやすいかも！



        // ----- 3. イベントの配線 -----
        GameEventBus eventBus = new GameEventBus();

        // 被ダメージ通知を受け取ったら、PlayerViewの死亡エフェクトを鳴らす！！
        eventBus.OnDamageTaken += (targetId, damage) =>
        {
           if (targetId == _playerSystem.RuntimeData.ActorId)
            {
                playerView.PlayDamageEffect(damage);
            }
        };

        // 死亡通知を受け取ったら、PlayerViewの死亡エフェクトを鳴らす！！
        eventBus.OnActorDeath += (targetId) =>
        {
            if (targetId == _playerSystem.RuntimeData.ActorId)
            {
                playerView.PlayDeathEffect();
            }
        };

        // 戻って来た通知を受け取って画面を動かす
        eventBus.OnTelegraphRequested += (targetCells, isWarning) =>
        {
            _fieldView.ShowTelegraph(targetCells, isWarning);
        };
        eventBus.OnAttackHit += (targetActorId) =>
        {
            Debug.Log($"<color=red>[View] ActorID:{targetActorId} に攻撃ヒットエフェクトを再生！</color>");
        };

        fieldService.OnActorMoved += (actorId, x, y) =>
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


        // ----- 4. UseCaseとDispatcherの紐づけ -----
        CommandDispatcher dispatcher = new CommandDispatcher();

        // コマンドディスパッチャーを生成して、移動コマンドを処理できるようにする！
        MoveUseCase moveUseCase = new MoveUseCase(fieldService, tileEffect);
        // AttackUseCase attackUseCase = new AttackUseCase(battleManager, fieldService, dispatcher);
        // EnemyActionUseCase enemyUseCase = new EnemyActionUseCase();

        // UseCaseをDispatcherに登録
        dispatcher.Register<MoveCommand>(moveUseCase.Execute);
        // dispatcher.Register<AttackCommand>(attackUseCase.Execute);


        // ----- 5. Mediatorを生成して、システムやビューを登録する -----
        _mediator = new GameMediator();
        _mediator.Initialize(dispatcher, eventBus);

        Debug.Log("テストバトルの配線完了！十字キーで移動コマンドを発行できます！");
    }

    private void Update()
    {
        if (_mediator == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int pId = _playerSystem.RuntimeData.ActorId; // プレイヤーのID(1)

        // 移動テスト
        if (keyboard.upArrowKey.wasPressedThisFrame) _mediator.SendCommand(new MoveCommand(pId, GridDirection.Up, 1));
        if (keyboard.downArrowKey.wasPressedThisFrame) _mediator.SendCommand(new MoveCommand(pId, GridDirection.Down, 1));
        if (keyboard.leftArrowKey.wasPressedThisFrame) _mediator.SendCommand(new MoveCommand(pId, GridDirection.Left, 1));
        if (keyboard.rightArrowKey.wasPressedThisFrame) _mediator.SendCommand(new MoveCommand(pId, GridDirection.Right, 1));


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
    }
}
