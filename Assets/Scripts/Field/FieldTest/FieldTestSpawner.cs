using UnityEngine;

public class FieldTestSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _mockPrefab;

    [SerializeField]
    private float _mockSpawnInterval = 1.0f;
    private float _mockSpawnIntervalCnt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _mockSpawnIntervalCnt = _mockSpawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        _mockSpawnIntervalCnt -= Time.deltaTime;
        if (_mockSpawnIntervalCnt <= 0.0f)
        {
            _mockSpawnIntervalCnt = _mockSpawnInterval;
            Instantiate(_mockPrefab,transform.position,Quaternion.identity);
        }
    }

    public void Initialize(float Interval)
    {
        _mockSpawnIntervalCnt = Interval;
        _mockSpawnIntervalCnt = _mockSpawnInterval;
    }
}
