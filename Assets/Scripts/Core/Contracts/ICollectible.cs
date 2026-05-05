// ------------------------------------------------------------
// File		: ICollectible.cs
// Summary	: アイテムの収集可能なオブジェクトが実装するインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6 ベース作成
// ------------------------------------------------------------
namespace Game.Core.Contracts
{
    /// <summary>
    /// アイテムの収集可能なオブジェクトが実装するインターフェース
    /// </summary>
    public interface ICollectible
    {
        /// <summary>
        /// 現在収集可能かどうかを判定する関数
        /// </summary>
        /// <returns>収集可能なら true</returns>
        bool CanCollect();

        /// <summary>
        /// 収集された際の処理を実装する関数
        /// </summary>
        void OnCollected();
    }
}
