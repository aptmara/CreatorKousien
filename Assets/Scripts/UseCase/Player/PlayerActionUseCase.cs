// ================================================================================
// File         : PlayerActionUseCase.cs
// Author       : Iwai Shogo
//
// Description  : どのカードを使ったかを聞きだし、その結果をTurnManagerに提出する。
// Created      : 2026-04-20
// ================================================================================

using CreatorKousien.Command;
using CreatorKousien.Data;
using CreatorKousien.Battle;
using CreatorKousien.Player;
using CreatorKousien.Field;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build.Pipeline.Tasks;

namespace CreatorKousien.UseCase
{
    public class PlayerActionUseCase
    {
        private CardSystem _cardSystem;
        private TurnManager _turnManager;
        private PlayerSystem _playerSystem;
        private FieldService _fieldService;

        public PlayerActionUseCase(CardSystem cardSystem, TurnManager turnManager, PlayerSystem playerSystem, FieldService fieldService)
        {
            _cardSystem = cardSystem;
            _turnManager = turnManager;
            _playerSystem = playerSystem;
            _fieldService = fieldService;
        }

        public void Execute(PlayerActionCommand command)
        {
            // 1. カードシステムから効果を引き出し、カードを裏返す
            EffectData effect = _cardSystem.UseCard(command.Slot);

            if (effect == null)
            {
                Debug.LogWarning($"[PlayerAction] スロット {command.Slot} にカードがセットされていません。");
                return;
            }

            int playerId = _playerSystem.RuntimeData.ActorId;
            ActionRuntimeData actionData;

            // 2. 効果の種類に応じてチケットを生成
            if (effect.Type == ActionType.Move)
            {
                // 移動の場合は、押したボタンの位置をそのまま移動方向とする
                GridDirection dir = ConvertSlotToDirection(command.Slot);
                actionData = new ActionRuntimeData(playerId, dir);
            }
            else
            {
                // 攻撃等の場合は、プレイヤーの現在地を基準にターゲットマスを計算
                Vector2Int playerPos = _playerSystem.RuntimeData.Position;
                List<Vector2Int> targetCells = CalculateTargetCells(playerPos, effect.AreaType);

                actionData = new ActionRuntimeData(playerId, effect.Type, effect.Property, targetCells);
            }

            // 3. TurnManagerにアクションを提出
            _turnManager.SubmitPlayerAction(actionData);
        }

        /// <summary>
        /// 入力されたボタンに対応した方向をGridDirectionで返す
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        private GridDirection ConvertSlotToDirection(SlotPosition slot)
        {
            switch (slot)
            {
                case SlotPosition.Up:       return GridDirection.Up;
                case SlotPosition.Down:     return GridDirection.Down;
                case SlotPosition.Left:     return GridDirection.Left;
                case SlotPosition.Right:    return GridDirection.Right;
                default:                    return GridDirection.Up;
            }
        }


        private List<Vector2Int> CalculateTargetCells(Vector2Int origin, TargetAreaType areaType)
        {
            var cells = new List<Vector2Int>();

            switch (areaType)
            {
                case TargetAreaType.Front1:
                    cells.Add(origin + new Vector2Int(1, 0));
                    break;
                case TargetAreaType.FrontPierce2:
                    cells.Add(origin + new Vector2Int(1, 0));
                    cells.Add(origin + new Vector2Int(2, 0));
                    break;
                case TargetAreaType.Surround:
                    cells.Add(origin + new Vector2Int(1, 0));
                    cells.Add(origin + new Vector2Int(-1, 0));
                    cells.Add(origin + new Vector2Int(0, 1));
                    cells.Add(origin + new Vector2Int(0, -1));
                    break;
                case TargetAreaType.Self:
                    cells.Add(origin);
                    break;
            }

            return cells;
        }
    }
}
