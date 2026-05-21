using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using UnityEngine;

public class CompressCollectable : MonoBehaviour
{
    [SerializeField,Tooltip("落下判定用のフィールドのTransform")]
    public Transform field;

    [SerializeField, Tooltip("解放時に生成するプレハブ")]
    private CollectibleObject _ExpandPrefab;

    [SerializeField, Tooltip("解放する際に設定するSO")]
    private CollectibleData[] _data;

    // 解放時の生成個数
    public int amount = 10;

    public void OnStart()
    {// これうまくいってない
        field = GameObject.FindGameObjectWithTag("Floor").transform;
    }

    private void FixedUpdate()
    {
        // 位置から無理やり解放位置を指定
        if(transform.position.y < 0.0f)
        {
            Expand();
        }
    }

    
    public void Expand()
    {
        // 解放する個数に応じて生成
        for (int i = 0; i < amount; ++i)
        {
            Vector3 offset =
                Random.insideUnitSphere * 0.5f;
            // 生成
            var obj = Instantiate(
                _ExpandPrefab,
                transform.position,
                Quaternion.identity
                );
            // 生成オブジェクト
            var collect = obj.GetComponent<CollectibleObject>();
            collect.Initialize(
                _data[UnityEngine.Random.RandomRange(0, _data.Length)],
                Destroy,
                false
                );
            // 解放時に爆散するように
            var rb = obj.GetComponent<Rigidbody>();
            rb.AddExplosionForce(
                amount,transform.position,
                5.0f,3.0f,ForceMode.Impulse);
            // 二秒後に削除
            Destroy(obj.gameObject, 2.0f);
        }
        // 圧縮オブジェクトを削除
        Destroy(gameObject);
    }

}
