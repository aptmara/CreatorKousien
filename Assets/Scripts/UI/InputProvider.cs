//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   InputProvider.cs
// @brief  入力を取得。UI個別ではなく、共通操作の取得
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

public class InputProvider : MonoBehaviour
{
    /// <summary>
    /// 現在のカーソル位置を取得する
    /// </summary>
    /// <returns>マウスのスクリーン座標</returns>
    public Vector2 GetCursorPosition()
    {
        return Input.mousePosition;
    }

    /// <summary>
    /// 決定操作が押されたか判定する
    /// </summary>
    /// <returns>左クリックまたはEnterキーが押されたフレームでtrue</returns>
    public bool IsSelectPressd()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return);
    }
    /// <summary>
    /// キャンセル操作が押されたか判定する
    /// </summary>
    /// <returns>右クリックまたはEscapeキーが押されたフレームでtrue</returns>
    public bool IsCancelPressed()
    {
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
    }

    /// <summary>
    /// 指定キーが押されたか判定する
    /// </summary>
    /// <param name="key">判定対象のキー</param>
    /// <returns>指定キーが押されたフレームでtrue</returns>
    public bool IsKeyTrigger(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }

    /// <summary>
    /// 指定キーが離されたか判定する
    /// </summary>
    /// <param name="key">判定対象のキー</param>
    /// <returns>指定キーが押されたフレームでtrue</returns>
    public bool IsKeyReleased(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }

    /// <summary>
    /// マウスホイールのスクロール量を取得する
    /// </summary>
    /// <returns>Y方向のスクロール量</returns>
    public float GetSrollDelta()
    {
        return Input.mouseScrollDelta.y;
    }
}
