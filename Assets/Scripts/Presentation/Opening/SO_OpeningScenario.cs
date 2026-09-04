// ------------------------------------------------------------
// File		: SO_OpeningScenario.cs
// Summary	: オープニングで表示する文章データ,かいが触る用
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;


namespace Game.Presentation.Opening
{
    /// <summary>
    /// オープニングで表示する文章データ
    /// </summary>
    [CreateAssetMenu(fileName = "SO_OpeningScenario", menuName = "CreatorKousien/Opening/Scenario")]
    public sealed class SO_OpeningScenario : ScriptableObject
    {
        [Header("--- BGM ---")]
        [Tooltip("Sound Dataに登録したBGMのName")]
        [SerializeField] private string _bgmName;

        [Header("--- スライドごとの文章 ---")]
        [SerializeField] private OpeningSlideScript[] _slides;

        public string BgmName => _bgmName;
        public IReadOnlyList<OpeningSlideScript> Slides => _slides ?? System.Array.Empty<OpeningSlideScript>();
    }


    /// <summary>
    /// オープニングのスライドごとの文章データ
    /// </summary>
    [System.Serializable]
    public sealed class OpeningSlideScript
    {
        [Tooltip("インスペクタ整理用の見出し")]
        [SerializeField] private string _label = "スライド";

        [Tooltip("順番に表示する文章")]
        [SerializeField, TextArea(2, 4)] private string[] _lines;

        [Tooltip("一文字あたりの表示間隔(秒)")]
        [SerializeField, Min(0f)] private float _charInterval = 0.05f;

        [Tooltip("0より大きいと、読み終わってからその秒数で自動的に次の行にすすむ！")]
        [SerializeField, Min(0f)] private float _autoAdvanceDelay = 0f;

        [Tooltip("文字送り音")]
        [SerializeField] private string _typeSeName;

        [Tooltip("文字送り音を何文字ごとに鳴らすか")]
        [SerializeField, Min(1)] private int _typeSeInterval = 3;

        public string Label => _label;
        public IReadOnlyList<string> Lines => _lines ?? System.Array.Empty<string>();
        public float CharInterval => _charInterval;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public string TypeSeName => _typeSeName;
        public int TypeSeInterval => _typeSeInterval;
    }
}


