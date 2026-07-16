// ------------------------------------------------------------
// File		: FieldBuilder.cs
// Summary	: フィールドの構築を担当するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-23
//
// Notes	:
// - フィールドの構築や初期化を担当するクラス
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// FieldData を元にフィールドの傾きを決め、床に反映し、クリスタルを生成する
    /// </summary>
    public class FieldBuilder : MonoBehaviour
    {
        [Header("フィールドデータ")]
        [SerializeField] private FieldData _fieldData;

        [Header("シーン参照")]
        [Tooltip("傾ける床のルートオブジェクト")]
        [SerializeField] private Transform _fieldRoot;

        // 他のシステムにも傾きを配るために保持しておく
        /// <summary>
        /// フィールドの傾き(クォータニオン)
        /// </summary>
        public Quaternion FieldRotation { get; private set; }

        /// <summary>
        /// フィールドの上方向(ワールド座標系)
        /// </summary>
        public Vector3 FieldUp => FieldRotation * Vector3.up;

        /// <summary>
        /// 開始の処理
        /// </summary>
        private void Awake()
        {
            Build();
        }


        private void Build()
        {
            if (_fieldData == null)
            {
                Debug.LogError("FieldData が設定されていません。");
                return;
            }

            // 1. 傾き回転を決定
            FieldRotation = Quaternion.Euler(_fieldData.FieldTilt, 0f, 0f);
            Transform root = _fieldRoot != null ? _fieldRoot : transform;
            FieldContext.Set(FieldRotation, root.position);

            // 2. 床の傾きを当てる
            root.rotation = FieldRotation;

            // 3. クリスタルを原点に同じ傾きで生成する
            if (_fieldData.CrystalPrefab != null)
            {
                var crystal = Instantiate(_fieldData.CrystalPrefab);
                crystal.transform.SetParent(root, false);
                crystal.transform.localPosition = _fieldData.CrystalLocalPosition;
                crystal.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
