using Game.Core.Enemy;
using System.Collections;
using UnityEngine;

public class EnemyRising : MonoBehaviour
{
    private EnemyController _controller;
    private Vector3 _targetPosition;

    [Tooltip("上昇にかかる時間")]
    [SerializeField] private float _riseDuration = 1.5f;
    [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void StartRise(EnemyController controller,Vector3 targetPos,float startYOffset)
    {
        _controller = controller;
        _targetPosition = targetPos;

        transform.position = _targetPosition + Vector3.down * startYOffset;

        StartCoroutine(RiseRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0.0f;

        while (elapsed < _riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _riseDuration;
            float curveT = _riseCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, _targetPosition, curveT);
            yield return null;
        }

        transform.position = _targetPosition;

        Debug.Log("[Rising] 到着");
    }
}
