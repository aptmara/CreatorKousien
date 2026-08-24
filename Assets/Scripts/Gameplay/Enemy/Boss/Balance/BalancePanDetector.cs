using UnityEngine;

public class BalancePanDetector : MonoBehaviour
{
    [SerializeField] private RealisticBalanceScale _scaleController;
    [SerializeField] private bool _isLeftPan = true;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Rigidbody>(out var rb))
        {
            _scaleController.RegisterWeight(_isLeftPan,rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Rigidbody>(out var rb))
        {
            _scaleController.UnregisterWeight(_isLeftPan, rb);
        }
    }
}
