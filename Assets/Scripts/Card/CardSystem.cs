using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.LightAnchor;

class CardRuntimeData
{
    //! 実行データのID
    int _instanceID;
    public int InstanceID => _instanceID;
    //! 登録されているカードの効果
    int _cardID;
    public int CardID => _cardID;
    //! 効果変更などを加味したカードの効果
    int _currentCardID;
    public int CurrentCardID => _currentCardID;
    //! カードの表裏
    CardFace _currentFace;
    public CardFace CurrentFace => _currentFace;
    //! 実行データとしてのカードの方向
    SlotDirection _instanceCardDirection;
    public SlotDirection InstanceCardDirection => _instanceCardDirection;

    public CardRuntimeData(int instanceID, int cardID, CardFace face, SlotDirection instanceCardDirection)
    {
        this._instanceID = instanceID;
        this._cardID = cardID;
        this._currentCardID = cardID;
        this._currentFace = face;
        this._instanceCardDirection = instanceCardDirection;
    }

    public void FlipFace()
    {
        // 向きを裏返す
        _currentFace = _currentFace == CardFace.FaceUp ? CardFace.FaceDown : CardFace.FaceUp;
    }
}

public class CardSystem
{
    //! データベース
    private CardDataBase _dataBase;
    //! 手札
    private Dictionary<SlotDirection ,CardRuntimeData> _haveCard;
    // コンストラクタ
    public CardSystem(CardDataBase dataBase, Dictionary<SlotDirection, int> haveCardIDs)
    {
        // 初期化
        _dataBase = dataBase;
        _haveCard = new Dictionary<SlotDirection, CardRuntimeData>();

        ResetCardInstance();
        // 渡されたIDからカードのインスタンスを作成
        SetCardInstance(haveCardIDs);
    }

    // カード作成API
    public void SetCard(Dictionary<SlotDirection ,int> setCardID)
    {
        // 渡されたIDからカードのインスタンスを作成
        SetCardInstance(setCardID);
    }

    void SetCardInstance(Dictionary<SlotDirection, int> setCardID)
    {
        // 各方向分カードを作成
        for (int i = 0; i < (int)SlotDirection.MaxDirection; i++)
        {
            // 渡された手札を確認し、存在する場合は渡されたカードを使用、そうでない場合は予備のデータを挿入
            CardData data = _dataBase.FallBackCard;
            // 確認用にデータを取得
            SlotDirection direction = (SlotDirection)i;
            int cardID;
            bool hasData = setCardID.TryGetValue(direction, out cardID);
            if (!hasData) continue;
            CardData checkData = _dataBase.GetCard(cardID);
            
            // チェックが通ったのでデータを確定
            data = checkData;
            
            // インスタンスを作成
            _haveCard[(SlotDirection)i] = CreateHaveCard(0, data, (SlotDirection)i);
        }
        // 不正データが入っていた場合抜ける
        HandErrorMessage();
    }


    void HandErrorMessage()
    {
        int fallBackID = _dataBase.FallBackCard.CardID;

        for (int i = 0; i < (int) SlotDirection.MaxDirection; i++)
        {
            SlotDirection direction = (SlotDirection)i;
            if (_haveCard[direction] == null)
            {
                Debug.LogError("[CardSystem]カードが存在しません");
            }
            else if (_haveCard[direction].CardID == fallBackID) 
            {
                Debug.LogError("[CardSystem]カードにFallBack値が入っています");
            }
        }
    }

    CardRuntimeData CreateHaveCard(int instanceID,CardData data, SlotDirection direction)
    {
        return new CardRuntimeData(instanceID, data.CardID, CardFace.FaceUp, direction);
    }

    public int UseSlotCard(SlotDirection useDirection)
    {
        int effectID = GetSlotEffect(useDirection); 
        _haveCard[useDirection].FlipFace();

        return effectID;
    }

    int GetSlotEffect(SlotDirection direction)
    {
        int cardID = _haveCard[direction].CardID;
        CardFace face = _haveCard[direction].CurrentFace;
        return _dataBase.GetCard(cardID).faceEffectID[(int)face];
    }

    void ResetCardInstance()
    {
        for (int i = 0; i < (int)SlotDirection.MaxDirection; i++)
        {
            SlotDirection direction = (SlotDirection)i;
            _haveCard[direction] = CreateHaveCard(-1, _dataBase.FallBackCard, direction);
        }
    }
}
