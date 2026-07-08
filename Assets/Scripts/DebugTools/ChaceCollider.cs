using UnityEngine;

public class ChaceCollider : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private GameObject _chaseTarget;
    [SerializeField]
    private Collider _collider;
    // Colliderのセンターを弄るため、元のセンターをオフセットとして加算する
    private Vector3 _chaceOffset;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
