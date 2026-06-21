using UnityEngine;

public class CollectableGravity : MonoBehaviour
{
    private GameObject _field;

    private Vector3 _gravityDir;

    private Rigidbody _rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Awake()
    {
        _field = GameObject.FindWithTag("Floor");
        _rb = GetComponent<Rigidbody>();
        float fieldAngle = -(_field.transform.rotation.x);

        float gravZ = Mathf.Sin(fieldAngle);
        float gravY = Mathf.Cos(fieldAngle);

        _gravityDir = new Vector3(0.0f,-gravY, gravZ);
        _gravityDir.Normalize();
        _gravityDir *= (9.8f * _rb.mass);
        _rb.useGravity = false;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        _rb.AddForce(_gravityDir);
    }

    
}
