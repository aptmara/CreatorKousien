using Unity.VisualScripting;
using UnityEngine;

public class PoolSystem
{
    CardPool _cardPool;
    public PoolSystem(CardPool cardPool)
    {
        _cardPool = cardPool;
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
