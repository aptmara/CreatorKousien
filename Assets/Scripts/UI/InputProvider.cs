using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class InputProvider : MonoBehaviour
{
    public Vector2 GetCursorPosition()
    {
        return Input.mousePosition;
    }

    public bool IsSelectPressd()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return);
    }
    public bool IsCancelPressed()
    {
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
    }

    public bool IsKeyTrigger(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }

    public bool IsKeyReleased(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }

    public float GetSrollDelta()
    {
        return Input.mouseScrollDelta.y;
    }
}
