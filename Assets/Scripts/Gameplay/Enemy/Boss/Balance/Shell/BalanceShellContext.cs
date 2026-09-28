/**
 * File: BalanceShellContext.cs
 * 
 * 天秤ボスのカップマジックギミックを実現するための共有する参照と進行フェーズ
 * 
 * 
 */
using Game.Gameplay.Collectibles;


namespace Game.Gameplay.Enemy.Boss
{

    /// <summary>
    /// カップマジック進行管理列挙型
    /// </summary>
    public enum BalanceShellPhase
    {
        Idle,
        Showcase,
        CoverIn,
        Shuffle,
        Reveal,
        Result,
        Cleanup,
    };

    public class BalanceShellContext
    {
        public BossContext Boss { get; }
        public BossBalanceBeamController Beam { get; }
        public RealisticBalanceScale Scale { get; }
        public CollectibleSpawner Collectibles { get; }

        public BalanceShellContext(
            BossContext boss,
            BossBalanceBeamController beam,
            RealisticBalanceScale scale,
            CollectibleSpawner collectibles
            )
        {
            Boss = boss;
            Beam = beam;
            Scale = scale;
            Collectibles = collectibles;
        }
    }
}
