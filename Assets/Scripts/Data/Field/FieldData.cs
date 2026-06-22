// ------------------------------------------------------------
// File		: FieldData.cs
// Summary	: フィールドに関するデータを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-23
//
// Notes	:
// - フィールドの状態や属性を管理するためのクラス
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Stage
{
    [CreateAssetMenu(fileName = "FieldData", menuName = "Game/Stage/Field Data")]
    public class FieldData : ScriptableObject
    {
        [Header("フィールドの本体")]
        [Tooltip("フィールドの傾き角")]
        [SerializeField] private float _fieldTilt = -35.0f;

        [Header("クリスタル")]
        [Tooltip("生成するクリスタルプレファブ")]
        [SerializeField] private GameObject _crystalPrefab;

        [Tooltip("生成するクリスタルの位置(フィールドのローカル座標)")]
        [SerializeField] private Vector3 _crystalLocalPosition = Vector3.zero;


        /// <summary>
        /// フィールドの傾き角(度)
        /// </summary>
        public float FieldTilt => _fieldTilt;

        /// <summary>
        /// 生成するクリスタルプレファブ
        /// </summary>
        public GameObject CrystalPrefab => _crystalPrefab;

        /// <summary>
        /// 生成するクリスタルの位置(フィールドのローカル座標)
        /// </summary>
        public Vector3 CrystalLocalPosition => _crystalLocalPosition;
    }
}
