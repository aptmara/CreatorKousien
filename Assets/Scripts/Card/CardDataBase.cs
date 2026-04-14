using System.Collections.Generic;
using UnityEngine;

public enum CardFace
{
    FaceUp,
    FaceDown,
    MaxFace
}

public enum SlotDirection
{
    Up,
    Down,
    Left,
    Right,
    MaxDirection
}

[System.Serializable]
public struct CardData
{
    [SerializeField] private int cardID;
    public int CardID => cardID;

    [SerializeField] private SlotDirection direction;
    public SlotDirection Direction => direction;

    // 固定長配列の構造体を作ることができないため両面分変数で持つ
    [SerializeField] private int faceUpEffectID;
    public int FaceUpEffectID => faceUpEffectID;

    [SerializeField] private int faceDownEffectID;
    public int FaceDownEffectID => faceDownEffectID;

}

[CreateAssetMenu(fileName = "SO_CardDataBase", menuName = "CreatorKousien/Data/CardDataBase")]
public class CardDataBase : ScriptableObject
{
    [SerializeField]
    CardData[] _cardList;
    [SerializeField]
    CardData _fallBackCard;
    public CardData FallBackCard => _fallBackCard;

    public CardData GetCard(int a_cardID)
    {
        // 見つからなかった時用のデータを挿入
        CardData ret = _fallBackCard;
        // IDで探索
        foreach (CardData data in _cardList)
        {
            // IDが違ったら次へ
            if (data.CardID != a_cardID) continue;
            // IDが等しかったら上書きして抜ける
            ret = data;
            break;
        }
        return ret;
    }

    public void CheckAllData()
    {
        Dictionary<int, bool> hasData = new Dictionary<int, bool>();
        int i = 0;
        foreach (CardData cardData in _cardList)
        {
            bool isSafe = true;
            // IDの数値チェック
            if (CheckID(cardData))
            {
                Debug.Log("[CardDataBase]要素数" + i + "番のIDの値が不正です！");
                isSafe = false;
            }
            // 方向チェック
            if (CheckDirection(cardData))
            {
                Debug.Log("[CardDataBase]要素数" + i + "番の方向の値が不正です！");
            }
            // 重複チェック
            if (CheckDuplicateID(cardData, hasData))
            {
                Debug.Log("[CardDataBase]要素数" + i + "のID" + cardData.CardID + "が重複しています！");
                isSafe = false;
            }

            if (isSafe)
            {
                hasData.Add(cardData.CardID, isSafe);
            }
            i++;
        }
    }

    bool CheckID(CardData cardData)
    {
        return cardData.CardID < 0;
    }

    bool CheckDirection(CardData cardData)
    {
        return cardData.Direction == SlotDirection.MaxDirection;
    }

    bool CheckDuplicateID(CardData cardData, Dictionary<int, bool> hasData)
    {
        bool exists;
        return hasData.TryGetValue(cardData.CardID, out exists) && exists;
    }
}
