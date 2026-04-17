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
        if(_currentFace == CardFace.FaceUp)
        {
            _currentFace = CardFace.FaceDown;
        }
        else if(_currentFace == CardFace.FaceDown)
        {
            _currentFace = CardFace.FaceUp;
        }
    }
}

public class CardSystem
{
    //! データベース
    private CardDataBase _dataBase;
    //! 手札
    private Dictionary<SlotDirection ,CardRuntimeData> _haveCard;
    // コンストラクタ
    public CardSystem(CardDataBase dataBase, List<int> haveCardID)
    {
        // 初期化
        _dataBase = dataBase;
        _haveCard = new Dictionary<SlotDirection, CardRuntimeData>();
        // 渡されたIDからカードのインスタンスを作成
        SetCardInstance(haveCardID);
    }

    // カード作成API
    public void SetCard(List<int> haveCardID)
    {
        // 渡されたIDからカードのインスタンスを作成
        SetCardInstance(haveCardID);
    }

    void SetCardInstance(List<int> haveCardID)
    {
        // 各方向分カードを作成
        for (int i = 0; i < (int)SlotDirection.MaxDirection; i++)
        {
            // 渡された手札を確認し、存在する場合は渡されたカードを使用、そうでない場合は予備のデータを挿入
            CardData data = _dataBase.FallBackCard;
            // 確認用にデータを取得
            CardData checkData = _dataBase.GetCard(haveCardID[i]);
            if (IsSafeHandData(i, checkData, haveCardID))
            {
                // チェックが通ったのでデータを確定
                data = checkData;
            }
            else
            {
                HandErrorMessage(i, checkData, haveCardID);
            }

            // インスタンスを作成
            _haveCard[(SlotDirection)i] = CreateHaveCard(0, data, (SlotDirection)i);
        }
    }

    bool IsSafeHandData(int index, CardData checkData, List<int> haveCardID)
    {
        return haveCardID.Count >= index && checkData.Direction == (SlotDirection)index;
    }

    void HandErrorMessage(int index, CardData checkData, List<int> haveCardID)
    {
        // エラーメッセージ
        if (checkData.Direction != (SlotDirection)index) Debug.LogError("[CardSystem]登録されたカードの方向が不正です。カードID" + checkData.CardID + "の方向は" + checkData.Direction.ToString() + "ですが" + ((SlotDirection)index).ToString() + "に設定されました");

        if (haveCardID.Count <= index) Debug.LogError("[CardSystem]渡された手札の枚数が不足しています、" + haveCardID.Count + "枚のカード情報が渡されました");
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
}
