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
using UnityEngine;
using CreatorKousien.Command;
using CreatorKousien.UseCase;
using CreatorKousien.Core;
using UnityEngine.InputSystem;

public class TestBattleStarter : MonoBehaviour
{
    [Header("ステージ設定")]
    [SerializeField] private StageData _testStageData;
    [SerializeField] private FieldView _fieldView;

    [Header("プレイヤー設定")]
    [SerializeField] private PlayerData _testPlayerData;

    private CommandDispatcher _dispatcher;
    private PlayerSystem _playerSystem;

    private void Start()
    {
        // 1. システムを生成
        FieldService fieldService = new FieldService();
        fieldService.Initialize(_testStageData);
        _fieldView.BuildView(fieldService.State, _testStageData);

        TileEffectSystem tileEffect = new TileEffectSystem(fieldService.State);

        // 2. プレイヤーのシステム（裏側）と見た目（表側）の初期化
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

        // 3. イベントの配線
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

        // 4. UseCaseとDispatcherを生成
        MoveUseCase moveUseCase = new MoveUseCase(fieldService, tileEffect);
        _dispatcher = new CommandDispatcher(moveUseCase);

        Debug.Log("テストバトルの配線完了！十字キーで移動コマンドを発行できます！");
    }

    private void Update()
    {
        if (_dispatcher == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int pId = _playerSystem.RuntimeData.ActorId; // プレイヤーのID(1)

        if (keyboard.upArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(pId, GridDirection.Up, 1));
        if (keyboard.downArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(pId, GridDirection.Down, 1));
        if (keyboard.leftArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(pId, GridDirection.Left, 1));
        if (keyboard.rightArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(pId, GridDirection.Right, 1));
    }
}
