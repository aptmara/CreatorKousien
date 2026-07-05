using UnityEngine;
using System.Collections;


public class EnemyBodyPose : MonoBehaviour
{
    Coroutine _dropPoseCoroutine;
    float _elapsedTime;
    [SerializeField] float _dropDuration;
    Quaternion _startRot;
    [SerializeField] Vector3 _dropRot;

    public void DropPose(Transform _transform)
    {
        _elapsedTime = 0.0f;
        _startRot = _transform.rotation;
        StartCoroutine(DropRoutine(_transform));
    }

    private IEnumerator DropRoutine(Transform _transform)
    {
        while (_elapsedTime <= 1.0f)
        {
            _elapsedTime += Time.deltaTime / _dropDuration;

            _transform.rotation = Quaternion.Euler(Vector3.Lerp(_startRot.eulerAngles, _dropRot, _elapsedTime));
            yield return null;
        }
    }


}
