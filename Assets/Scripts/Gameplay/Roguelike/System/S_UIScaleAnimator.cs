//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UIScaleAnimator.cs
// brief  : ローグライクシーンでのUIアニメーション
//
// auther : Takitani Shohei
// date   : 2026/07/14 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

public class S_UIScaleAnimator : MonoBehaviour
{
    [Header("対象")]
    [SerializeField] private RectTransform _target;

    [Header("フォーカス中(継続的な拡縮)")]
    [Tooltip("フォー活中の拡大倍率")]
    [SerializeField] private float _hoverScale = 1.1f;

}
