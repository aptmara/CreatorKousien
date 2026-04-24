//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   CardCommandStack.cs
// @brief  カード方向コマンドを最大数まで保持するクラス
//         WASD入力をSlotDirectionに変換し、対応するカードをコマンドとして積む
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カード方向コマンドを最大数まで保持するクラス
/// WASD入力をSlotDirectionに変換し、対応するカードをコマンドとして積む
/// </summary>
public class CardCommandStack : MonoBehaviour
{
    [SerializeField] private InputProvider inputProvider;
    [SerializeField] private CardSlotController cardSlotController;
    [SerializeField] private CommandStackView commandStackView;

    [SerializeField] private int maxCommandCount = 5;

    private readonly List<UICardData> commandCards = new List<UICardData>();
    private readonly List<SlotDirection> commandDirections = new List<SlotDirection>();

    private void Start()
    {
        RefreshView();
    }

    private void Update()
    {
        if (cardSlotController == null)
        {
            return;
        }

        if (cardSlotController.IsHandLocked())
        {
            return;
        }

        if (inputProvider == null)
        {
            return;
        }

        if (inputProvider.IsKeyTrigger(KeyCode.W))
        {
            PushCommand(SlotDirection.Up);
        }
        else if (inputProvider.IsKeyTrigger(KeyCode.S))
        {
            PushCommand(SlotDirection.Down);
        }
        else if (inputProvider.IsKeyTrigger(KeyCode.A))
        {
            PushCommand(SlotDirection.Left);
        }
        else if (inputProvider.IsKeyTrigger(KeyCode.D))
        {
            PushCommand(SlotDirection.Right);
        }
        else if (inputProvider.IsKeyTrigger(KeyCode.Backspace))
        {
            PopCommand();
        }
    }

    /// <summary>
    /// 指定方向のカードをコマンドとして積む
    /// 最大数に達している場合は追加しない
    /// </summary>
    /// <param name="direction">入力された方向</param>
    public void PushCommand(SlotDirection direction)
    {
        if (commandCards.Count >= maxCommandCount)
        {
            return;
        }

        if (cardSlotController == null)
        {
            return;
        }

        UICardData card = cardSlotController.GetCard(direction);

        if (card == null)
        {
            return;
        }

        commandDirections.Add(direction);
        commandCards.Add(card);

        RefreshView();
    }

    /// <summary>
    /// 最後に積んだコマンドを取り消す
    /// </summary>
    public void PopCommand()
    {
        if (commandCards.Count == 0)
        {
            return;
        }

        int lastIndex = commandCards.Count - 1;

        commandCards.RemoveAt(lastIndex);
        commandDirections.RemoveAt(lastIndex);

        RefreshView();
    }

    /// <summary>
    /// すべてのコマンドを消去する
    /// </summary>
    public void ClearCommands()
    {
        commandCards.Clear();
        commandDirections.Clear();

        RefreshView();
    }

    /// <summary>
    /// 現在積まれているカードコマンド一覧を取得する
    /// </summary>
    /// <returns>カードコマンド一覧</returns>
    public IReadOnlyList<UICardData> GetCommandCards()
    {
        return commandCards;
    }

    /// <summary>
    /// 現在積まれている方向コマンド一覧を取得する
    /// </summary>
    /// <returns>方向コマンド一覧</returns>
    public IReadOnlyList<SlotDirection> GetCommandDirections()
    {
        return commandDirections;
    }

    /// <summary>
    /// 現在積まれているコマンド数を取得する
    /// </summary>
    /// <returns>コマンド数</returns>
    public int GetCommandCount()
    {
        return commandCards.Count;
    }

    /// <summary>
    /// コマンドが最大数まで積まれているか判定する
    /// </summary>
    /// <returns>最大数に達していればtrue</returns>
    public bool IsFull()
    {
        return commandCards.Count >= maxCommandCount;
    }

    private void RefreshView()
    {
        if (commandStackView == null)
        {
            return;
        }

        commandStackView.Apply(commandCards);
    }
}
