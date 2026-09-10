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
        private static readonly List<IBocchaHazard> _actives = new List<IBocchaHazard>();

        /// <summary>
        /// 生存中のお邪魔アイテム一覧。
        /// </summary>
        public static IReadOnlyList<IBocchaHazard> Actives => _actives;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _actives.Clear();

        public static void Register(IBocchaHazard hazard)
        {
            if (hazard == null || _actives.Contains(hazard))
            {
                return;
            }

            _actives.Add(hazard);
        }


        public static void Unregister(IBocchaHazard hazard)
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
        /// <param name="despawnVfxPrefab">消える位置に出すVFX。nullなら出さない</param>
        /// <param name="vfxLifeTime">VFXを破棄するまでの時間</param>
        public static void DespawnAll(GameObject despawnVfxPrefab = null, float vfxLifeTime = 3.0f)
        {
            for (int i = _actives.Count - 1; i >= 0; --i)
            {
                // インターフェース型はUnityの破棄済み判定が効かないので、Objectへ落として確認する
                if (_actives[i] is UnityEngine.Object obj && obj == null) continue;

                // 唐突に消えたように見えないよう、消える位置に煙を出す
                if (despawnVfxPrefab != null)
                {
                    GameObject vfx = Object.Instantiate(despawnVfxPrefab, _actives[i].Position, Quaternion.identity);

                    Object.Destroy(vfx, vfxLifeTime);
                }

                _actives[i].ForceDespawn();
            }

            _actives.Clear();
        }


        /// <summary>
        /// 指定したハザードタイプの生存中のお邪魔アイテムをすべて消す
        /// </summary>
        /// <param name="hazardType">お邪魔アイテムの種類</param>
        public static void DespawnAll(BocchaHazardType hazardType, GameObject despawnVfxPrefab = null, float vfxLifeTime = 3.0f)
        {
            for (int i = _actives.Count - 1; i >= 0; i--)
            {
                // インターフェース型はUnityの破棄済み判定が効かないので、Objectへ落として確認する
                if (_actives[i] is UnityEngine.Object obj && obj == null) continue;

                // 指定された種類以外は残す
                if (_actives[i].HazardType != hazardType) continue;

                // 唐突に消えたように見えないよう、消える位置に煙を出す
                if (despawnVfxPrefab != null)
                {
                    GameObject vfx = Object.Instantiate(despawnVfxPrefab, _actives[i].Position, Quaternion.identity);
                    Object.Destroy(vfx, vfxLifeTime);
                }

                _actives[i].ForceDespawn();
            }
        }
    }
}
