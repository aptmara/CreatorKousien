// ================================================================================
// File         : TitlePlayerRotationLimiter.cs
//
// Description  : タイトルシーンの接触禁止範囲に手が重ならないよう自動回転方向を制限する。
// ================================================================================

using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Presentation.UI.Title
{
    /// <summary>
    /// タイトルシーンの接触禁止範囲との位置関係に応じて自動回転方向を制限するクラス
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerAttachmentController))]
    public sealed class TitlePlayerRotationLimiter : MonoBehaviour
    {
        private const float RotationProbeStep = 5f;
        private const float BoundaryBackoffAngle = 1.5f;
        private const int BoundarySearchIterations = 8;

        [Header("回転制限対象")]
        [Tooltip("手との重なりを避ける範囲")]
        [SerializeField] private BoxCollider _forbiddenArea;

        private PlayerMotor _motor;
        private PlayerAttachmentController _attachmentController;
        private PhysicalAttachment _cachedAttachment;
        private Collider[] _attachmentColliders;
        private int _recoveryDirectionSign;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _attachmentController = GetComponent<PlayerAttachmentController>();
        }


        private void OnEnable()
        {
            _motor.SetAutoRotationDirectionConstraint(GetSafeDirection);
        }


        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.ClearAutoRotationDirectionConstraint(GetSafeDirection);
            }
        }


        /// <summary>
        /// 接触禁止範囲に手が重ならない最も近い回転方向を取得する
        /// </summary>
        private Vector3 GetSafeDirection(Vector3 desiredDirection, Vector3 up)
        {
            Vector3 planarDesiredDirection = Vector3.ProjectOnPlane(desiredDirection, up).normalized;
            if (!TryRefreshAttachmentColliders())
            {
                _recoveryDirectionSign = 0;
                return planarDesiredDirection;
            }

            Vector3 currentDirection = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (!IsDirectionSafe(currentDirection, currentDirection, up))
            {
                return FindNearestSafeDirection(currentDirection, planarDesiredDirection, up);
            }

            float desiredAngle = Vector3.SignedAngle(currentDirection, planarDesiredDirection, up);
            int probeCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(desiredAngle) / RotationProbeStep));
            float lastSafeAngle = 0f;

            for (int i = 1; i <= probeCount; i++)
            {
                float candidateAngle = desiredAngle * i / probeCount;
                Vector3 candidateDirection = Quaternion.AngleAxis(candidateAngle, up) * currentDirection;
                if (IsDirectionSafe(candidateDirection, currentDirection, up))
                {
                    lastSafeAngle = candidateAngle;
                    continue;
                }

                return FindBoundaryDirection(currentDirection, up, lastSafeAngle, candidateAngle);
            }

            _recoveryDirectionSign = 0;
            return planarDesiredDirection;
        }


        /// <summary>
        /// 安全な角度と危険な角度の間から接触直前の方向を取得する
        /// </summary>
        private Vector3 FindBoundaryDirection(Vector3 currentDirection, Vector3 up, float safeAngle, float unsafeAngle)
        {
            for (int i = 0; i < BoundarySearchIterations; i++)
            {
                float candidateAngle = (safeAngle + unsafeAngle) * 0.5f;
                Vector3 candidateDirection = Quaternion.AngleAxis(candidateAngle, up) * currentDirection;
                if (IsDirectionSafe(candidateDirection, currentDirection, up))
                {
                    safeAngle = candidateAngle;
                }
                else
                {
                    unsafeAngle = candidateAngle;
                }
            }

            _recoveryDirectionSign = unsafeAngle > 0f ? -1 : 1;
            float backedOffAngle = Mathf.MoveTowards(safeAngle, 0f, BoundaryBackoffAngle);
            return Quaternion.AngleAxis(backedOffAngle, up) * currentDirection;
        }


        /// <summary>
        /// 現在すでに重なっている場合に最寄りの安全方向を取得する
        /// </summary>
        private Vector3 FindNearestSafeDirection(Vector3 currentDirection, Vector3 desiredDirection, Vector3 up)
        {
            if (_recoveryDirectionSign != 0)
            {
                for (float angle = RotationProbeStep; angle <= 180f; angle += RotationProbeStep)
                {
                    Vector3 preferredDirection = Quaternion.AngleAxis(angle * _recoveryDirectionSign, up) * currentDirection;
                    if (IsDirectionSafe(preferredDirection, currentDirection, up))
                    {
                        return preferredDirection;
                    }
                }

                _recoveryDirectionSign = 0;
            }

            for (float angle = RotationProbeStep; angle <= 180f; angle += RotationProbeStep)
            {
                Vector3 leftDirection = Quaternion.AngleAxis(-angle, up) * currentDirection;
                Vector3 rightDirection = Quaternion.AngleAxis(angle, up) * currentDirection;
                bool isLeftSafe = IsDirectionSafe(leftDirection, currentDirection, up);
                bool isRightSafe = IsDirectionSafe(rightDirection, currentDirection, up);

                if (isLeftSafe && isRightSafe)
                {
                    bool useLeftDirection = Vector3.Angle(leftDirection, desiredDirection) <= Vector3.Angle(rightDirection, desiredDirection);
                    _recoveryDirectionSign = useLeftDirection ? -1 : 1;
                    return useLeftDirection ? leftDirection : rightDirection;
                }

                if (isLeftSafe)
                {
                    _recoveryDirectionSign = -1;
                    return leftDirection;
                }

                if (isRightSafe)
                {
                    _recoveryDirectionSign = 1;
                    return rightDirection;
                }
            }

            return currentDirection;
        }


        /// <summary>
        /// 指定方向へ回転した手のColliderが接触禁止範囲に重ならないか判定する
        /// </summary>
        private bool IsDirectionSafe(Vector3 direction, Vector3 currentDirection, Vector3 up)
        {
            if (_forbiddenArea == null)
            {
                return true;
            }

            float rotationAngle = Vector3.SignedAngle(currentDirection, direction, up);
            Quaternion rotationDelta = Quaternion.AngleAxis(rotationAngle, up);
            Vector3 pivot = transform.position;

            for (int i = 0; i < _attachmentColliders.Length; i++)
            {
                Collider attachmentCollider = _attachmentColliders[i];
                if (attachmentCollider == null || !attachmentCollider.enabled || attachmentCollider.isTrigger)
                {
                    continue;
                }

                Vector3 candidatePosition = pivot + rotationDelta * (attachmentCollider.transform.position - pivot);
                Quaternion candidateRotation = rotationDelta * attachmentCollider.transform.rotation;

                if (Physics.ComputePenetration(
                    attachmentCollider,
                    candidatePosition,
                    candidateRotation,
                    _forbiddenArea,
                    _forbiddenArea.transform.position,
                    _forbiddenArea.transform.rotation,
                    out _,
                    out _))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 実行時に生成されたアタッチメントのColliderを更新する
        /// </summary>
        private bool TryRefreshAttachmentColliders()
        {
            PhysicalAttachment currentAttachment = _attachmentController.CurrentAttachment;
            if (currentAttachment == null)
            {
                _cachedAttachment = null;
                _attachmentColliders = null;
                return false;
            }

            if (_cachedAttachment != currentAttachment)
            {
                _cachedAttachment = currentAttachment;
                _attachmentColliders = currentAttachment.GetComponentsInChildren<Collider>(false);
            }

            return _attachmentColliders != null && _attachmentColliders.Length > 0;
        }
    }
}
