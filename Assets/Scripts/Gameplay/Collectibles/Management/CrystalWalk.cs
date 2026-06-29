// 描画している線上をcrystalが練り歩きます
// 2026/6/29
// 山本郁也
using Game.Gameplay.Collectibles;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class CrystalWalk : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private Vector3[] _rightBézier;
    [SerializeField]
    private Vector3[] _leftBézier;

    public Vector3 centerPos;
    public Vector3 rightPos;
    public Vector3 leftPos;
    public Vector3 rightControllPos;
    public Vector3 leftControllPos;

    private int segmentCount = 32;

    [Header("スタート位置(0秒or最大の半分の時間推奨)")]
    [SerializeField]
    public float startCount;
    [Header("一周にかかる時間(秒)")]
    [SerializeField]
    public float maxCount;
    private float _currentCount;
    private float _baseCount;
    [Header("ヒットストップ")]
    [SerializeField] private float _hitStop = 0.5f;
    private float _currentHitStop = 0.0f;

    [Header("欠片関連")]
    [SerializeField]
    private CrystalShardEmitter _emitter;
    [SerializeField]
    private Vector3 _emitVec;
    [SerializeField] private DrawVectorforGizmo _gizmo;
    [SerializeField] private ShardEmissionVector _vecGizmo;
    [SerializeField] public int shardCount;
    [SerializeField] public float power;
    [SerializeField] public CrystalShardEmitter.WeightedShardData[] _shard;
    [SerializeField] private GameObject VFX;
    [SerializeField] private Vector3 _scale;

    [Header("方向制御")]
    [SerializeField] private GameObject _target;
    [SerializeField] private GameObject _origin;

    void Start()
    {
        _currentCount = startCount;
        _baseCount = maxCount / 4.0f;
        if(_scale.x <= 0.0f || _scale.y <= 0.0f || _scale.z <= 0.0f)
        {
            _scale = new Vector3(1, 1, 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(_currentCount < maxCount / 4.0f)
        {
            gameObject.transform.position = 
            CalculateQuadraticBezierPoint(
                (_currentCount)/_baseCount,
                centerPos,
                leftControllPos,
                leftPos);
        }
        else if(_currentCount > maxCount / 4.0f && _currentCount <= maxCount / 2.0f)
        {
            gameObject.transform.position = 
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount)) / _baseCount,
                leftPos,
                leftControllPos,
                centerPos);
        }
        else if(_currentCount > maxCount / 2.0f && _currentCount <= 3.0f * maxCount/ 4.0f)
        {
            gameObject.transform.position = 
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount * 2.0f)) / _baseCount,
                centerPos,
                rightControllPos,
                rightPos);
        }
        else if(_currentCount > 3.0f * maxCount / 4.0f)
        {
            gameObject.transform.position =
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount * 3.0f)) / _baseCount,
                rightPos,
                rightControllPos,
                centerPos);
        }
        else
        {
            gameObject.transform.position = new Vector3(0, 0, 0);
        }
        if(_currentHitStop > 0.0f)
        {
            _currentHitStop -= Time.deltaTime;
        }
        else
        {
            _currentCount += Time.deltaTime;
            if (_currentCount >= maxCount) _currentCount = 0.0f;
        }

        if (_currentHitStop <= 0.0f) _currentHitStop = 0.0f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Emits();
        }

        Vector3 dir = _target.transform.position - _origin.transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }


    private void OnDrawGizmos()
    {
        if (centerPos == null ||
            rightPos == null ||
            leftPos == null ||
            leftControllPos == null ||
            rightControllPos == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        Vector3 previousPoint = leftPos;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;

            Vector3 currentPoint = CalculateQuadraticBezierPoint(
                t,
                leftPos,
                leftControllPos,
                centerPos
            );

            Gizmos.DrawLine(previousPoint, currentPoint);

            previousPoint = currentPoint;
        }

        previousPoint = centerPos;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;

            Vector3 currentPoint = CalculateQuadraticBezierPoint(
                t,
                centerPos,
                rightControllPos,
                rightPos
            );

            Gizmos.DrawLine(previousPoint, currentPoint);

            previousPoint = currentPoint;
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(
        float t,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2
    )
    {
        float u = 1.0f - t;

        return
            u * u * p0 +
            2.0f * u * t * p1 +
            t * t * p2;
    }

    [ContextMenu("DEBUG: Emit Shards")]
    public void Emits()
    {
        if (_emitter != null)
        {
            for (int i = 0; i < shardCount; i++)
                _emitter.Emitter(_emitVec, _vecGizmo.GetAngle(), power, _shard);
        }
        _currentHitStop = _hitStop;
        PlayEffect(gameObject.transform.position, _scale.x);
    }
    public void PlayEffect(Vector3 position, float size)
    {
        GameObject effect = Instantiate(VFX, position, Quaternion.identity);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            var main = particle.main;
            main.startSizeMultiplier *= size;
        }

        Destroy(effect, 2.0f);
    }
}
