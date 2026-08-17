using UnityEngine;
using System.Collections.Generic;

public class BalanceBossSummonGimmick : MonoBehaviour ,IBossGimmick
{
    bool IsComplete { get; }

    [SerializeField] private List<GameObject> _props = new List<GameObject>();
    [SerializeField] private Transform[] spawnPoints;

    public void Execute()
    {
        int index_1 = 0, index_2 = 0;
        while (index_1 == index_2)
        {
            index_1 = Random.Range(0, _props.Count);
            index_2 = Random.Range(0, _props.Count);
        }
        GameObject spawnObj_1 = _props[index_1];
        GameObject spawnObj_2 = _props[index_2];
        Instantiate(spawnObj_1, spawnPoints[0].position, spawnPoints[0].rotation);
        Instantiate(spawnObj_2, spawnPoints[1].position,spawnPoints[1].rotation);

        IsComplete = true;
    }

    public void Cancel()
    {

    }
}
