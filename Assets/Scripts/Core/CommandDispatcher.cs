// ------------------------------------------------------------
// File		: CommandDispatcher.cs
// Summary	: ICommandを適切なUseCaseに分配するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 随時コマンドやUseCaseに応じて更新する予定
// - AttackCommand,EnemyActionCommandの追加に伴い、Dispatcherも更新 (4/17)
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
        /// コンストラクタ
        /// </summary>
        public CommandDispatcher() {}


        /// <summary>
        /// コマンドの型と、それを処理する関数を登録するメソッド
        /// [T]が来たら[handler]を実行する、というルールを登録する
        /// [T]はICommandを実装したクラスでなければならない
        /// </summary>
        /// <typeparam name="T">ICommandの実装クラス</typeparam>
        /// <param name="handler">実行するUseCase</param>
        public void Register<T>(Action<T> handler) where T : ICommand
        {
            _handlers[typeof(T)] = (cmd) => handler((T)cmd);
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

