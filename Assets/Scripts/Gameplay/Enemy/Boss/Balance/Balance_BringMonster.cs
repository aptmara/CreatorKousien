using System;
using UnityEngine;

public class Balance_BringMonster : MonoBehaviour
{
    private Vector2 _target;
    private Vector2 _direction;
    private Action<Vector3, Quaternion> _onArrived;
    
    private bool _isSpawned = false;
    public bool IsSpawned => _isSpawned;
    [SerializeField]
    private float _speed = 8.0f;

    [SerializeField] private Animator _animator;

    public void Initialize(Vector3 initialPos,Vector3 targetPosition,Action<Vector3,Quaternion> onArrived)
    {
        _target = new Vector2(targetPosition.x,targetPosition.z);
        Vector2 InitialPos = new Vector2(initialPos.x,initialPos.z);

        _direction = (_target - InitialPos).normalized;
        
        _onArrived = onArrived;

        //_animator.Play("Climb");
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 move = new Vector3(
            _direction.x * Time.deltaTime * _speed,
            0.0f,
            0.0f
            );

        transform.Translate(move);

        Vector2 current = new Vector2(transform.position.x,transform.position.z);
        if(!_isSpawned && Vector2.Distance(_target,current) < 1.0f)
        {
            _isSpawned = true;
            _onArrived?.Invoke(transform.position, transform.rotation);
            Destroy(gameObject, 15.0f);
        }
    }
}
