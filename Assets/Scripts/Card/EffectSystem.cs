using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;

public class EffectSystem
{
    struct RegisterEffectData
    {
        public EffectData data;
        public int currentDuration;

        public RegisterEffectData(EffectData effectData)
        {
            this.data = effectData;
            currentDuration = effectData.Duration;
        }
    }
    //! エフェクト情報
    EffectDataBase _effectDataBase;
    //! 
    List<RegisterEffectData> _registerEffects;

    EffectSystem(EffectDataBase effectDataBase)
    {
        _registerEffects = new List<RegisterEffectData>();
        _effectDataBase = effectDataBase;
    }

    public void RegisterData(int effectID)
    {
        // 効果を取得
        EffectData registerData = _effectDataBase.GetEffect(effectID);
        // 登録
        _registerEffects.Add(new RegisterEffectData(registerData));
    }

    public void ApplyAllEffect()
    {
        foreach (var effect in _registerEffects)
        {
            // 遅延中だった場合は処理しない
            if (effect.currentDuration > 0) continue;

            // 処理を反映
            if(!ApplyEffect(effect.data))
            {
                Debug.Log("[EffectSystem]" + effect.data.EffectName + "の反映に失敗しました")
            }
        }

    }

    public void DurationUpdate(int turn)
    {
        for(int i = 0; i < _registerEffects.Count; i++)
        {
            RegisterEffectData effect = _registerEffects[i];
            effect.currentDuration -= turn;
            _registerEffects[i] = effect;
        }
    }

    bool ApplyEffect(EffectData data)
    {
        return true;
    }
}
