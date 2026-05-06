// ------------------------------------------------------------
// File		: PlayerSpawner.cs
// Summary	: プレイヤーのスポーンを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// PlayerPrefabを生成し、初期位置に配置するクラス
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("プレハブ設定")]
        [Tooltip("生成するプレイヤーのプレハブ")]
        [SerializeField] private GameObject _playerPrefab;

        [Header("初期位置設定")]
        [Tooltip("プレイヤーの初期位置")]
        [SerializeField] private Transform _spawnPoint;



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// プレイヤーの生成と初期化を行うメソッド
        /// </summary>
        /// <returns>PlayerFacadeで生成されたプレイヤーのインスタンス</returns>
        public PlayerFacade Spawn()
        {
            if (_playerPrefab == null || _spawnPoint == null)
            {
                Debug.LogError("[PlayerSpawner] プレハブまたはSpawnPointが設定されていません。");
                return null;
            }

            // プレイヤーのプレハブを生成と配置
            PlayerFacade playerInstance = Instantiate(_playerPrefab, _spawnPoint.position, _spawnPoint.rotation).GetComponent<PlayerFacade>();

            playerInstance.Initialize();

            Debug.Log("[PlayerSpawner] プレイヤーの生成と初期化が完了しました。");
            return playerInstance;
        }
    }
}
