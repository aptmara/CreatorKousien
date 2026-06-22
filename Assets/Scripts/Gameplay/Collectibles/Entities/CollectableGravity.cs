// ------------------------------------------------------------
// File		: CollectableGravity.cs
// Summary	: 重力の方向をフィールドの傾きに合わせる
//
// Author	: [浅野 勇生]
// Created	: 2026-06-23
//
// Notes	:
// - 重力の方向をフィールドの傾きに合わせる
// ------------------------------------------------------------
using Game.Gameplay.Stage;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CollectableGravity : MonoBehaviour
{
    [Tooltip("重力加速度")]
    [SerializeField] private float _gravity = 9.8f;

    private Vector3 _gravityDir = Vector3.down;
    private Rigidbody _rb;
    private bool _released;     // 落とされてワールド重力に切り替えたかどうか


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;     // Rigidbody の重力は無効化する
    }


    private void OnEnable()
    {
        // プール再利用に備えて毎回フィールド重力にリセット
        _released = false;
        _rb.useGravity = false;
        _gravityDir = FieldContext.GravityDir;
    }

    /// <summary>
    /// アイテムが落とされたときに呼ばれる。ワールド重力に切り替える
    /// </summary>
    public void Release()
    {
        _released = true;
        _rb.useGravity = true;      // ワールド重力に切り替える
    }



    /// <summary>
    /// スタート時に FieldContext から重力方向を取得する
    /// </summary>
    void Start()
    {
        // FieldContext からフィールドの傾きを取得し、重力方向を設定する
        _gravityDir = FieldContext.GravityDir;
    }


    /// <summary>
    /// Rigidbody に重力方向の力を加える
    /// </summary>
    void FixedUpdate()
    {
        if (_released)
        {
            // 落とされた後はワールド重力に任せる
            return;
        }

        // 質量に頼らず、重力加速度を直接加えるために ForceMode.Acceleration を使用する
        _rb.AddForce(_gravityDir * _gravity, ForceMode.Acceleration);
    }


}
