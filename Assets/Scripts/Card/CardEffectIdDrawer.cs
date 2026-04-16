using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// 作りかけ

//public class CardEffectIdAttribute : PropertyAttribute { };

//[CustomPropertyDrawer(typeof(CardEffectIdAttribute))]
//public class CardEffectIdDrawer : PropertyDrawer
//{
//    public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
//    {
//        // データベースから情報を取得
//        EffectDataBase effectDataBase = GetDatabase();
//        List<int> ids = new List<int>();
//        List<string> names = new List<string>();
//        foreach(EffectData data in effectDataBase.GetEffectList())
//        {
//            ids.Add(data.EffectID);
//            names.Add(data.EffectName);
//        }
//        // IDから配列の要素番号を逆算
//        int index = ids.IndexOf(prop.intValue);
//        if (index < 0) index = 0;
//        // 要素番号と配列を使って表示
//        index = EditorGUI.Popup(rect, index, names.ToArray());
//        prop.intValue = ids[index];
//    }

//    EffectDataBase GetDatabase()
//    {
//        return ;
//    }
//}
