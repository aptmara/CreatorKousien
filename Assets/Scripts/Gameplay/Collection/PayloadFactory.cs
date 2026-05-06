// ================================================================================
// File         : PayloadFactory.cs
// Author       : Iwai Shogo
//
// Description  : CollectableDefinitionからPayloadを生成するファクトリ。
// Created      : 2026-05-06
// ================================================================================

using Game.Data.Collectables;
using System.Collections.Generic;

namespace Game.Gameplay.Collection
{
    /// <summary>
    /// フィールド上のアイテムを回収した際、
    /// マスタデータの情報を元に、軽量なPayloadへ変換します。
    /// </summary>
    public static class PayloadFactory
    {
        /// <summary>
        /// マスタデータから初期状態のPayloadを生成します。
        /// </summary>
        /// <param name="definition">アイテムのマスタデータ(SO)</param>
        /// <returns>初期化されたPayload</returns>
        public static Payload Create(CollectableDefinition definition)
        {
            if (definition == null) return new Payload();

            return new Payload
            {
                DefinitionId = definition.Id,
                Power = definition.BasePower,
                GaugeDamage = definition.BaseGaugeDamage,
                Weight = definition.Weight,
                Element = definition.Element,
                Shape = "Default",
                Size = 1.0f,
                Tags = new List<string>()
            };
        }
    }
}
