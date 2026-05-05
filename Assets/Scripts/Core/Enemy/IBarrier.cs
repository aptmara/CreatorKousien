// 制作者: 山内陽

namespace Game.Core.Enemy
{
    /// <summary>
    /// バリア機能の抽象化インターフェース。
    /// プロト: BarrierController（割合軽減）のみ実装。
    /// 将来的に属性対応バリアや多層バリアへ差し替え可能にするための契約。
    /// </summary>
    public interface IBarrier
    {
        /// <summary>現在バリアが有効かどうか</summary>
        bool IsActive { get; }

        /// <summary>
        /// 受けたゲージダメージを処理し、実際に通すダメージ量を返す。
        /// バリアが有効なら軽減、無効ならそのまま通す。
        /// </summary>
        /// <param name="rawDamage">加工前のゲージダメージ量</param>
        /// <returns>実際に適用するゲージダメージ量</returns>
        float ProcessGaugeDamage(float rawDamage);

        /// <summary>
        /// バリアの有効/無効を切り替える。
        /// ダウン中は無効化、復帰後に再有効化する用途を想定。
        /// </summary>
        /// <param name="active">true: 有効, false: 無効</param>
        void SetActive(bool active);
    }
}
