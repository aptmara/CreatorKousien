//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   UIData.cs
//
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/

using System.Collections.Generic;
using UnityEngine;


namespace CreatorKousien.View.UI
{
    /// <summary>
    /// ゲーム進行状況
    /// </summary>
    public enum UIState
    {
        None,
        MainMenu,
        InGame,
        Pause,
        Result
    }

    /// <summary>
    /// レイヤー
    /// </summary>
    public enum ViewType
    {
        None,
        CardSlot,
        HpGauge,
        TurnDisplay,
        Result,
        GridCell
    }

    /// <summary>
    /// UI用の方向
    /// </summary>
    public enum UIDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }


    /// <summary>
    /// 一旦のカードデータ
    /// </summary>
    [System.Serializable]
    public class UICardData
    {
        public string MasterId;
        public string InstanceId;
        public string Name;
        public Sprite Icon;
        public string Description;
        public UIDirection Direction = UIDirection.None;
    }

    [System.Serializable]
    public class CardLayoutData
    {
        public Vector2 offset;
        public Vector2 size = new Vector2(200.0f,100.0f);
    }

    /// <summary>
    /// スロットの位置情報データ
    /// </summary>
    [System.Serializable]
    public class CardSlotLayoutData
    {
        public Vector2 CenterPosition;
        public List<CardLayoutData> CardLayouts = new List<CardLayoutData>();
    }

    /// <summary>
    /// 各￥レイヤーデータ
    /// </summary>
    [System.Serializable]
    public class LayerData
    {
        public int LayerOrder;
        public bool InteractionEnabled;
        public int TopLayerIndex;
    }

    /// <summary>
    /// リザルトデータ
    /// </summary>
    public struct ResultData
    {
        public bool IsWin;
        public int Score;
    }
}
