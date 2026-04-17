// ------------------------------------------------------------
// File		: MoveUseCase.cs
// Summary	: 移動に関するユースケース
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 設計書をもとに、移動に関するユースケースを定義しています
// ------------------------------------------------------------
using UnityEngine;
using CreatorKousien.Command;
using CreatorKousien.Field;

namespace CreatorKousien.UseCase
{
    /// <summary>
    /// 移動に関するユースケース
    /// </summary>
    public class MoveUseCase
    {
        private FieldService _fieldService;         /// フィールドサービス
        private TileEffectSystem _tileEffect;       /// タイルエフェクトシステム


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fieldService">フィールドサービス</param>
        /// <param name="tileEffect">タイルエフェクトシステム</param>
        public MoveUseCase(FieldService fieldService, TileEffectSystem tileEffect)
        {
            _fieldService = fieldService;
            _tileEffect = tileEffect;
        }


        public void Execute(MoveCommand cmd)
        {
            // ----- 1. 現在の位置を取得 -----
            Vector2Int currentPos = _fieldService.GetActorPosition(cmd.MoverId);
            if (currentPos.x == -1)
            {
                Debug.LogWarning($"[MoveUseCase] ActorId {cmd.MoverId} の現在位置が見つかりません。移動をキャンセルします。");
                return;
            }

            // ----- 2. 移動先の位置を計算 -----
            Vector2Int delta = Vector2Int.zero;
            switch (cmd.Direction)
            {
                case GridDirection.Up: delta = new Vector2Int(0, -cmd.Distance); break;
                case GridDirection.Down: delta = new Vector2Int(0, cmd.Distance); break;
                case GridDirection.Left: delta = new Vector2Int(-cmd.Distance, 0); break;
                case GridDirection.Right: delta = new Vector2Int(cmd.Distance, 0); break;
            }
            Vector2Int targetPos = currentPos + delta;

            // ----- 3. FieldServiceに移動リクエストを送る -----
            if (!_fieldService.CanMoveTo(cmd.MoverId, targetPos.x, targetPos.y))
            {
                Debug.Log($"[MoveUseCase] ActorId {cmd.MoverId} は位置 ({targetPos.x}, {targetPos.y}) に移動できません。");
                return;
            }

            // ----- 4. 移動を実行 -----
            Debug.Log($"<color=cyan>[MoveUseCase] ActorID:{cmd.MoverId} が {currentPos} から {targetPos} へ移動します！</color>");

            _tileEffect.TriggerOnExit(cmd.MoverId, currentPos.x, currentPos.y); // 現在のマスから出るときの床効果を呼び出す

            _fieldService.UpdateOccupancy(cmd.MoverId, currentPos.x, currentPos.y, targetPos.x, targetPos.y); // FieldServiceに移動を通知

            _tileEffect.TriggerOnEnter(cmd.MoverId, targetPos.x, targetPos.y); // 移動先のマスに入るときの床効果を呼び出す

            _fieldService.NotifyActorMoved(cmd.MoverId, targetPos.x, targetPos.y); // Actorの移動をFieldServiceに通知（必要に応じて）

        }
    }

}
