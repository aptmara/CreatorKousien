using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System;

public class PoolSystem
{
    CardPool _cardPool;
    int _fallBackID;
    PoolDataBase _poolDataBase;

    public PoolSystem(int poolID, int fallBackID, PoolDataBase PoolDataBase)
    {
        _fallBackID = fallBackID;
        _poolDataBase = PoolDataBase;
        SetPool(poolID);
    }

    public void SetPool(int poolID)
    {
        _cardPool = _poolDataBase.GetPool(poolID);
    }

    public List<int> PickDistinctCards(int pickCount)
    {
        List<int> cardList = new List<int>();
        int finalPickUpCount = Math.Min(pickCount, _cardPool.DataList.Count);
        for (int i = 0; i < finalPickUpCount; i++)
        {
            // 既に取得したカードを除外して抽選
            int pickedCard = PickRandomCard(cardList);
            // 抽選したカードを追加
            cardList.Add(pickedCard);
        }

        return cardList;
    }

    int PickRandomCard(List<int> exclusionIDList)
    {
        var exclusionSet = new HashSet<int>(exclusionIDList);

        float maxRate = 0.0f;
        List<PoolData> pickList = new List<PoolData>();
        // 除外しつつ抽選リストと抽選用の値を作る
        foreach (var card in _cardPool.DataList)
        {
            if (exclusionSet.Contains(card.CardID)) continue;

            pickList.Add(card);
            maxRate += card.Rate;
        }
        // 不正な値
        if(maxRate == 0.0f)
        {
            // エラーログを吐く
            Debug.LogError("カード出現率が不正だったためFallBackIDを返します");
            return _fallBackID;
        }

        float random = UnityEngine.Random.Range(0.0f, maxRate);
        int id = _fallBackID;
        foreach (var card in pickList)
        {
            random -= card.Rate;
            if(random <= 0.0f)
            {
                id = card.CardID;
                break;
            }
        }

        return id;
    }
}
