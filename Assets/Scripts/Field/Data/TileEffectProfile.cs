// ------------------------------------------------------------
// File		: TileEffectProfile.cs
// Summary	: タイルの効果のプロファイルクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// -
// ------------------------------------------------------------
using System;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using CreatorKousien.Field;

namespace CreatorKousien.Field
{
    /// <summary>
    /// 床が保持する効果の発火タイミングと効果IDの定義
    /// </summary>
    [Serializable]
    public class TileEffectProfile
    {
        [Tooltip("マスに進入した瞬間に発動する効果のID")]
        public int OnEnterEffectId = -1;

        [Tooltip("マスから退出した瞬間に発動する効果のID")]
        public int OnExitEffectId = -1;

        [Tooltip("ターン終了時にそのマスにいた場合に発動する効果のID")]
        public int OnTurnEndEffectId = -1;

        [Tooltip("そのマスに滞在している間、継続的に付与される効果のID")]
        public int WhiteStayEffectId = -1;
    }
}
