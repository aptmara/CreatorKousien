using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
public enum EffectValueType
{
    Attack,
    Buff,
    Diffence,

}

public enum TargetType
{
    Player,
    Enemy
}

public enum EffectType
{
    Attack,
    Buff,
    Diffence,
}

[System.Serializable]
public struct EffectValue
{
    // 効果タイプ、これによりValueの使われ方が変わる
    [SerializeField] private EffectValueType valueType;
    public EffectValueType ValueType => valueType;
    // 効果対象
    [SerializeField] private TargetType targetType;
    public TargetType TargetType => targetType;
    // 効果自体の値
    [SerializeField] private float value;
    public float Value => value;
}

[System.Serializable]
public struct EffectData
{
    //! 効果ID
    [SerializeField] private int effectID;
    public int EffectID => effectID;
    //! 効果の値、対象、分類が入っている
    [SerializeField] private List<EffectValue> value;
    public List<EffectValue> Value => value;
    //! 移動距離、これだけ別数値で保持
    [SerializeField] private Vector2Int moveValue;
    public Vector2Int MoveValue => moveValue;
    //! 効果発動までの遅延ターン
    [SerializeField] private int duration;
    public int Duration => duration;

    //! 効果名
    [SerializeField] private string effectName;
    public string EffectName => effectName;

    //! 効果説明
    [SerializeField] private string effectInfo;
    public string EffectInfo => effectInfo;

    //! 効果アイコン
    [SerializeField] private Sprite effectIcon;
    public Sprite EffectIcon => effectIcon;

    //! カード効果全体で見た分類
    [SerializeField] private EffectType effectType;
    public EffectType EffectType => effectType;
}

[CreateAssetMenu(fileName = "SO_EffectDataBase", menuName = "CreatorKousien/Data/EffectDataBase")]
public class EffectDataBase : ScriptableObject
{
    //! 効果リスト、基本的にすべてここで管理される
    [SerializeField]
    EffectData[] _effectList;
    //! 不正な値が入力された際に返す予備の値
    [SerializeField]
    EffectData _fallbackEffect;

    public void CheckAllData()
    {
        Dictionary<int, bool> hasData = new Dictionary<int, bool>();
        int i = 0;
        foreach(EffectData effectData in _effectList)
        {
            bool isSafe = true;
            // IDの数値チェック
            if(CheckID(effectData))
            {
                Debug.LogError("[EffectDataBase]要素数" + i + "番のIDの値が不正です！");
                isSafe = false;
            }
            // アイコンチェック
            if (CheckIcon(effectData))
            {
                Debug.LogError("[EffectDataBase]要素数" + i + "番のアイコンの値が不正です！");
            }
            // 重複チェック
            if(CheckDuplicateID(effectData, hasData))
            {
                Debug.LogError("[EffectDataBase]要素数" + i + "のID" + effectData.EffectID + "が重複しています！");
                isSafe = false;
            }
            
            if(isSafe)
            {
                hasData.Add(effectData.EffectID, isSafe);
            }
            i++;
        }
    }

    bool CheckID(EffectData effectData)
    {
        return effectData.EffectID < 0;
    }

    bool CheckIcon(EffectData effectData)
    {
        return effectData.EffectIcon == null;
    }

    bool CheckDuplicateID(EffectData effectData, Dictionary<int, bool> hasData)
    {
        bool exists;
        return hasData.TryGetValue(effectData.EffectID, out exists) && exists;
    }

    public EffectData GetEffect(int a_effectID)
    {
        // 見つからなかった時用のデータを挿入
        EffectData ret = _fallbackEffect;
        //  IDで探索
        foreach (EffectData data in _effectList)
        {
            // IDが違ったら次へ
            if (data.EffectID != a_effectID) continue;
            // IDが等しかったら上書きして抜ける
            ret = data;
            break;
        }

        return ret;
    }

#if UNITY_EDITOR
    public IReadOnlyList<EffectData> GetEffectList()
    {
        return _effectList;
    }
#endif
}
