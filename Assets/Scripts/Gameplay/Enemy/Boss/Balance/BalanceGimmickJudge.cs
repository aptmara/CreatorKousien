using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gimmick_Judge", menuName = "Boss/Gimmicks/Balance/")]
public class BalanceGimmickJudge : BossGimmickSO
{
    [SerializeField] private GameObject _weaknessPrefab;
    [SerializeField] private GameObject _barrierAttackPrefab;

    private BossBalanceBeamController _beam;
    private bool _isComplete;

    public override bool IsComplete => _isComplete;
    public override bool IsTick => false;

    public override void Initialize(BossContext context)
    {
        base.Initialize(context);
        _beam = context.Transform.GetComponentInChildren<BossBalanceBeamController>();
    }

    public override void Execute()
    {
        bool weaknessOnLeft = UnityEngine.Random.value < 0.5f;

        SpawnAt(TraySide.Left, weaknessOnLeft ? _weaknessPrefab : _barrierAttackPrefab);
        SpawnAt(TraySide.Right, weaknessOnLeft ? _barrierAttackPrefab : _weaknessPrefab);

        _isComplete = true;
    }

    private void SpawnAt(TraySide side, GameObject prefab)
    {
        var socket = _beam.GetTraySocket(side);
        var obj = GameObject.Instantiate(prefab, socket.position, socket.rotation, socket);
        if (obj.TryGetComponent(out IBossTrayItem item)) _beam.RegisterItem(side, item);
    }
}
