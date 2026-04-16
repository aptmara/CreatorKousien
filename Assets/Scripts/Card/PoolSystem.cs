using Unity.VisualScripting;
using UnityEngine;

public class PoolSystem
{
    CardPool _cardPool;
    int _fallBackID;

    public PoolSystem(CardPool cardPool, int fallBackID)
    {
        _cardPool = cardPool;
        _fallBackID = fallBackID;
    }

    public void SetPool(CardPool cardPool)
    {
        _cardPool = cardPool;
    }

    public int PickRandumCard()
    {

        float maxRate = 0.0f;
        foreach (var card in _cardPool.Data)
        {
            maxRate += card.Rate;
        }
        // 不正な値
        if(maxRate == 0.0f)
        {
            // 
            Debug.LogError("カード出現率が不正だったためFallBackIDを返します");
            return _fallBackID;
        }

        float random = Random.Range(0.0f, maxRate);
        int id = 0;
        foreach (var card in _cardPool.Data)
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
