// ------------------------------------------------------------
// File		: GameMediator.cs
// Summary	: ゲーム全体の中央となるクラス。外部から命令を受ける窓口であり、各システムをつなぐ役割を担う。
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 設計書をもとに、ゲーム全体の調整役となるクラスを定義。
// ------------------------------------------------------------
using UnityEngine;
using CreatorKousien.Command;

namespace CreatorKousien.Core
{
    /// <summary>
    /// ゲーム全体の進行を管理するクラス
    /// Commandを受け取って、各システムに流す役割を担う
    /// </summary>
    public class GameMediator
    {
        /// <summary>
        /// コマンドディスパッチャーへの参照
        /// </summary>
        private CommandDispatcher _dispatcher;


        /// <summary>
        /// ゲーム内のイベントを管理するイベントバスへの参照
        /// </summary>
        public GameEventBus EventBus { get; private set; }


        /// <summary>
        /// 初期化処理。CommandDispatcherを受け取って、内部で保持する。
        /// </summary>
        /// <param name="dispatcher">ディスパッチャーの参照</param>
        /// <param name="eventBus">イベントバスの参照</param>
        public void Initialize(CommandDispatcher dispatcher, GameEventBus eventBus)
        {
            _dispatcher = dispatcher;
            EventBus = eventBus;
            Debug.Log("[GameMediator] 起動完了");
        }


        /// <summary>
        /// 外部からコマンドを受け取るためのメソッド。受け取ったコマンドは、CommandDispatcherに転送する。
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCommand(ICommand cmd)
        {
            if (_dispatcher == null)
            {
                Debug.LogError("[GameMediator] CommandDispatcherが未設定です。コマンドを送信できません。");
                return;
            }
            Debug.Log($"<color=orange>[GameMediator] {cmd.GetType().Name} を受付！ Dispatcherへ転送します。</color>");

            // ディスパッチャーに丸投げする
            _dispatcher.Dispatch(cmd);
        }
    }
}
