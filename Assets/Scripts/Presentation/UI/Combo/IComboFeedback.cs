// ================================================================================
// File         : IComboFeedback.cs
// Author       : Iwai Shogo
//
// Description  : コンボや猶予時間の変化に応じたUI/VFX演出を適用するためのインターフェース。
// Created      : 2026-06-08
// ================================================================================

using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    public interface IComboFeedback
    {
        /// <summary>
        /// 初期化処理
        /// </summary>
        void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText);

        /// <summary>
        /// コンボ数や猶予割合が更新された時に呼ばれる演出更新処理
        /// </summary>
        void OnUpdate(int currentCombo, float durationRatio);

        /// <summary>
        /// コンボ数が途切れてリセットされた時の演出処理
        /// </summary>
        void OnReset();
    }
}
