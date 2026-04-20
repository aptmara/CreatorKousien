using UnityEngine;

public static class UIUtility
{
    public static string FormatTine(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

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

}
