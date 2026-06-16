using System.Transactions;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Presentation.UI.Combo
{
public class ComboGaugeUI : MonoBehaviour
{
        [Header("======== ゲージ情報 ========")]

        [SerializeField, Tooltip("段階アップコンボ値")]
        private int _gaugeUpdateValue;

        private float _gaugeValue;

        [SerializeField, Tooltip("コンボレベルの最大値")]
        private int _maxGaugeLevel;

        [SerializeField, Tooltip("")]
        private float _gaugeBonusRatio = 0.3f;

        [Header("======== SubScene連携 ========")]
        [SerializeField, Tooltip("SubScene同士で実行したい関数を連携するSO")]
        private SceneEventChannel _sceneEventChannel;

        // コンボレベル(仮)の値
        private int _gaugeLevel;
        // コンボの値
        private int _comboValue;

        // アップグレードできるかどうか確認するための変数
        private int _nextComboValue;

        [SerializeField, Tooltip("ゲージのフレーム")]
        private Image _gaugeFrame;

        [SerializeField,Tooltip("ゲージのImage")]
        private Image _gaugeImgae;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _comboValue = 0;
            _gaugeValue = 0.0f;
            _gaugeLevel = 0;
            _gaugeFrame.color = Color.red;
            Vector3 currentScale = _gaugeImgae.rectTransform.localScale;
            currentScale.y = _gaugeValue;
            _gaugeImgae.rectTransform.localScale = currentScale;
        }

        /// <summary>
        /// 更新するコンボの量を投げて処理する関数
        /// </summary>
        /// <param name="comboVal"></param>
        /// <returns></returns>
        public int GaugeUpdate(int comboVal)
        {
            // ゲージ用コンボの更新
            _comboValue += comboVal;
            _nextComboValue += comboVal;
            if(_nextComboValue >= _gaugeUpdateValue)
            {
                _nextComboValue -= _gaugeUpdateValue;
                // コンボの段階レベルを更新
                if(++_gaugeLevel >= _maxGaugeLevel)
                    _gaugeLevel = _maxGaugeLevel;
                Debug.Log("Upgrade CombpGauge : " + _gaugeLevel);
                // フレームの色も更新
                Color currentCol = _gaugeImgae.color;
                currentCol.b = (float)_gaugeLevel / (float)_maxGaugeLevel;
                _gaugeImgae.color = currentCol;

                // SubScene連携関数を実行
                _sceneEventChannel?.ExecuteEvent(
                    (int)(_gaugeLevel * _gaugeUpdateValue * _gaugeBonusRatio));

                Debug.Log("GaugeFrameColor B : " + _gaugeFrame.color.b);
            }
            _gaugeValue = (float)_nextComboValue / (float)_gaugeUpdateValue;


            // コンボのゲージ量からゲージの大きさを計算
            Vector3 currentScale = _gaugeImgae.rectTransform.localScale;
            currentScale.y = _gaugeValue;
            _gaugeImgae.rectTransform.localScale = currentScale;

            return _gaugeLevel;
        }

        public void resetGauge()
        {
            _gaugeLevel = 0;
            _gaugeValue = 0.0f;
            _nextComboValue = 0;
            // ゲージの大きさ
            Vector3 currentScale = _gaugeImgae.rectTransform.localScale;
            currentScale.y = _gaugeValue;
            _gaugeImgae.rectTransform.localScale = currentScale;

            // フレームの色の初期化
            Color currentCol = _gaugeFrame.color;
            currentCol.b = (float)_gaugeLevel / (float)_maxGaugeLevel;
            _gaugeFrame.color = currentCol;
        }
    }

}
