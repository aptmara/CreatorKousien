using UnityEngine;

public class BalancePanDetector : MonoBehaviour
{
    [SerializeField] private RealisticBalanceScale _scaleController;
    [SerializeField] private bool _isLeftPan = true;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if(rb != null)
        {
            _scaleController.RegisterWeight(_isLeftPan,rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            _scaleController.UnregisterWeight(_isLeftPan, rb);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            _scaleController.RegisterWeight(_isLeftPan, rb);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            _scaleController.UnregisterWeight(_isLeftPan, rb);
        }
    }
}
