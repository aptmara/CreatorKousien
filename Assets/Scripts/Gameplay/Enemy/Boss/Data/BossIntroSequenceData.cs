using UnityEngine;
using Game.Gameplay.Cameras;

[CreateAssetMenu(fileName = "BossIntroSequenceData", menuName = "Boss/Intro/SequenceData")]
public class BossIntroSequenceData : ScriptableObject
{
    [Header("===== 登場方向 =====")]
    [SerializeField] private bool _playFromLeft = true;
    public bool PlayFromLeft => _playFromLeft;

    [Header("===== ボスの開始/戦闘位置(ローカル座標) =====")]
    [SerializeField] private Vector3 _leftStartLocalPosition;
    [SerializeField] private Vector3 _leftStartEulerAngles;
    [SerializeField] private Vector3 _rightStartLocalPosition;
    [SerializeField] private Vector3 _rightStartEulerAngles;
    [SerializeField] private Vector3 _battleLocalPosition;
    [SerializeField] private Vector3 _battleEulerAngles;

    public Vector3 LeftStartLocalPosition => _leftStartLocalPosition;
    public Vector3 LeftStartEulerAngles => _leftStartEulerAngles;
    public Vector3 RightStartLocalPosition => _rightStartLocalPosition;
    public Vector3 RightStartEulerAngles => _rightStartEulerAngles;
    public Vector3 BattleLocalPosition => _battleLocalPosition;
    public Vector3 BattleEulerAngles => _battleEulerAngles;

    [Header("===== 移動 =====")]
    [SerializeField] private float _moveDuration = 1.2f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float MoveDuration => _moveDuration;
    public AnimationCurve MoveCurve => _moveCurve;

    [Header("===== アニメーション =====")]
    [Tooltip("登場時に鳴らすAnimatorのTrigger名。未使用なら空でよい")]
    [SerializeField] private string _animatorTriggerName;
    public string AnimatorTriggerName => _animatorTriggerName;

    [Header("===== カメラ(ボスごとに変更可) =====")]
    [SerializeField] private StaticCameraConfig _battleCameraConfig;
    [SerializeField] private CameraRigController.ProjectionMode _battleCameraProjectionMode = CameraRigController.ProjectionMode.Perspective;
    [SerializeField] private float _cameraMoveDuration = 1.0f;
    [SerializeField] private AnimationCurve _cameraBlendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _cameraReturnDuration = 0.5f;

    public StaticCameraConfig BattleCameraConfig => _battleCameraConfig;
    public CameraRigController.ProjectionMode BattleCameraProjectionMode => _battleCameraProjectionMode;
    public float CameraMoveDuration => _cameraMoveDuration;
    public AnimationCurve CameraBlendCurve => _cameraBlendCurve;
    public float CameraReturnDuration => _cameraReturnDuration;

    [Header("===== 警告UI =====")]
    [SerializeField] private bool _showWarningUI = true;
    public bool ShowWarningUI => _showWarningUI;
}
