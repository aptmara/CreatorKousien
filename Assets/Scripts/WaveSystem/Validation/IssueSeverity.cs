// ------------------------------------------------------------
// File		: IssueSeverity.cs
// Summary	: 検証結果の深刻度を定義します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - Errorが1件でもあれば、そのデータはPlayで必ず失敗します。
// - Warningは失敗しないが、意図しない挙動になり得る状態です。
// ------------------------------------------------------------
namespace Game.WaveSystem
{
    public enum IssueSeverity
    {
        /// <summary>
        /// 情報。意図しない挙動にはならない状態。
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告。意図しない挙動になる可能性がある状態。
        /// </summary>
        Warning = 1,

        /// <summary>
        /// エラー。Playで必ず失敗する状態。
        /// </summary>
        Error = 2,
    }
}

