using UnityEngine;
using CreatorKousien.Command;
using CreatorKousien.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace CreatorKousien.Effect
{
    public class EffectUseCase
    {
        private EffectSystem _effectSystem;
        private CardSystem _cardSystem;
        private PoolSystem _poolSystem;
        private CommandDispatcher _dispatcher;
        private EffectCommandFactory _commandFactory;



        public EffectUseCase(EffectSystem effectSystem, PoolSystem poolSystem, CardSystem cardSystem, CommandDispatcher dispatcher)
        {
            _poolSystem = poolSystem;
            _effectSystem = effectSystem;
            _cardSystem = cardSystem;
            _dispatcher = dispatcher;
        }

        public void UseCard(UseCardCommand useCardCommand)
        {
            // 使用するカードの方向を受け取る
            SlotDirection registerSlotDirection = useCardCommand.RegisterSlotDirection;
            // カードを使用し効果IDを受け取る
            int effectID = _cardSystem.UseSlotCard(registerSlotDirection);
            // 対応する効果を登録
            // TODO 対象を列挙する構造体を渡し、効果対象IDも受け取るようにする
            EffectData useEffect;
            _effectSystem.GetEffect(effectID, out useEffect);

            // 効果を適用、コマンド変換もEffectSystemが行っても良いかも？
            List<ICommand> commands;
            commands = _commandFactory.EffectToCommand(1, useEffect);
            
        }

        public void PickCard(PickCommand pickCommand)
        {
            // Poolを指定
            int poolID = pickCommand.PoolID;
            _poolSystem.SetPool(poolID);

            // 指定枚数だけ抽選
            int pickCount = pickCommand.PickCount;
            List<int> pickedCard = _poolSystem.PickDistinctCards(pickCount);

            // 現在の手札を取得
            // UIManagerにコマンドを送る
            //_dispatcher.Dispatch();
        }

        public void SetSlotCard(SetSlotCardCommand setSlotCardCommand)
        {
            _cardSystem.SetCard(setSlotCardCommand.SlotIDs);
        }
    }

}
