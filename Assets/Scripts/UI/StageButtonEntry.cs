// ------------------------------------------------------------
// File        : StageButtonEntry.cs
// Summary     : ステージ選択ボタン1枠。ステージ名を表示し、
//               押下時に選択コールバックを呼ぶ。
//
// Author      : 山内
// Created     : 2026-04-18
//
// Input       : SelectSceneViewから Setup(data, onSelected) を呼ばれる
// Change      : ステージ名テキスト設定 / ボタンイベント登録
// Output      : 押下時に onSelected() を呼ぶ
// ------------------------------------------------------------
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CreatorKousien.Data;

/// <summary>
/// ステージ選択ボタン1枠のView。SelectSceneViewによってInstantiateされる。
/// </summary>
public class StageButtonEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;    // ステージ名
    [SerializeField] private Button          _button;      // クリック判定

    private Action _onSelected;

    /// <summary>SelectSceneViewから呼ばれる初期化。データとコールバックを受け取る。</summary>
    public void Setup(BattleSetupData data, Action onSelected)
    {
        _onSelected = onSelected;

        if (_nameText != null)
            _nameText.text = data != null ? data.name : "???";

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelected?.Invoke());
        }
    }
}
