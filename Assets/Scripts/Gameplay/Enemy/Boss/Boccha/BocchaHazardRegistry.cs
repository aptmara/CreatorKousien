// ------------------------------------------------------------
// File		: BocchaHazardRegistry.cs
// Summary	: ボスのハザードを管理するレジストリ
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - ボスの爆発時に一括で消すために使用!
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 生存中のお邪魔アイテムを管理する。
    /// </summary>
    public static class BocchaHazardRegistry
    {
        private static readonly List<BocchaHazardBase> _actives = new List<BocchaHazardBase>();

        /// <summary>
        /// 生存中のお邪魔アイテム一覧。
        /// </summary>
        public static IReadOnlyList<BocchaHazardBase> Actives => _actives;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _actives.Clear();

        public static void Register(BocchaHazardBase hazard)
        {
            if (hazard == null || _actives.Contains(hazard))
            {
                return;
            }

            _actives.Add(hazard);
        }


        public static void Unregister(BocchaHazardBase hazard)
        {
            if (hazard == null)
            {
                return;
            }
            _actives.Remove(hazard);
        }


        /// <summary>
        /// 生存中のお邪魔アイテムをすべて消す
        /// </summary>
        public static void DespawnAll()
        {
            // ForceDespawnの中でUnregisterされるので、foreachではなくforループで回す
            for (int i = _actives.Count - 1; i >= 0; --i)
            {
                if (_actives[i] == null)
                    continue;

                _actives[i].ForceDespawn();
            }

            _actives.Clear();
        }


        /// <summary>
        /// 指定したハザードタイプの生存中のお邪魔アイテムをすべて消す
        /// </summary>
        /// <param name="hazardType">お邪魔アイテムの種類</param>
        public static void DespawnAll(BocchaHazardType hazardType)
        {
            for (int i = _actives.Count - 1; i >= 0; i--)
            {
                if (_actives[i] == null || _actives[i].HazardType != hazardType)
                    continue;

                _actives[i].ForceDespawn();
            }
        }
    }
}
