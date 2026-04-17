using UnityEngine;
using System.Collections.Generic;

struct CardRuntimeData
{
    //! 実行データのID
    int instanceID;
    //! 登録されているカードの効果
    int cardID;
    //! 効果変更などを加味したカードの効果
    int currentCardID;
    //! カードの表裏
    CardFace currentFace;
    //! 実行データとしてのカードの方向
    SlotDirection instanceCardDirection;

    public CardRuntimeData(int instanceID, int cardID, CardFace face, SlotDirection instanceCardDirection)
    {
        this.instanceID = instanceID;
        this.cardID = cardID;
        this.currentCardID = cardID;
        this.currentFace = face;
        this.instanceCardDirection = instanceCardDirection;
    }
}

public class CardSystem
{
    //! データベース
    private CardDataBase _dataBase;
    //! 手札
    private CardRuntimeData[] _haveCard;
    // コンストラクタ
    public CardSystem(CardDataBase dataBase, List<int> haveCardID)
    {
        // 初期化
        _dataBase = dataBase;
        _haveCard = new CardRuntimeData[(int)SlotDirection.MaxDirection];
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
            _haveCard[i] = CreateHaveCard(0, data, (SlotDirection)i);
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
    
}
