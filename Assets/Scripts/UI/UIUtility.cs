//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   UIUtility.cs
// @brief  便利APIをおいてるだけ
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

public static class UIUtility
{
    /// <summary>
    /// 秒数を分:秒形式の文字列へ変換する
    /// </summary>
    /// <param name="timeInSeconds">変換対象の秒数</param>
    /// <returns>00:00形式の文字列</returns>
    public static string FormatTine(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 中心座標とサイズからRectを生成する
    /// </summary>
    /// <param name="center">矩形の中心座標</param>
    /// <param name="size">矩形サイズ</param>
    /// <returns>計算されたRect</returns>
    public static Rect CalcRect(Vector2 center,Vector2 size)
    {
        return new Rect(
            center.x - size.x * 0.5f,
            center.y - size.y * 0.5f,
            size.x,
            size.y
            );
    }

    public static Vector2 CalcAnchorPosition(Camera camera, RectTransform canvasRect,Vector3 worldPos)
    {
        Vector3 screenPos = camera.ScreenToViewportPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out Vector2 LocalPoint
            );

        return LocalPoint;
    }

    /// <summary>
    /// 基準位置にオフセットを加えたアンカー座標を返す
    /// </summary>
    /// <param name="basePos">基準位置</param>
    /// <param name="offset">加算するオフセット</param>
    /// <returns>計算後の座標</returns>
    public static Vector2 CalcAnchorPosition(Vector2 basePos, Vector2 offset)
    {
        return basePos + offset;
    }

    /// <summary>
    /// ワールド座標をCanvasローカル座標へ変換する
    /// </summary>
    /// <param name="worldCamera">ワールド座標をスクリーン座標へ変換するカメラ</param>
    /// <param name="uiCamera">Canvas用カメラ。Overlay Canvasの場合はnull</param>
    /// <param name="canvasRect">変換先CanvasのRectTransform</param>
    /// <param name="worldPos">変換元のワールド座標</param>
    /// <returns>Canvasローカル座標</returns>
    public static Vector2 WorldToCanvasPos(Camera worldCamera, Camera uiCamera, RectTransform canvasRect, Vector3 worldPos)
    {
        if (worldCamera == null || canvasRect == null)
        {
            return Vector2.zero;
        }

        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }
}
