using UnityEngine;

[CreateAssetMenu(fileName = "Spawn", menuName = "Scriptable Objects/Spawn")]
public class EnemyDebugSpawneDefinition : ScriptableObject
{

    [Tooltip("1回の自動スポーンで出す敵の数")]
    [SerializeField, Min(1)] private int _enemiesPerSpawn = 1;
}
