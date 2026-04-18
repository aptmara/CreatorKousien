using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using CreatorKousien.Effect;
public class CardTestSystem : MonoBehaviour
{
    [SerializeField]
    CardDataBase _cardDataBase;
    [SerializeField]
    int _poolID;
    [SerializeField]
    int upCardID;
    [SerializeField]
    int downCardID;
    [SerializeField]
    int leftCardID;
    [SerializeField]
    int rightCardID;

    [SerializeField]
    PoolDataBase _poolDataBase;

    CardSystem _cardSystem;
    PoolSystem _poolSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cardDataBase.CheckAllData();
        Dictionary<SlotDirection, int> haveCardID = new Dictionary<SlotDirection, int>();
        haveCardID[SlotDirection.Up] = upCardID;
        haveCardID[SlotDirection.Down] = downCardID;
        haveCardID[SlotDirection.Left]  = leftCardID;
        haveCardID[SlotDirection.Right] = rightCardID;

        _cardSystem = new CardSystem(_cardDataBase, haveCardID);
        _poolSystem = new PoolSystem(_poolID, _cardDataBase.FallBackCard.CardID, _poolDataBase);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
