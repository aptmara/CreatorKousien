using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovieGate : MonoBehaviour
{

    bool _toglleGate = false;

    [Header("--- 門の各コンポーネント参照位置 ---")]
    [SerializeField] private Transform _leftDoorHinge;
    [SerializeField] private Transform _rightDoorHinge;
    [SerializeField] private Vector3 _openAngle;
    [SerializeField] private Vector3 _closeAngle;
    [SerializeField] private float _gateTime;

    Coroutine _lerpCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.f6Key.wasPressedThisFrame)
        {
            _toglleGate = !_toglleGate;
            if (_lerpCoroutine != null) StopCoroutine(_lerpCoroutine);
            if (_toglleGate) StartCoroutine(LerpAngle(_openAngle));
            else StartCoroutine(LerpAngle(_closeAngle));
        }
    }

    IEnumerator LerpAngle(Vector3 targetAngle)
    {
        float progress = 0.0f;
        Vector3 startAngle = _leftDoorHinge.rotation.eulerAngles;

        while(progress < 1.0f)
        {
            progress += Time.deltaTime / Mathf.Max(_gateTime, 0.001f);
            progress = Mathf.Clamp(progress, 0.0f, 1.0f);
            Vector3 angle;
            angle.x = Mathf.Lerp(startAngle.x, targetAngle.x, progress);
            angle.y = Mathf.Lerp(startAngle.y, targetAngle.y, progress);
            angle.z = Mathf.Lerp(startAngle.z, targetAngle.z, progress);

            _leftDoorHinge.transform.rotation = Quaternion.Euler(angle.x, angle.y, angle.z);
            _rightDoorHinge.transform.rotation = Quaternion.Euler(angle.x, -angle.y, angle.z);

            yield return null;
        }

        _lerpCoroutine = null;
    }
}
