using System.Collections;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossIntroSequenceController : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private CameraRigController _cameraRigController;
    [SerializeField] private Transform _bossRoot;

    public bool IsPlaying { get; private set; }

    private Vector3 _normalCameraPosition;
    private Quaternion _normalCameraRotation;
    private float _normalCameraFieldOfView;
    private float _normalCameraOrthographicSize;
    private bool _hasSavedNormalCameraPose;
    private bool _isCancellationRequested;

    private void Awake()
    {
        if (_targetCamera == null) _targetCamera = Camera.main;
        if (_cameraRigController == null && _targetCamera != null)
            _cameraRigController = _targetCamera.GetComponentInParent<CameraRigController>();
        if (_cameraRigController == null)
            _cameraRigController = Object.FindFirstObjectByType<CameraRigController>();
    }

    public IEnumerator PlayPresentation(BossIntroSequenceData data, Animator bossAnimator)
    {
        if (data == null)
        {
            Debug.LogWarning($"[{nameof(BossIntroSequenceController)}] dataがnullです。");
            yield break;
        }

        IsPlaying = true;
        _isCancellationRequested = false;

        ApplyStartPose(data);

        if (_cameraRigController != null) _cameraRigController.SetCinematicModeActive(true);
        SaveNormalCameraPose();

        if (data.ShowWarningUI) EventBus.Publish(new BossIntroWarningStartedEvent());

        if (!string.IsNullOrEmpty(data.AnimatorTriggerName) && bossAnimator != null)
        {
            bossAnimator.SetTrigger(data.AnimatorTriggerName);
        }

        yield return StartCoroutine(MoveCameraToBattlePosition(data));
        yield return StartCoroutine(MoveBossToBattlePosition(data));

        if (data.ShowWarningUI) EventBus.Publish(new BossIntroWarningEndedEvent());

        IsPlaying = false;
    }

    private void ApplyStartPose(BossIntroSequenceData data)
    {
        if (_bossRoot == null) return;

        _bossRoot.localPosition = data.PlayFromLeft ? data.LeftStartLocalPosition : data.RightStartLocalPosition;
        _bossRoot.localEulerAngles = data.PlayFromLeft ? data.LeftStartEulerAngles : data.RightStartEulerAngles;
    }

    private IEnumerator MoveBossToBattlePosition(BossIntroSequenceData data)
    {
        if (_bossRoot == null) yield break;

        Vector3 startPos = _bossRoot.localPosition;
        Quaternion startRot = _bossRoot.localRotation;
        Vector3 targetPos = data.BattleLocalPosition;
        Quaternion targetRot = Quaternion.Euler(data.BattleEulerAngles);

        float duration = Mathf.Max(0f, data.MoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = data.MoveCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            _bossRoot.localPosition = Vector3.LerpUnclamped(startPos, targetPos, t);
            _bossRoot.localRotation = Quaternion.SlerpUnclamped(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _bossRoot.localPosition = targetPos;
        _bossRoot.localRotation = targetRot;
    }

    private void SaveNormalCameraPose()
    {
        if (_hasSavedNormalCameraPose || _targetCamera == null) return;

        Transform camTf = _targetCamera.transform;
        _normalCameraPosition = camTf.position;
        _normalCameraRotation = camTf.rotation;
        _normalCameraFieldOfView = _targetCamera.fieldOfView;
        _normalCameraOrthographicSize = _targetCamera.orthographicSize;
        _hasSavedNormalCameraPose = true;
    }

    private IEnumerator MoveCameraToBattlePosition(BossIntroSequenceData data)
    {
        var config = data.BattleCameraConfig;
        if (config == null || _targetCamera == null) yield break;

        var settings = config.GetSettings(data.BattleCameraProjectionMode);
        Transform camTf = _targetCamera.transform;

        Vector3 startPos = camTf.position;
        Quaternion startRot = camTf.rotation;
        float fovStart = _targetCamera.fieldOfView;
        float orthoStart = _targetCamera.orthographicSize;

        Vector3 targetPos = settings.Position;
        Quaternion targetRot = Quaternion.Euler(settings.Rotation);
        float targetFov = settings.FieldOfView;
        float targetOrtho = settings.OrthographicSize;

        float duration = Mathf.Max(0f, data.CameraMoveDuration);
        float elapsed = 0f;

        while (!_isCancellationRequested && elapsed < duration)
        {
            float t = data.CameraBlendCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            camTf.position = Vector3.LerpUnclamped(startPos, targetPos, t);
            camTf.rotation = Quaternion.SlerpUnclamped(startRot, targetRot, t);
            _targetCamera.fieldOfView = Mathf.LerpUnclamped(fovStart, targetFov, t);
            _targetCamera.orthographicSize = Mathf.LerpUnclamped(orthoStart, targetOrtho, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_isCancellationRequested)
        {
            camTf.position = targetPos;
            camTf.rotation = targetRot;
            _targetCamera.fieldOfView = targetFov;
            _targetCamera.orthographicSize = targetOrtho;
        }
    }

    public void RestoreCameraImmediately()
    {
        if (_targetCamera != null && _hasSavedNormalCameraPose)
        {
            Transform camTf = _targetCamera.transform;
            camTf.position = _normalCameraPosition;
            camTf.rotation = _normalCameraRotation;
            _targetCamera.fieldOfView = _normalCameraFieldOfView;
            _targetCamera.orthographicSize = _normalCameraOrthographicSize;
        }

        if (_cameraRigController != null) _cameraRigController.SetCinematicModeActive(false);
        _hasSavedNormalCameraPose = false;
    }

    public void CancelPresentationAndRestoreCamera()
    {
        _isCancellationRequested = true;
        StopAllCoroutines();
        RestoreCameraImmediately();
        IsPlaying = false;
    }
}
