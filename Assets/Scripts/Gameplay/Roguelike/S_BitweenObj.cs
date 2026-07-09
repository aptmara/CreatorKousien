using UnityEngine;

public class S_BitweenObj : MonoBehaviour
{
    private MeshRenderer _mr;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _mr = GetComponent<MeshRenderer>();
        _mr.sortingLayerID = SortingLayer.NameToID("BitweenUIs");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
