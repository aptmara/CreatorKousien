/**
 * 
 * 
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using Game.Gameplay.Cameras;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore;

namespace Game.Gameplay.Enemy.Boss
{


    [DisallowMultipleComponent]
    public sealed class BossBalanceRevealCloseUpPresenter : MonoBehaviour
    {

        [Header("===== 参照 ======")]
        [SerializeField, Tooltip("シェルゲームの進行役、未設定なら自分、子、親から探す")]
        private BalanceShellGameDirector _director;

        [SerializeField, Tooltip("動かすカメラ。未設定ならMainCamera")]
        private Camera _targetCamera;

        [SerializeField, Tooltip("カメラリグ。未設定なら自動で探す")]
        private CameraRigController _cameraRigController;

        [Header("====== 寄り ======")]
        [SerializeField, Min(0.5f)]
        [Tooltip("透視投影：中身からカメラまでの距離")]
        private float _closeUpDistance = 6.0f;

        [SerializeField, Min(0.5f)]
        [Tooltip("平行投影：寄った時のカメラサイズ")]
        private float _closeUpOrthoSize = 2.5f;

        [SerializeField]
        [Tooltip("透視投影：寄った時の画角の増減(度数)。")]
        private float _closeUpFovOffset = 0.0f;

        [SerializeField,Tooltip("中身の中心から中止店をずらす量")]
        private Vector3 _focusOffset = new Vector3(0.0f,3.0f,0.0f);

        [SerializeField, Range(0.0f, 15.0f)]
        [Tooltip("寄った時にカメラを傾ける角度(度数)。")]
        private float _rollDegrees = 4.0f;

        [Header("===== 時間 =====")]
        [SerializeField, Min(0.01f)]
        [Tooltip("中身へ急接近する時間(実時間・秒)")]
        private float _zoomInDuration = 0.14f;

        [SerializeField, Min(0.0f)]
        [Tooltip("寄ったまま見せる時間(実時間・秒)")]
        private float _holdDuration = 0.7f;

        [SerializeField, Min(0.01f)]
        [Tooltip("元のカメラへ戻る時間(実時間・秒)")]
        private float _zoomOutDuration = 0.35f;

        [SerializeField, Tooltip("急接近の進み方。既定は最初が早いイーズアウト")]
        private AnimationCurve _zoomInCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f, 0.0f, 3.0f),
            new Keyframe(1.0f, 1.0f, 0.0f, 0.0f)
            );

        [SerializeField]
        private AnimationCurve _zoomOutCurve = AnimationCurve.EaseInOut(0.0f,0.0f,1.0f,1.0f);

        [Header("===== スローモーション ======")]
        [SerializeField]
        private bool _useSlowMotion = true;

        [SerializeField, Range(0.02f, 1.0f)]
        [Tooltip("寄りから見せ終わるまでの時間の進み方")]
        private float _slowTimeScale = 0.15f;

        [Header("===== その他 =====")]
        [SerializeField, Min(1)]
        [Tooltip("順番待ちの最大数。超えた分は演出させずに捨てる")]
        private int _maxQueue = 4;

        [SerializeField, Tooltip("寄り始めに呼ぶ。引数は中身の表示名とアイコン")]
        private UnityEvent<string, Sprite> _onCloseUpStarted;

        [SerializeField, Tooltip("元のカメラへ戻り始める時に呼ぶ")]
        private UnityEvent _onCloseUpEnded;

        private struct RevealRequest
        {
            public TraySide slot;
            public BalanceTrayContentBase Content;
        }

        private readonly Queue<RevealRequest> _queue = new Queue<RevealRequest>();
        private Coroutine _routine;

        private Vector3 _savedPos;
        private Quaternion _savedRot;
        private float _savedFov;
        private float _savedOrtho;
        private bool _rigWasCinematic;
        private bool _hasSavedCamera;

        private bool _slowActive;
        private float _savedTimeScale = 1.0f;
        private float _savedFixedDelta = 0.02f;

        private Vector3 _lastFocus;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnEnable()
        {
            ResolveReferences();

            if(_director != null)
            {
                _director.OnContentRevealed += HandleContentRevealed;
            }
        }

        private void OnDisable()
        {
            if(_director != null )
            {
                _director.OnContentRevealed -= HandleContentRevealed;
            }

            AbortPresentation();
        }

        private void ResolveReferences()
        {
            if(_director == null)
            {
                _director = GetComponentInChildren<BalanceShellGameDirector>(true);
            }

            if(_director == null)
            {
                _director = GetComponentInParent<BalanceShellGameDirector>();
            }

            if(_director == null)
            {
                _director = FindFirstObjectByType<BalanceShellGameDirector>();
            }

            if(_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if(_cameraRigController == null && _targetCamera != null)
            {
                _cameraRigController = _targetCamera.GetComponent<CameraRigController>();
            }

            if (_cameraRigController == null)
            {
                _cameraRigController = FindFirstObjectByType<CameraRigController>();
            }
        }

        private void HandleContentRevealed(TraySide slot,BalanceTrayContentBase content)
        {
            if (content == null || _targetCamera == null) return;

            if (_queue.Count >= _maxQueue) return;

            _queue.Enqueue(new RevealRequest { slot = slot, Content = content });

            if (_routine == null)
            {
                _routine = StartCoroutine(PresentRoutine());
            }
        }

        private IEnumerator PresentRoutine()
        {
            SaveCamera();

            while(true)
            {
                while(_queue.Count > 0)
                {
                    RevealRequest request = _queue.Dequeue();

                    if (request.Content == null) continue;

                    yield return StartCoroutine(PlayCloseUp(request));
                }

                yield return StartCoroutine(ZoomOutRoutine());

                if (_queue.Count == 0) break;
            }

            RestoreCamera();

            _routine = null;
        }

        private IEnumerator PlayCloseUp(RevealRequest request)
        {
            BalanceTrayContentBase content = request.Content;

            float roll = request.slot == TraySide.Left ? _rollDegrees : -_rollDegrees;

            _onCloseUpStarted?.Invoke(content.DisplayName, content.Icon);

            if(_useSlowMotion)
            {
                BeginSlowMotion();
            }

            Transform cam = _targetCamera.transform;

            Vector3 fromPos = cam.position;
            Quaternion fromRot = cam.rotation;
            float fromFov = _targetCamera.fieldOfView;
            float fromOrtho = _targetCamera.orthographicSize;

            float elapsed = 0.0f;

            while(elapsed < _zoomInDuration)
            {
                elapsed += StepDelta();

                float k = _zoomInCurve.Evaluate(Mathf.Clamp01(elapsed / _zoomInDuration));

                ComputeCloseUpPose(content, roll, out Vector3 pos, out Quaternion rot, out float fov, out float ortho);

                cam.position = pos;
                cam.rotation = rot;
                _targetCamera.fieldOfView = fov;
                _targetCamera.orthographicSize = ortho;

                yield return null;
            }

            yield return StartCoroutine(EndSlowMotion());

            _onCloseUpEnded?.Invoke();

        }

        private void ComputeCloseUpPose(
            BalanceTrayContentBase content,
            float roll,out Vector3 position,
            out Quaternion rotation,
            out float fov,out float orthoSize
            )
        {
            if(content != null)
            {
                _lastFocus = content.transform.position;
            }

            Vector3 focus = _lastFocus + _savedRot * _focusOffset;
            Vector3 forward = _savedRot * Vector3.forward;


            position = focus - forward * _closeUpDistance;
            rotation = _savedRot * Quaternion.Euler(0.0f, 0.0f, roll);
            fov = Mathf.Max(5.0f, _savedFov + _closeUpFovOffset);
            orthoSize = _closeUpOrthoSize;
        }

        private IEnumerator ZoomOutRoutine()
        {
            Transform cam = _targetCamera.transform;

            Vector3 fromPos = cam.position;
            Quaternion fromRot = cam.rotation;
            float fromFov = _targetCamera.fieldOfView;
            float fromOrtho = _targetCamera.orthographicSize;

            float elapsed = 0.0f;

            while(elapsed < _zoomOutDuration)
            {
                elapsed += StepDelta();

                float k = _zoomOutCurve.Evaluate(Mathf.Clamp01(elapsed / _zoomOutDuration));

                cam.position = Vector3.LerpUnclamped(fromPos, _savedPos, k);
                cam.rotation = Quaternion.SlerpUnclamped(fromRot, _savedRot, k);
                _targetCamera.fieldOfView = Mathf.LerpUnclamped(fromFov, _savedFov, k);
                _targetCamera.orthographicSize = Mathf.LerpUnclamped(fromOrtho, _savedOrtho,k);

                yield return null;
            }
        }

        private static float StepDelta()
        {
            return Time.timeScale > 0.0f ? Time.unscaledDeltaTime : 0.0f;
        }

        private void SaveCamera()
        {
            Transform cam = _targetCamera.transform;

            _savedPos = cam.position;
            _savedRot = cam.rotation;
            _savedFov = _targetCamera.fieldOfView;
            _savedOrtho = _targetCamera.orthographicSize;

            _rigWasCinematic = _cameraRigController != null && _cameraRigController.IsCinematicModeActive;
            _cameraRigController?.SetCinematicModeActive(true);

            _hasSavedCamera = true;
        }

        private void RestoreCamera()
        {
            if (!_hasSavedCamera) return;

            if (_targetCamera != null)
            {
                Transform cam = _targetCamera.transform;

                cam.position = _savedPos;
                cam.rotation = _savedRot;
                _targetCamera.fieldOfView = _savedFov;
                _targetCamera.orthographicSize = _savedOrtho;
            }

            _cameraRigController?.SetCinematicModeActive(_rigWasCinematic);

            _hasSavedCamera = false;
        }

        private void BeginSlowMotion()
        {
            if (_slowActive || Time.timeScale <= 0.0f) return;

            _savedTimeScale = Time.timeScale;
            _savedFixedDelta = Time.fixedDeltaTime;

            Time.timeScale = _slowTimeScale;

            Time.fixedDeltaTime = _savedFixedDelta * _slowTimeScale;

            _slowActive = true;
        }

        private IEnumerator EndSlowMotion()
        {
            if(!_slowActive) yield break;

            while (Time.timeScale <= 0.0f)
            {
                yield return null;
            }

            Time.timeScale = _savedTimeScale;
            Time.fixedDeltaTime = _savedFixedDelta;

            _slowActive = false;
        }


        private void AbortPresentation()
        {
            StopAllCoroutines();

            _queue.Clear();
            _routine = null;

            if(_slowActive && Time.timeScale > 0.0f)
            {
                Time.timeScale = _savedTimeScale;
                Time.fixedDeltaTime = _savedFixedDelta;
            }

            _slowActive = false;

            RestoreCamera();
        }
    }

}
