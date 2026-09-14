// 制作者: 山内陽
// 横移動つかいずらいので新規実装します - 2026/09/14 Asano
using UnityEngine;


namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の横移動の方式。
    /// </summary>
    public enum LateralMoveType
    {
        /// <summary>横移動しない</summary>
        None = 0,

        /// <summary>LateralCurveに沿って決まった動きを繰り返す（従来方式）</summary>
        Curve = 1,

        /// <summary>移動可能な横幅の中をランダムに行ったり来たりする</summary>
        RandomWander = 2,
    }
}


    namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の性能定義。ScriptableObjectで管理することでコード変更なしにバリエーション量産が可能。
    /// 各フィールドの意味・単位はインラインコメントを参照。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyDefinition", menuName = "Game/Enemy/EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("識別")]
        [Tooltip("ゲームシーン内でユニークな文字列。EventBusのフィルタリングに使用。")]
        public string EnemyId = "Enemy_01";

        [Tooltip("有効にするとフィールド中央アンカーから出現するボスとして扱う。")]
        public bool IsBoss = false;

        [Header("敵実体オブジェクト")]
        [Tooltip("敵の実体として生成されるオブジェクト、敵のプレファブを設定する")]
        public GameObject EnemyBody;

        [Header("本体基本HP")]
        [Tooltip("敵毎の標準となる数値、倍率をかけることで実数値となる")]
        public float MaxHp = 300f;

        [Header("ダメージ無効")]
        [Tooltip("ONにすると通常ダメージを一切受けない。ギミックからの強制撃破のみ有効")]
        public bool IsDamageImmune = false;

        [Header("経験値倍率")]
        [Tooltip("経験値倍率、HP実数値にかけることで実数地となる")]
        public float ExpRate = 1.0f;

        [Header("ダウン挙動")]
        [Tooltip("ダウン持続時間[秒]。経過後にNormal状態へ復帰する。")]
        public float DownDuration = 5f;

        [Header("バリア")]
        [Tooltip("バリアあり/なし。なしの場合BarrierControllerは無効状態で初期化される。")]
        public bool HasBarrier = false;
        [Header("バリアゲージ")]
        [Tooltip("ゲージの最大値。0になるとダウンする。")]
        public float MaxGauge = 100;

        [Header("バリア実体オブジェクト")]
        [Tooltip("バリアの実体として生成されるオブジェクト、バリア系のプレファブを設定する")]
        public GameObject BarrierBody;

        [Header("バリア破壊時の最大値減少率")]
        [Tooltip("バリアが破壊された時に減る最大値の割合")]
        public float barrierBreakMaxLossRate = 0.7f;

        [Header("攻撃力")]
        [Tooltip("防衛ラインに対する攻撃力")]
        public float AttackPower = 5.0f;

        [Header("攻撃間隔")]
        [Tooltip("攻撃する際の間隔、EnemyRisingで上り切ってからカウントされる")]
        public float Attackinterval = 5.0f;

        [Header("攻撃モーション時間")]
        [Tooltip("攻撃モーション開始から終了までの時間")]
        [Min(0f)]
        public float AttackMotionTime = 3.0f;

        [Header("攻撃前隙")]
        [Tooltip("攻撃の前隙")]
        [Min(0f)]
        public float AttackStartUpTime = 1.5f;


        [Header("回復待機時の復帰間隔")]
        [Tooltip("回復待機に移行した際に復帰するまでの間隔")]
        public float HealRegenWaitTime = 7.0f;

        [Header("回復力")]
        [Tooltip("1秒あたりの回復力")]
        public float HealPower = 30.0f;

        [Header("上昇合計時間")]
        [Tooltip("敵の上昇にかかる秒数")]
        public float RiseDuration = 30.0f;

        [Header("--- 横移動 ---")]

        [Tooltip("横移動の方式。Curveは従来どおりLateralCurve/LateralDurationを使う")]
        public LateralMoveType LateralMove = LateralMoveType.Curve;

        [Header("横移動の速度")]
        [Tooltip("敵の横移動一回の完了速度（Curve方式でのみ使用）")]
        public float LateralDuration = 30.0f;


        [Header("--- ランダム横移動(RandomWander)専用 ---")]

        [Tooltip("横移動の最高速度[m/秒]")]
        [Min(0f)] public float LateralMaxSpeed = 2.5f;

        [Tooltip("目標へ寄っていく滑らかさ[秒]。小さいほどキビキビ、大きいほどふわふわ動く")]
        [Min(0.01f)] public float LateralSmoothTime = 0.6f;

        [Tooltip("スポーン範囲の左右をどれだけ内側に詰めるか[m]。負の値で範囲を広げる")]
        public float LateralRangePadding = 0.0f;

        [Tooltip("次の目標を決める時、現在地から最低これだけ離す[m]。その場足踏みの防止")]
        [Min(0f)] public float LateralMinMoveDistance = 1.5f;

        [Tooltip("目標に到達してから次を決めるまでの待ち時間[秒] X=最小 Y=最大")]
        public Vector2 LateralHoldTimeRange = new Vector2(0.1f, 0.8f);


        [Header("落下合計時間")]
        [Tooltip("敵の落下にかかる秒数")]
        public float DropDuration = 1.0f;

        [Header("ずり落ち合計時間")]
        [Tooltip("バリア破壊時のずり落ちにかかる秒数")]
        public float BarrierBreakDuration = 0.5f;

        [Header("ダメージずり落ち合計時間")]
        [Tooltip("被ダメージ時のずり落ちにかかる秒数")]
        public float DamageDropDuration = 0.2f;

        [Header("上昇カーブ")]
        [Tooltip("敵の上昇カーブ")]
        public AnimationCurve RiseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [Header("横移動カーブ")]
        [Tooltip("敵の横移動カーブ")]
        public AnimationCurve LateralCurve = AnimationCurve.Linear(0.0f, 0.0f, 0.0f, 0.0f);

        [Header("落下カーブ")]
        [Tooltip("敵の落下カーブ")]
        public AnimationCurve DropCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

        [Header("ずり落ちカーブ")]
        [Tooltip("バリア破壊時のずり落ちカーブ")]
        public AnimationCurve BarrierBreakCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

        [Header("ダメージずり落ちカーブ")]
        [Tooltip("被ダメージ時のずり落ちカーブ")]
        public AnimationCurve DamageDropCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

        [Header("ずり落ち量")]
        [Tooltip("バリア破壊時のずり落ち量")]
        public float BreakDropDistance = 0.2f;


        [Header("ダメージずり落ち量")]
        [Tooltip("被ダメージ時のずり落ち量")]
        public float DamageDropDistance = 0.015f;


        [Header("--- ボスの生成位置 ---")]

        [Tooltip("ONなら個別設定を使用。OFFなら従来のボス出現位置を使用する")]
        public bool UseCustomBossSpawnPosition = false;

        [Tooltip("個別設定時の補正量。X・ZはField_Center、YはSpawn Base Pointが基準")]
        public Vector3 BossSpawnOffset = new Vector3(0f, 0f, 5f);
    }
}
