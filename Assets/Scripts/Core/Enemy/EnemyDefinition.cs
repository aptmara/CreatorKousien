// 制作者: 山内陽
using UnityEngine;

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

        [Header("本体HP")]
        [Tooltip("ダウン中にのみ削れるHP。")]
        public float MaxHp = 300f;

        [Header("ダウン挙動")]
        [Tooltip("ダウン持続時間[秒]。経過後にNormal状態へ復帰する。")]
        public float DownDuration = 5f;

        [Header("バリア")]
        [Tooltip("バリアあり/なし。なしの場合BarrierControllerは無効状態で初期化される。")]
        public bool HasBarrier = false;
        [Header("バリアゲージ")]
        [Tooltip("ゲージの最大値。0になるとダウンする。")]
        public float MaxGauge = 100f;

        [Header("攻撃力")]
        [Tooltip("防衛ラインに対する攻撃力")]
        public float AttackPower = 5.0f;

        [Header("攻撃間隔")]
        [Tooltip("攻撃する際の間隔、EnemyRisingで上り切ってからカウントされる")]
        public float Attackinterval = 5.0f;

        [Header("回復待機時の復帰間隔")]
        [Tooltip("回復待機に移行した際に復帰するまでの間隔")]
        public float HealRegenWaitTime = 7.0f;

        [Header("回復力")]
        [Tooltip("1秒あたりの回復力")]
        public float HealPower = 30.0f;

    }
}
