// ================================================================================
// File         : Payload.cs
// Author       : Iwai Shogo
//
// Description  : 収集済みのアイテム1単位を表す軽量データ。
// Created      : 2026-05-06
// ================================================================================

using Game.Data.Collectables;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Collection
{
    /// <summary>
    /// 収集済みの1単位を表すデータ。
    /// </summary>
    public struct Payload
    {
        [Tooltip("CollectableDefinitionの一意なID")]
        public string DefinitionId;

        [Tooltip("敵本体へ与えるダメージ")]
        public float Power;

        [Tooltip("敵ゲージへ与えるダメージ")]
        public float GaugeDamage;

        [Tooltip("アイテムの重さ")]
        public float Weight;

        [Tooltip("アイテムの属性")]
        public CollectableElement Element;

        [Tooltip("アイテムの形状")]
        public string Shape;

        [Tooltip("アイテムのサイズ")]
        public float Size;

        [Tooltip("追加の特性を管理するタグ")]
        public List<string> Tags;
    }
}
