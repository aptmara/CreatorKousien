// ------------------------------------------------------------
// File		: FieldContext.cs
// Summary	: フィールドのコンテキストを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-23
//
// Notes	:
// - フィールドの傾きやその他のコンテキスト情報を管理するクラス
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// フィールドのコンテキストを管理するクラス
    /// </summary>
    public static class FieldContext
    {
        /// <summary>
        /// フィールドの傾きが設定され、使用可能かどうかを示すフラグ
        /// </summary>
        public static bool IsReady { get; private set; } = false;


        /// <summary>
        /// フィールドの上方向(ワールド座標系)
        /// </summary>
        public static Vector3 Up { get; private set; } = Vector3.up;


        /// <summary>
        /// フィールドの傾き(クォータニオン)
        /// </summary>
        public static Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// 重力方向(正規化・下向き = -Up)
        /// </summary>
        public static Vector3 GravityDir => -Up;

        /// <summary>
        /// フィールドの傾きを設定するメソッド
        /// </summary>
        /// <param name="rotation"></param>
        public static void Set(Quaternion rotation)
        {
            Rotation = rotation;
            Up = Rotation * Vector3.up;
            IsReady = true;
        }

        /// <summary>
        /// フィールドのコンテキストをリセットするメソッド
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            IsReady = false;
            Up = Vector3.up;
            Rotation = Quaternion.identity;
        }
    }

}
