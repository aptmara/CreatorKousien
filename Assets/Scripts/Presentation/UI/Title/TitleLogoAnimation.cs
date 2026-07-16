using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Presentation.UI.Title
{


    public class TitleLogoAnimation : MonoBehaviour
    {

        enum TitleAnimState
        {
            State_Initial,
            State_DemonPop,
            State_Scatter,
            State_PumpkinPop,
            State_LogoPop,
            State_GhostAssign,
            State_Jump,
        };
        private TitleAnimState _currentState;
        private bool _isFinish;

        [SerializeField] private RectTransform _rootTransform;
        [SerializeField] private List<Sprite> _scatterSprites;
        [SerializeField] private GameObject _scatterObj;

        [Header("====== あくまポップ設定 ======")]
        [SerializeField] private float _demonPopTime;
        [SerializeField] private float _demonPopMultiplier;


        [Header("====== 小物生成設定 ======")]
        [SerializeField] private float _scatterInterval;
        private float _currentScatterInterval;
        [SerializeField] private int _scatterCount;
        [SerializeField] private int _scatterTimes;
        [SerializeField] private float _scatterScale;
        [SerializeField] private float _scatterRotMin;
        [SerializeField] private float _scatterRotMax;
        private int _currentScatterTimes;

        [Header("====== かぼちゃポップ設定 ======")]
        [SerializeField] private float _pumpkinPopTime;
        [SerializeField] private float _pumpkinPopMultiplier;

        [Header("====== ロゴポップ設定 ======")]
        [SerializeField] private float _logoPopTime;
        [SerializeField] private float _logoPopMultiplier;

        [Header("====== お化け参上 ======")]
        [SerializeField] private float _ghostAssignTime;
        [SerializeField] private float _ghostMoveMultiplier;
        private Vector3 _ghostInitPos;

        [Header("====== ジャンプ ======")]
        [SerializeField] private float _JumpChargeTime;
        [SerializeField] private float _JumpTime;
        [SerializeField] private float _JumpScale;
        private Vector3 _jumpInitPos;

        [Header("======== アニメーション全般 =======")]
        [SerializeField] private float _animScale;
        private float _currentAnimTime;
        [SerializeField] private float _nextInterval;
        private float _currentNextInterval;

        [Header("======== アニメーションで使用するオブジェクト =======")]
        [SerializeField] private GameObject _defLogo;
        [SerializeField] private GameObject _fill;
        [SerializeField] private GameObject _demon;
        [SerializeField] private GameObject _onhandItems;
        [SerializeField] private GameObject _pumpkin;
        [SerializeField] private GameObject _logo;
        [SerializeField] private GameObject _ghost;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if(_rootTransform == null)
                _rootTransform = GetComponent<RectTransform>();
            _currentScatterInterval = 0.0f;

            _currentState = TitleAnimState.State_Initial;
            
        }

        private void FixedUpdate()
        {
            switch (_currentState)
            {
                case TitleAnimState.State_Initial:
                    Initialize();
                    break;
                case TitleAnimState.State_DemonPop:
                    UpdateDemonPop();
                    break;
                case TitleAnimState.State_Scatter:
                    UpdateScatter();
                    break;
                case TitleAnimState.State_PumpkinPop:
                    PumpkinPop();
                    break;
                case TitleAnimState.State_LogoPop:
                    UpdateLogoPop();
                    break;
                case TitleAnimState.State_GhostAssign:
                    UpdateGhostAssign();
                    break;
                case TitleAnimState.State_Jump:
                    UpdateJump();
                    break;
            }
        }

        void Initialize()
        {
            _currentAnimTime = _demonPopTime;
            // すべての対象オブジェクトの有効設定
            _defLogo.SetActive(false);
            _fill.SetActive(true);

            // あくまの設定
            _demon.SetActive(false);

            _onhandItems.SetActive(false);
            _pumpkin.SetActive(false);
            _logo.SetActive(false);
            _ghost.SetActive(false);

            _currentState = TitleAnimState.State_DemonPop;
        }

        void UpdateDemonPop()
        {
            _currentAnimTime -= Time.fixedDeltaTime;
            if (_currentAnimTime < 0.0f)
            {
                _currentState = TitleAnimState.State_Scatter;
                _currentScatterInterval = _scatterInterval;
                _currentScatterTimes = 0;
                _demon.transform.localScale = new Vector3(1.0f,1.0f,1.0f);
                return;
            }
            if (!_demon.activeSelf)
                _demon.SetActive(true);

            float rate = _currentAnimTime / _demonPopTime;
            float upper = Mathf.Sin(Mathf.Deg2Rad * (rate * 180.0f)) * _demonPopMultiplier;
            Vector3 scale = new Vector3(
                1.0f - upper,
                1.0f + upper,
                1.0f
                );
            _demon.transform.localScale = scale;
        }

        void UpdateScatter()
        {
            _currentScatterInterval -= Time.unscaledDeltaTime;
            if (_currentScatterInterval > 0.0f) return;
            
            _currentScatterTimes++;
            _currentScatterInterval = _scatterInterval;
            if (_currentScatterTimes > _scatterTimes)
            {
                _currentAnimTime = _pumpkinPopTime;
                _currentNextInterval = _nextInterval;
                _currentState = TitleAnimState.State_PumpkinPop;
                return;
            }
            for (int i = 0; i < _scatterCount; ++i)
            {
                GameObject obj = Instantiate(_scatterObj, _rootTransform);

                // 画像設定
                var image = obj.GetComponent<Image>();
                image.sprite = _scatterSprites[Random.Range(0, _scatterSprites.Count)];
                //　物理設定
                var rb = obj.GetComponent<Rigidbody2D>();
                //　速度設定
                rb.AddForce(new Vector2(
                    Random.Range(-10.0f, 10.0f) * _scatterScale,
                    Random.Range(5.0f, 10.0f) * _scatterScale),
                    ForceMode2D.Impulse);
                float rotPower = Random.Range(-30.0f, 30.0f);
                rotPower = rotPower >= 0.0f ?
                    Mathf.Clamp(rotPower,_scatterRotMin,_scatterRotMax):
                    Mathf.Clamp(rotPower, -_scatterRotMax, -_scatterRotMin);
                rb.AddTorque(rotPower);
                // 一定期間後に削除
                Destroy(obj, 5.0f);
            }
        }

        void PumpkinPop()
        {
            if (_currentNextInterval > 0.0f)
            {
                _currentNextInterval -= Time.unscaledDeltaTime;
                return;
            }
            _currentAnimTime -= Time.fixedDeltaTime;
            if (_currentAnimTime < 0.0f)
            {
                _currentState = TitleAnimState.State_LogoPop;
                _currentAnimTime = _logoPopTime;
                _pumpkin.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                _onhandItems.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);                
                return;
            }
            if (!_pumpkin.activeSelf)
                _pumpkin.SetActive(true);
            if(!_onhandItems.activeSelf)
                _onhandItems.SetActive(true);

            float rate = _currentAnimTime / _pumpkinPopTime;
            float upper = Mathf.Sin(Mathf.Deg2Rad * (rate * 180.0f)) * _pumpkinPopMultiplier;
            Vector3 scale = new Vector3(
                1.0f - upper,
                1.0f + upper,
                1.0f
                );
            _pumpkin.transform.localScale = scale;
            _onhandItems.transform.localScale = scale;
        }

        void UpdateLogoPop()
        {
            _currentAnimTime -= Time.fixedDeltaTime;
            if (_currentAnimTime < 0.0f)
            {
                _currentState = TitleAnimState.State_GhostAssign;
                _currentAnimTime = _ghostAssignTime;
                var rect = _ghost.GetComponent<RectTransform>();
                _ghostInitPos = rect.position;
                _logo.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                return;
            }
            if (!_logo.activeSelf)
                _logo.SetActive(true);
           

            float rate = _currentAnimTime / _logoPopTime;
            float upper = Mathf.Sin(Mathf.Deg2Rad * (rate * 180.0f)) * _logoPopMultiplier;
            Vector3 scale = new Vector3(
                1.0f - upper,
                1.0f + upper,
                1.0f
                );
            
            _logo.transform.localScale = scale;
        }

        void UpdateGhostAssign()
        {
            _currentAnimTime -= Time.unscaledDeltaTime;
            if (_currentAnimTime < 0.0f)
            {
                var temp = _ghost.GetComponent<RectTransform>();
                _currentAnimTime = _JumpChargeTime + _JumpTime;
                temp.position = _ghostInitPos;
                _jumpInitPos = _rootTransform.position;
                _currentState = TitleAnimState.State_Jump;
            }
            if (!_ghost.activeSelf)
                _ghost.SetActive(true);

            float rate = _currentAnimTime / _ghostAssignTime;
            float rad = Mathf.Deg2Rad * (rate * 360.0f);
            var rect = _ghost.GetComponent<RectTransform>();

            Vector3 newPos = new Vector3(
                _ghostInitPos.x + rate * _ghostMoveMultiplier,
                _ghostInitPos.y + Mathf.Sin(rad) * _ghostMoveMultiplier,
                0.0f
                );
            rect.position = newPos;
        }

        void UpdateJump()
        {
            _currentAnimTime -= Time.unscaledDeltaTime;
            if (_currentAnimTime < 0.0f)
            {
                _fill.SetActive(false);
                gameObject.SetActive(false);
                _defLogo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                _defLogo.SetActive(true);
                return;
            }

            var rootRect = _rootTransform.GetComponent<RectTransform>();
            Vector3 scale;
            if (_currentAnimTime > _JumpTime)
            {
                float chargeRate = ( _currentAnimTime - _JumpTime ) / _JumpChargeTime;
                float chargeRad = Mathf.Deg2Rad * (chargeRate * 180.0f);

                rootRect.pivot = new Vector2(0.5f,0.0f);
                scale = new Vector3(
                    Mathf.Cos(chargeRad),
                    Mathf.Sin(chargeRad),
                    0.0f
                    );
                _rootTransform.localScale = scale;

                return;
            }
            float jumpRate = (_currentAnimTime - _JumpChargeTime) / _JumpTime;
            float jumpRad = Mathf.Deg2Rad * (jumpRate * 180.0f);

            rootRect.pivot = new Vector2(0.5f, 0.0f);
            scale = new Vector3(
                0.8f,
                1.2f,
                0.0f
                );

            var pos = new Vector3(
                _jumpInitPos.x,
                _jumpInitPos.y + (1 - jumpRate) * _JumpScale,
                _jumpInitPos.z
                );
            _rootTransform.position = pos;
            _rootTransform.localScale = scale;
        }
    }

}
