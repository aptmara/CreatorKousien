using UnityEngine;
using CreatorKousien.Command;
using NUnit.Framework;
using System.Collections.Generic;

public class EffectUseCase
{
    EffectSystem _effectSystem;
    CardSystem _cardSystem;
    PoolSystem _poolSystem;



    public EffectUseCase(EffectSystem effectSystem, PoolSystem poolSystem, CardSystem cardSystem)
    {
        _poolSystem = poolSystem;
        _effectSystem = effectSystem;
        _cardSystem = cardSystem;
    }

    public void RegisterUseCard(RegisterSlotCardCommand registerSlotCardCommand)
    {
        // 使用するカードの方向を受け取る
        SlotDirection registerSlotDirection = registerSlotCardCommand.RegisterSlotDirection;
        // カードを使用し効果IDを受け取る
        int effectID = _cardSystem.UseSlotCard(registerSlotDirection);
        // 対応する効果を登録
        _effectSystem.RegisterData(effectID);
    }

    public void ApplyEffect(ApplyEffectCommand applyEffectCommand)
    {
        _effectSystem.ApplyAllEffect();
    }

    public void SetPool(SetPoolCommand setPoolCommand)
    {
        int poolID = setPoolCommand.PoolID;
        _poolSystem.SetPool(poolID);
    }

    public void PickCard(PickCommand pickCommand)
    {
        // 指定枚数だけ抽選
        int pickCount = pickCommand.PickCount;
        List<int> pickedCard = _poolSystem.PickDistinctCards(pickCount);

        // 現在の手札を取得

        // UIManagerにコマンドを送る

    }
}
