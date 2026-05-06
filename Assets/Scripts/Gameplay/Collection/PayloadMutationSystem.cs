// ================================================================================
// File         : PayloadMutationSystem.cs
// Author       : Iwai Shogo
//
// Description  : 保持中Payloadの合体、形状変化、属性付与を処理する基盤。
// Created      : 2026-05-06
// ================================================================================

using System.Collections.Generic;

namespace Game.Gameplay.Collection
{
    /// <summary>
    /// ColletionBufferから呼び出される、アイテム変化ロジックの集約窓口。
    /// </summary>
    public class PayloadMutationSystem
    {
        public List<Payload> ProcessMutations(List<Payload> currentPayloads)
        {
            // TODO: FusionResolver や ShapeMorphTracker 等の処理

            return currentPayloads;
        }
    }
}
