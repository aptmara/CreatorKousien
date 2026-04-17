// ------------------------------------------------------------
// File		: CommandDispatcher.cs
// Summary	: ICommandを適切なUseCaseに分配するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 随時コマンドやUseCaseに応じて更新する予定
// ------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;
using CreatorKousien.Command;
using CreatorKousien.UseCase;

namespace CreatorKousien.Core
{
    /// <summary>
    /// ICommandを適切なUseCaseに分配するクラス
    /// </summary>
    public class CommandDispatcher
    {
        /// <summary>
        /// 型(Type) と それを実行する関数(Action) の辞書
        /// </summary>
        private readonly Dictionary<Type, Action<ICommand>> _handlers = new Dictionary<Type, Action<ICommand>>();


        /// <summary>
        /// GameManagerから各種UseCaseを受け取って、辞書に登録する
        /// </summary>
        /// <param name="moveUseCase">移動要求コマンド</param>
        public CommandDispatcher(MoveUseCase moveUseCase /* 将来的にここにコマンドをどんどん追加！ */)
        {
            // MoveCommandの処理を登録
            _handlers[typeof(MoveCommand)] = cmd =>
            {
                var moveCmd = (MoveCommand)cmd;
                moveUseCase.Execute(moveCmd);
            };
        }


        /// <summary>
        /// UIや入力システムから送られてきたICommandを、登録されたUseCaseに分配して実行する
        /// </summary>
        /// <param name="command">分配するコマンド</param>
        public void Dispatch(ICommand command)
        {
            if (_handlers.TryGetValue(command.GetType(), out var handler))
            {
                handler(command);       // 該当するUseCaseに横流し
            }
            else
            {
                Debug.LogWarning($"[Dispatcher] {command.GetType().Name} のハンドラーが見つかりません！");
            }
        }
    }
}

