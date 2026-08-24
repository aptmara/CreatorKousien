using UnityEngine;

[RequireComponent (typeof(SkinnedMeshRenderer))]
public class BoddTraySkinnedBlendVisual : MonoBehaviour
{
    [SerializeField] private BalanceBarrierAttackObject _source;
    [SerializeField] private SkinnedMeshRenderer _renderer;
    [SerializeField] private int _blendShapeIndex;
    [SerializeField] private float _transitionSpeed = 200.0f;

    private float _targetWeight;

    private void Awake()
    {
        if(_renderer == null) _renderer = GetComponent<SkinnedMeshRenderer> ();
    }

    private void OnEnable()
    {
        if (_source != null) _source.OnActiveChanged += HandleActiveChanged;
    }

    private void OnDisable()
    {
        if (_source != null) _source.OnActiveChanged -= HandleActiveChanged;
    }

    private void HandleActiveChanged(bool isActive) => _targetWeight = isActive ? 100.0f : 0.0f;

    private void Update()
    {
        float current = _renderer.GetBlendShapeWeight(_blendShapeIndex);
        float next = Mathf.MoveTowards(current, _targetWeight, _transitionSpeed * Time.deltaTime);
        if(!Mathf.Approximately(current,next))
            _renderer.SetBlendShapeWeight(_blendShapeIndex,next);
    }
}
