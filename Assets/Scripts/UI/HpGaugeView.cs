// ------------------------------------------------------------
// File		: HpGaugeView.cs
// Summary	: HPゲージの表示を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - HPゲージの表示を更新するためのクラスです。HPの変化に応じてゲージの表示を更新します。
// ------------------------------------------------------------
using CreatorKousien.Core;
using UnityEngine;
using UnityEngine.UI;

public class HpGaugeView : MonoBehaviour
{
    [Header("HPゲージの設定")]
    [Tooltip("HPゲージのImageコンポーネント")]
    [SerializeField] private Image hpGauge;

    private float _currentHp;           /// 現在のHP
    private float _maxHp;               /// 最大HP
    private int _targetActorId;         /// HPを表示する対象のアクターID

    /// <summary>
    /// GameManagerから呼ばれる初期化処理
    /// </summary>
    /// <param name="eventBus">共通のマイク</param>
    /// <param name="actorId">監視対象のActorID (プレイヤーなら1など)</param>
    /// <param name="maxHp">最大HP</param>
    public void Initialize(GameEventBus eventBus, int actorId, float maxHp)
    {
        _targetActorId = actorId;
        _maxHp = maxHp;
        _currentHp = maxHp;

        // HPバーの初期化
        UpdateHPBar(_currentHp, _maxHp);

        // ダメージを受けたときのイベントを連携
        eventBus.OnDamageTaken += OnDamageTaken;
    }


    /// <summary>
    /// ダメージを受けたときのイベントハンドラー。対象のアクターIDとダメージ量を受け取る。
    /// </summary>
    /// <param name="actorId">対象のアクターID</param>
    /// <param name="damage">ダメージ量</param>
    private void OnDamageTaken(int actorId, int damage)
    {
        // もしダメージを受けたのが監視対象のアクターなら、バーを更新
        if (actorId == _targetActorId)
        {
            _currentHp -= damage;
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp); // HPは0未満にならないようにする
            UpdateHPBar(_currentHp, _maxHp);
        }
    }


    /// <summary>
    /// HPバーの表示を更新するメソッド。現在のHPと最大HPを受け取って、ゲージのfillAmountを更新します。
    /// </summary>
    /// <param name="currentHP">現在のHP</param>
    /// <param name="maxHP">最大HP</param>
    public void UpdateHPBar(float currentHP, float maxHP)
    {
        if (hpGauge != null && maxHP > 0)
        {
            hpGauge.fillAmount = currentHP / maxHP;
        }
    }
}
