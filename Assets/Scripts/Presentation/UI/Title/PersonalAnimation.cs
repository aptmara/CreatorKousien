/**
 * UIオブジェクトがふよふよする動きを実現する
 * 
 * 
 * テラダ
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Title
{

    public class PersonalAnimation : MonoBehaviour
    {
        /// <summary>
        /// 個別のアニメーションの種類
        /// </summary>
        enum PersonalAnimType
        {
            AnimType_Float,
            AnimType_Pop,
            AnimType_Scale,
            AnimType_Rotate,
            AnimType_Scatter,
        };


        [Header("======= 基本情報 ========")]
        [SerializeField,Tooltip("アニメーションの種類")]
        private PersonalAnimType _animType;
        [SerializeField, Tooltip("アニメーションのスピード")]
        private float _cycleSpeed = 1.0f;
        private float _currentCycle = 0.0f;


        [Header("======= アニメーションのパラメータ =======")]
        [SerializeField, Tooltip("アニメーションの大きさ")]
        private float _animScale = 1.0f;

        [SerializeField, Tooltip("アニメーションのインターバル")]
        private float _animInterval = 0.0f;
        private float _currentInterval = 0.0f;
        [SerializeField, Tooltip("アニメーションの継続時間")]
        private float _continueTime;

        [SerializeField, Tooltip("ばらまく個数")]
        private int _scatterValue = 10;
        [SerializeField, Tooltip("ばらまく小物のSprite")]
        private List<Sprite> _scatterItems;
        [SerializeField,Tooltip("ばらまくオブジェクトのプレハブ")]
        private GameObject _scatterObj;
        [SerializeField,Tooltip("最小回転量")] private float _scatterRotMin;
        [SerializeField,Tooltip("最大回転量")] private float _scatterRotMax;

        [SerializeField, Tooltip("アンカーポイント")]
        private Vector2 _anchorPoint = new Vector2( 0.5f,0.5f );

        // アニメーションに使うRectTrasnform
        [SerializeField,Tooltip("基準のトランスフォーム")]
        private RectTransform _transform;
        // 初期位置パラメータ保存用
        private Vector3 _initialPosition;
        private Vector3 _initialScale;
        private Quaternion _initialRotation;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _currentCycle = 0.0f;
            _currentInterval = 0.0f;
            if(_transform == null)
                _transform = gameObject.GetComponent<RectTransform>();
            _initialPosition = _transform.localPosition;
            _initialScale = _transform.localScale;
            _initialRotation = _transform.localRotation;
            _transform.pivot = _anchorPoint;
        }

        private void FixedUpdate()
        {
            _currentInterval -= Time.unscaledDeltaTime;
            if (_currentInterval > 0.0f)
            {
                _currentCycle = 0.0f;
                return;
            }
            _currentCycle += (Time.unscaledDeltaTime);
            if (_currentCycle >= _continueTime)
            {
                if(Mathf.Abs(Mathf.Sin(_currentInterval)) < 0.01f)
                    _currentInterval = _animInterval;
            }

            switch (_animType)
            {
                case PersonalAnimType.AnimType_Float:
                    FloatAnimUpdate();
                    break;
                case PersonalAnimType.AnimType_Scale:
                    ScaleAnimUpdate();
                    break;
                case PersonalAnimType.AnimType_Rotate:
                    RotateAnimUpdate();
                    break;
                case PersonalAnimType.AnimType_Scatter:
                    ExecuteScatter();
                    break;
            }
        }

        private void FloatAnimUpdate()
        {
            Vector3 positionTemp = new Vector3(
                    Mathf.Cos(_currentCycle * _cycleSpeed * 0.5f) * _animScale,
                    Mathf.Sin(_currentCycle * _cycleSpeed) * _animScale,
                    0.0f
                );

            _transform.localPosition = _initialPosition + positionTemp;
        }

        private void ScaleAnimUpdate()
        {
            Vector3 scaleTemp = new Vector3(
                    Mathf.Abs(Mathf.Sin(_currentCycle * _cycleSpeed) * _animScale),
                    Mathf.Abs(Mathf.Sin(_currentCycle * _cycleSpeed) * _animScale),
                    0.0f
                );

            _transform.localScale = _initialScale + scaleTemp;
        }

        private void RotateAnimUpdate()
        {
            Quaternion quatTemp = _initialRotation;
            quatTemp.z += Mathf.Sin(_currentCycle) * _cycleSpeed * _animScale;
            _transform.localRotation = quatTemp;
        }

        private void ExecuteScatter()
        {
            _currentInterval = _animInterval;
            for(int i = 0; i < _scatterValue;++i)
            {
                GameObject obj = Instantiate(_scatterObj,_transform);
                
                // 画像設定
                var image = obj.GetComponent<Image>();
                image.sprite = _scatterItems[Random.Range(0, _scatterItems.Count)];
                //　物理設定
                var rb = obj.GetComponent<Rigidbody2D>();
                //　速度設定
                rb.AddForce(new Vector2(
                    Random.Range(-10.0f, 10.0f) * _animScale,
                    Random.Range(5.0f, 10.0f) * _animScale),
                    ForceMode2D.Impulse);
                float rotPower = Random.Range(-30.0f, 30.0f);
                rotPower = rotPower >= 0.0f ?
                    Mathf.Clamp(rotPower, _scatterRotMin, _scatterRotMax) :
                    Mathf.Clamp(rotPower, -_scatterRotMax, -_scatterRotMin);
                rb.AddTorque(rotPower);

                // 一定期間後に削除
                Destroy(obj, _continueTime);
            }
        }
}

}
