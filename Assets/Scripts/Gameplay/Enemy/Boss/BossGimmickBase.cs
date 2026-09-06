using UnityEngine;
using System.Collections.Generic;
using Game.Gameplay.Enemy.Boss;
using UnityEngine.Rendering;

public enum BossSocket
{
    Root,         // 足元・中心
    Head,         // 頭部
    LeftHand,     // 左手
    RightHand,    // 右手
    Muzzle,       // 発射口・口
    TargetPlayer  // プレイヤーの位置
}
/// <summary>
/// ギミック実行時にボスの各機能へアクセスするための参照コンテキスト
/// </summary>
public class BossContext
{
    public BossBattleFlowController Controller { get; }
    public Animator Animator { get; }
    public Transform Transform { get; }

    public BossPhaseMultipliers PhaseMultipliers { get; private set; } = BossPhaseMultipliers.Default;

    // 部位の割り当て（BossBattleFlowController側から初期化時に受け取る）
    private readonly Dictionary<BossSocket, Transform> _sockets;

    public BossContext(
        BossBattleFlowController controller,
        Animator animator,
        Transform transform,
        Dictionary<BossSocket, Transform> sockets = null)
    {
        Controller = controller;
        Animator = animator;
        Transform = transform;
        _sockets = sockets ?? new Dictionary<BossSocket, Transform>();
    }

    /// <summary>
    /// 指定した部位の Transform を取得する
    /// </summary>
    public Transform GetSocket(BossSocket socket)
    {
        // 登録されていればそれを返し、なければボス本体のTransformをフォールバックとして返す
        if (_sockets.TryGetValue(socket, out var target) && target != null)
        {
            return target;
        }
        return Transform;
    }

    public void UpdatePhaseMultipliers(BossPhaseMultipliers multipliers)
    {
        PhaseMultipliers = multipliers;
    }
}

[System.Serializable]
public struct BossPhaseMultipliers
{
    [Tooltip("ダメージ量にかける倍率")]
    public float DamageMultiplier;

    [Tooltip("速度や移動関連にかける倍率")]
    public float SpeedMultiplier;

    [Tooltip("スポーン間隔にかける倍率(小さくすると頻度が上がる)")]
    public float SpawnIntervalMultiplier;

    public static BossPhaseMultipliers Default => new BossPhaseMultipliers
    {
        DamageMultiplier = 1.0f,
        SpeedMultiplier = 1.0f,
        SpawnIntervalMultiplier = 1.0f,
    };


}


/// <summary>
/// 各ギミック処理
/// </summary>
public abstract class BossGimmickSO : ScriptableObject
{
    public abstract bool IsComplete { get; }
    public abstract bool IsTick {  get; }
    public virtual BossGimmickData NextOverrideGimmick => null;

    protected BossContext Context { get; private set; }

    public virtual void Initialize(BossContext context)
    {
        Context = context;
    }
    /// <summary>
    /// ギミック実行
    /// </summary>
    public abstract void Execute();

    public virtual void Tick(float dt) { }

    public virtual void Cancel() { }

}

