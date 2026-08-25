using UnityEngine;

public class BalanceCatapultController : MonoBehaviour
{
    [SerializeField] private RealisticBalanceScale _scale;
    [SerializeField] private float _launchImpulsePerMass = 3.0f;
    [SerializeField] private float _minLaunchImpulse = 8.0f;

    private void OnEnable() => _scale.OnWeightSuddenlyAdded += HandleSuddenWeight;
    private void OnDisable() => _scale.OnWeightSuddenlyAdded -= HandleSuddenWeight;

    private void HandleSuddenWeight(bool isLeftLoaded,float addedMass)
    {
        var launchTargets = _scale.GetWeightOnSide(!isLeftLoaded);
        float impulse = Mathf.Max(_minLaunchImpulse, addedMass * _launchImpulsePerMass);

        foreach (var rb in launchTargets)
        {
            if(rb == null) continue;
            rb.AddForce(Vector3.up * impulse, ForceMode.Impulse);
        }
    }
}
