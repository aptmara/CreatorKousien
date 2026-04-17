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
    [SerializeField] private StageData _testStageData;
    [SerializeField] private FieldView _fieldView;

    private CommandDispatcher _dispatcher;

    private void Start()
    {
        // 1. システムを生成
        FieldService fieldService = new FieldService();
        fieldService.Initialize(_testStageData);
        _fieldView.BuildView(fieldService.State, _testStageData);

        TileEffectSystem tileEffect = new TileEffectSystem(fieldService.State);

        // 2. UseCaseを生成
        MoveUseCase moveUseCase = new MoveUseCase(fieldService, tileEffect);

        // 3. Dispatcherを生成
        _dispatcher = new CommandDispatcher(moveUseCase);

        // テスト用に、ID:1のキャラを(0,0)に初期配置する
        fieldService.UpdateOccupancy(1, -1, -1, 0, 0);

        Debug.Log("テストバトルの配線完了！十字キーで移動コマンドを発行できます！");
    }

    private void Update()
    {
        if (_dispatcher == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.upArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(1, GridDirection.Up, 1));
        if (keyboard.downArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(1, GridDirection.Down, 1));
        if (keyboard.leftArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(1, GridDirection.Left, 1));
        if (keyboard.rightArrowKey.wasPressedThisFrame) _dispatcher.Dispatch(new MoveCommand(1, GridDirection.Right, 1));
    }
}
