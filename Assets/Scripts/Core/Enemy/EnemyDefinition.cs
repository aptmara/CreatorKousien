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

        [Header("攻撃ゲージ")]
        [Tooltip("ゲージの最大値。この値に到達すると攻撃行動を取る。")]
        public float MaxGauge = 100f;
        [Tooltip("毎秒の自然増加量（Time.deltaTime乗算）。小さいほどプレイヤーに余裕が生まれる。")]
        public float GaugeIncreaseRate = 10f;

        [Header("本体HP")]
        [Tooltip("ダウン中にのみ削れるHP。")]
        public float MaxHp = 300f;

        [Header("ダウン挙動")]
        [Tooltip("ダウン持続時間[秒]。経過後にNormal状態へ復帰する。")]
        public float DownDuration = 5f;

        [Header("バリア")]
        [Tooltip("バリアあり/なし。なしの場合BarrierControllerは無効状態で初期化される。")]
        public bool HasBarrier = false;
        [Tooltip("バリア有効時のゲージダメージ軽減割合。0=軽減なし, 1=完全無効。将来は属性ごとに持つ予定。")]
        [Range(0f, 1f)]
        public float BarrierDamageReduction = 0.5f;
    }
}
