// 制作者: 山内陽
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の状態異常に応じたVFXとマテリアル変化を管理するコンポーネント。
    /// EnemyController と同一 GameObject にアタッチして使用する（オプション）。
    /// </summary>
    public class EnemyStatusAilmentVFX : MonoBehaviour
    {
        /// <summary>状態異常の種類ごとのVFX・マテリアル設定。</summary>
        [Serializable]
        public class StatusAilmentVFXEntry
        {
            [Tooltip("CollectibleType.ToString() と一致させる（例: Poison, Ice）")]
            public string DebuffType;
            [Tooltip("状態異常中にループ再生するVFXプレハブ")]
            public GameObject LoopVFXPrefab;
            [Tooltip("状態異常のまま敵が死亡したときに再生するVFXプレハブ")]
            public GameObject DeathVFXPrefab;
            [Tooltip("状態異常中に敵本体のレンダラーに追加するオーバーレイマテリアル（省略可）")]
            public Material OverlayMaterial;
            [Tooltip("VFX直下の子オブジェクトに適用するスケール（ゼロの場合はグローバル設定を使用）")]
            public Vector3 ChildScale;
        }

        [SerializeField]
        [Tooltip("状態異常の種類ごとのVFX・マテリアル設定")]
        private StatusAilmentVFXEntry[] _vfxEntries = Array.Empty<StatusAilmentVFXEntry>();

        [SerializeField]
        [Tooltip("VFXをアタッチする基準Transform（敵の中心など）")]
        private Transform _vfxRoot;

        [SerializeField]
        [Tooltip("VFXの生成位置のローカルオフセット")]
        private Vector3 _vfxOffset = new Vector3(0.24f, 1.31f, 0f);

        [SerializeField]
        [Tooltip("VFX直下の子オブジェクトに適用するスケール")]
        private Vector3 _vfxChildScale = new Vector3(3.62f, 3.62f, 3.62f);

        // アクティブなループVFX: debuffType → インスタンス
        private readonly Dictionary<string, GameObject> _activeLoopVFXs = new();
        // レンダラーごとの元マテリアル退避: debuffType → (renderer, 元materials)
        private readonly Dictionary<string, List<(Renderer renderer, Material[] originalMaterials)>> _savedMaterials = new();

        private SkinnedMeshRenderer[] _renderers;
        private EnemyDebuffManager _debuffManager;
        private bool _applyOverlayMaterial = true;

        /// <summary>
        /// EnemySpawner からスポーン時にエントリと VFX ルートを注入する。
        /// Initialize() より前に呼ぶこと。
        /// </summary>
        /// <param name="applyOverlayMaterial">false にするとマテリアルオーバーレイをスキップする（ボス等）。</param>
        public void SetupEntries(StatusAilmentVFXEntry[] entries, Transform vfxRoot, Vector3 vfxOffset = default, bool applyOverlayMaterial = true, Vector3 childScale = default)
        {
            _vfxEntries = entries ?? System.Array.Empty<StatusAilmentVFXEntry>();
            if (vfxRoot != null) _vfxRoot = vfxRoot;
            _vfxOffset = vfxOffset;
            _applyOverlayMaterial = applyOverlayMaterial;
            if (childScale != default) _vfxChildScale = childScale;
        }

        /// <summary>
        /// EnemyController.Initialize() から呼ばれる初期化処理。
        /// </summary>
        public void Initialize(EnemyDebuffManager debuffManager, EnemyController controller)
        {
            _debuffManager = debuffManager;
            _debuffManager.OnDebuffAdded += HandleDebuffAdded;
            _debuffManager.OnDebuffRemoved += HandleDebuffRemoved;
            controller.OnDefeated += HandleDefeated;

            _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (_vfxRoot == null) _vfxRoot = transform;
        }

        private void OnDestroy()
        {
            if (_debuffManager != null)
            {
                _debuffManager.OnDebuffAdded -= HandleDebuffAdded;
                _debuffManager.OnDebuffRemoved -= HandleDebuffRemoved;
            }
        }

        // ─────────────────────────────────────────
        // イベントハンドラ
        // ─────────────────────────────────────────

        private void HandleDebuffAdded(string debuffType)
        {
            StatusAilmentVFXEntry entry = FindEntry(debuffType);
            if (entry == null) return;

            SpawnLoopVFX(debuffType, entry);
            if (_applyOverlayMaterial) ApplyOverlayMaterial(debuffType, entry);
        }

        private void HandleDebuffRemoved(string debuffType)
        {
            DestroyLoopVFX(debuffType);
            RestoreMaterials(debuffType);
        }

        private void HandleDefeated()
        {
            // アクティブな状態異常ごとに Death VFX を生成
            foreach (var kvp in _activeLoopVFXs)
            {
                StatusAilmentVFXEntry entry = FindEntry(kvp.Key);
                if (entry?.DeathVFXPrefab != null)
                {
                    Vector3 spawnPos = _vfxRoot.position + _vfxOffset;
                    GameObject deathVfx = Instantiate(entry.DeathVFXPrefab, spawnPos, _vfxRoot.rotation);
                    
                    Vector3 scale = (entry.ChildScale != Vector3.zero && entry.ChildScale != Vector3.one) ? entry.ChildScale : _vfxChildScale;
                    
                    foreach (Transform child in deathVfx.transform)
                    {
                        child.localScale = scale;
                    }
                    foreach (ParticleSystem ps in deathVfx.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        if (ps.transform.parent != deathVfx.transform)
                        {
                            ps.transform.localScale = scale;
                        }
                    }
                }
            }

            // ループVFX・マテリアルをすべてクリーンアップ
            foreach (var key in new List<string>(_activeLoopVFXs.Keys))
            {
                DestroyLoopVFX(key);
                RestoreMaterials(key);
            }
        }

        // ─────────────────────────────────────────
        // ループVFX制御
        // ─────────────────────────────────────────

        private void SpawnLoopVFX(string debuffType, StatusAilmentVFXEntry entry)
        {
            if (entry.LoopVFXPrefab == null || _activeLoopVFXs.ContainsKey(debuffType)) return;

            Vector3 spawnPos = _vfxRoot.position + _vfxOffset;
            GameObject vfx = Instantiate(entry.LoopVFXPrefab, spawnPos, _vfxRoot.rotation, _vfxRoot);
            vfx.transform.localPosition = _vfxOffset;

            // 直下の子オブジェクトおよびパーティクルシステムにスケールを適用（エントリ設定優先、未設定時はグローバル値）
            Vector3 scale = (entry.ChildScale != Vector3.zero && entry.ChildScale != Vector3.one) ? entry.ChildScale : _vfxChildScale;
            
            // 直下の子オブジェクト
            foreach (Transform child in vfx.transform)
            {
                child.localScale = scale;
            }
            
            // さらに階層が深い場合を考慮し、全パーティクルシステムのスケールも更新
            foreach (ParticleSystem ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps.transform.parent != vfx.transform) // 直下の子はすでに変更済みのためスキップ
                {
                    ps.transform.localScale = scale;
                }
            }

            _activeLoopVFXs[debuffType] = vfx;
        }

        private void DestroyLoopVFX(string debuffType)
        {
            if (!_activeLoopVFXs.TryGetValue(debuffType, out var vfx)) return;

            if (vfx != null) Destroy(vfx);
            _activeLoopVFXs.Remove(debuffType);
        }

        // ─────────────────────────────────────────
        // マテリアルオーバーレイ制御
        // ─────────────────────────────────────────

        private void ApplyOverlayMaterial(string debuffType, StatusAilmentVFXEntry entry)
        {
            if (entry.OverlayMaterial == null || _renderers == null) return;
            if (_savedMaterials.ContainsKey(debuffType)) return;

            var saved = new List<(Renderer, Material[])>();
            foreach (var r in _renderers)
            {
                Material[] original = r.materials;
                saved.Add((r, original));

                var newMats = new Material[original.Length + 1];
                original.CopyTo(newMats, 0);
                newMats[original.Length] = entry.OverlayMaterial;
                r.materials = newMats;
            }
            _savedMaterials[debuffType] = saved;
        }

        private void RestoreMaterials(string debuffType)
        {
            if (!_savedMaterials.TryGetValue(debuffType, out var saved)) return;

            foreach (var (r, original) in saved)
            {
                if (r != null) r.materials = original;
            }
            _savedMaterials.Remove(debuffType);
        }

        // ─────────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────────

        private StatusAilmentVFXEntry FindEntry(string debuffType)
        {
            foreach (var entry in _vfxEntries)
            {
                if (entry.DebuffType == debuffType) return entry;
            }
            return null;
        }
    }
}
