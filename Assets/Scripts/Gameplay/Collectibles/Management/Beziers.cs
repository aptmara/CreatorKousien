using UnityEngine;

public class Beziers : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Vector3 start, end;
        start = new Vector3(
            gameObject.transform.position.x - 1.0f,
            gameObject.transform.position.y,
            gameObject.transform.position.z
            );
        end = new Vector3(
            gameObject.transform.position.x + 1.0f,
            gameObject.transform.position.y,
            gameObject.transform.position.z
            );
        Gizmos.color = Color.red;
        Gizmos.DrawLine(start,end);

        start = new Vector3(
            gameObject.transform.position.x,
            gameObject.transform.position.y - 1.0f,
            gameObject.transform.position.z
            );
        end = new Vector3(
            gameObject.transform.position.x,
            gameObject.transform.position.y + 1.0f,
            gameObject.transform.position.z
            );
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);

        start = new Vector3(
            gameObject.transform.position.x,
            gameObject.transform.position.y,
            gameObject.transform.position.z - 1.0f
            );
        end = new Vector3(
            gameObject.transform.position.x,
            gameObject.transform.position.y,
            gameObject.transform.position.z + 1.0f
            );
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(start, end);
    }
}
