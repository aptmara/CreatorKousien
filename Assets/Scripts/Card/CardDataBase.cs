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
public class CardData
{
    //! ID
    [SerializeField] private int cardID;
    public int CardID => cardID;
    //! 方向
    [SerializeField] private SlotDirection direction;
    public SlotDirection Direction => direction;
    // 固定長配列の構造体を作ることができないため両面分変数で持つ
    //! 表面の効果ID
    [SerializeField] private int faceUpEffectID;
    //! 裏面の効果ID
    [SerializeField] private int faceDownEffectID;

    //! 外部に効果IDを渡す用の配列。外部に初期化関数を呼ばせる以外で安全に初期化する方法がなさそうなため、毎回作成する
    public int[] faceEffectID => new int[(int)CardFace.MaxFace] { faceUpEffectID, faceDownEffectID };

}

[CreateAssetMenu(fileName = "SO_CardDataBase", menuName = "CreatorKousien/Data/CardDataBase")]
public class CardDataBase : ScriptableObject
{
    //! カードリスト、要素数は関係なくIDかで探索しカードが引き出される。
    [SerializeField]
    CardData[] _cardList;
    //! 値が不正だった際に渡す予備のカード
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
                Debug.LogError("[CardDataBase]要素数" + i + "番のIDの値が不正です！");
                isSafe = false;
            }
            // 方向チェック
            if (CheckDirection(cardData))
            {
                Debug.LogError("[CardDataBase]要素数" + i + "番の方向の値が不正です！");
            }
            // 重複チェック
            if (CheckDuplicateID(cardData, hasData))
            {
                Debug.LogError("[CardDataBase]要素数" + i + "のID" + cardData.CardID + "が重複しています！");
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
