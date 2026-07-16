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
        /// フィールド中心のワールド座標
        /// </summary>
        public static Vector3 Center { get; private set; } = Vector3.zero;

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
            Set(rotation, Vector3.zero);
        }

        /// <summary>
        /// フィールドの傾きと中心座標を設定するメソッド
        /// </summary>
        /// <param name="rotation">フィールドの傾き</param>
        /// <param name="center">フィールド中心のワールド座標</param>
        public static void Set(Quaternion rotation, Vector3 center)
        {
            Rotation = rotation;
            Up = Rotation * Vector3.up;
            Center = center;
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
            Center = Vector3.zero;
        }
    }

}
