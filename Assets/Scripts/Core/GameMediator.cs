using UnityEngine;

/// <summary>
/// ターンの進行管理クラス
/// </summary>
public class GameMediator
{
    int turnNum;

    GameFlow flow;


    BattleManager _battleManager;
   

    public void Initialize()
    {
        turnNum = 0;
        flow = new GameFlow();
        
    }

    public void Update()
    {
        // turnManagerから今のフェーズを取得

    }

    public void SetBattleManager(BattleManager battleManager)
    {
        _battleManager = battleManager;
    }
}


class GameFlow
{
    AttackUseCase _attackUseCase;
    MoveUseCase _moveUseCase;

    void PlayerTurn()
    {
        // 各種UseCaseの呼び出し
        _moveUseCase.Execute();
        _attackUseCase.Execute();

    }

    void EnemyTurn()
    {
        // 各種UseCaseの呼び出し
        _attackUseCase.Execute();
    }


}
