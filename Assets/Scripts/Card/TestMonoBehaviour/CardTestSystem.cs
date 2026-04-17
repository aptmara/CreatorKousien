using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CardTestSystem : MonoBehaviour
{
    [SerializeField]
    CardDataBase _cardData;
    [SerializeField]
    CardPool _cardPool;
    [SerializeField]
    List<int> _cardList;

    CardSystem _cardSystem;
    PoolSystem _poolSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cardData.CheckAllData();
        _cardSystem = new CardSystem(_cardData, _cardList);
        _poolSystem = new PoolSystem(_cardPool, _cardData.FallBackCard.CardID);

    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
