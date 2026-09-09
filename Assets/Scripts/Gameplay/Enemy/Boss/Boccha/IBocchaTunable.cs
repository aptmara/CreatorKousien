// ------------------------------------------------------------
// File		: IBocchaTunable.cs
// Summary	: ラウンドごとに調整値を受け取れるギミックのインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - コピーして使うので、実行時に値を変えても元のSOには影響しない
// ------------------------------------------------------------
namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ラウンドの調整値を適用できるギミック。
    /// </summary>
    public interface IBocchaTunable
    {
        /// <summary>
        /// ラウンドの調整値を適用する
        /// </summary>
        /// <param name="tuning">適用する調整値</param>
        void ApplyTuning(BocchaRoundTuning tuning);
    }
}
