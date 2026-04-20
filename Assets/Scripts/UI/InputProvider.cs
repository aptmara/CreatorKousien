// ------------------------------------------------------------
// File		: InputProvider.cs
// Summary	: 入力を提供するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - 新しいInputSystemに移行
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace CreatorKousien.View.UI
{
    public class InputProvider : MonoBehaviour
    {
        public Vector2 GetCursorPosition()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }

        public bool IsSelectPressed()
        {
            bool mouseClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool enterPress = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
            return mouseClick || enterPress;
        }

        public bool IsCancelPressed()
        {
            bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool escPress = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            return rightClick || escPress;
        }
    }
}
