using UnityEngine;
using System.Collections.Generic;

namespace CreatorKousien.Effect
{
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
        List<EffectData> _registerEffects;

        public EffectSystem(EffectDataBase effectDataBase)
        {
            _registerEffects = new List<EffectData>();
            _effectDataBase = effectDataBase;
        }

        public void GetEffect(int effectID, out EffectData effectData)
        {
            // 効果を取得
            effectData = _effectDataBase.GetEffect(effectID);

        }
    }
}

