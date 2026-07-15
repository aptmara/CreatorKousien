// 制作者: 山内陽
using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 敵の頭上にHPと攻撃ゲージを表示するWorld Space UI。
    /// </summary>
    public sealed class EnemyWorldStatusView : MonoBehaviour
    {
        [Tooltip("表示対象の敵ID。実行時に親のEnemyControllerから自動取得されるユニークなID。")]
        private string _targetEnemyId;

        private void Start()
        {
            var controller = GetComponentInParent<Game.Core.Enemy.EnemyController>();
            if (controller != null && !string.IsNullOrEmpty(controller.InstanceEnemyId))
            {
                _targetEnemyId = controller.InstanceEnemyId;
            }
        }

        private bool _hasBarrier;

        public void Initialize(string enemyID, Vector3 worldOffset, bool hasBarrier)
        {
            _targetEnemyId = enemyID;
            _worldOffset = worldOffset;
            _hasBarrier = hasBarrier;

            // 初期化時にUIを生成（バリア有無が確定した後）
            if (_uiElements.Count == 0)
            {
                BuildAllViews();
            }
        }

        [SerializeField]
        [Tooltip("敵RootからUIを出す高さ")]
        private Vector3 _worldOffset = new Vector3(0f, 1.6f, 0.5f);

        [SerializeField]
        [Tooltip("World Space Canvasの大きさ")]
        private Vector2 _canvasSize = new Vector2(220f, 56f);

        [SerializeField]
        [Tooltip("World Space Canvasのスケール")]
        private float _canvasScale = 0.012f;

        private class StatusUIElement
        {
            public Transform TargetTransform;
            public Canvas Canvas;
            public Slider HpSlider;
            public Slider GaugeSlider;
            public Image HpFillImage;
            public Image GaugeFillImage;
            public Vector3 ElementOffset;
        }

        private readonly System.Collections.Generic.List<StatusUIElement> _uiElements = new System.Collections.Generic.List<StatusUIElement>();
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void BuildAllViews()
        {
            // 子オブジェクトからすべてのHitReceiverを取得して、それぞれにUIを付ける
            var hitReceivers = GetComponentsInChildren<Game.Core.Enemy.EnemyHitReceiver>();
            if (hitReceivers != null && hitReceivers.Length > 0)
            {
                foreach (var receiver in hitReceivers)
                {
                    BuildView(receiver);
                }
            }
            else
            {
                // HitReceiverが一つもない場合のフォールバック（最初のRendererか自分自身）
                var renderer = GetComponentInChildren<Renderer>();
                Transform target = renderer != null ? renderer.transform : transform;
                BuildView(null, target);
            }
        }

        private void OnEnable()
        {
            var controller = GetComponentInParent<Game.Core.Enemy.EnemyController>();
            if (controller != null)
            {
                controller.OnHealthChanged += OnHealthChanged;
                controller.OnGaugeChanged += OnGaugeChanged;
                controller.OnDownStarted += OnDownStarted;
                controller.OnDefeated += OnDefeated;
            }
        }

        private void OnDisable()
        {
            var controller = GetComponentInParent<Game.Core.Enemy.EnemyController>();
            if (controller != null)
            {
                controller.OnHealthChanged -= OnHealthChanged;
                controller.OnGaugeChanged -= OnGaugeChanged;
                controller.OnDownStarted -= OnDownStarted;
                controller.OnDefeated -= OnDefeated;
            }
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            foreach (var element in _uiElements)
            {
                if (element.Canvas == null || element.TargetTransform == null)
                {
                    continue;
                }

                // Root座標を基準にする（実体のローカルオフセットに影響されないようにする）
                element.Canvas.transform.position = transform.position + element.ElementOffset;

                if (_mainCamera != null)
                {
                    // カメラの回転をそのままコピー（ビルボード）
                    element.Canvas.transform.rotation = _mainCamera.transform.rotation;
                }
            }
        }

        /// <summary>
        /// World Space Canvasとバーを生成する。
        /// </summary>
        private void BuildView(Game.Core.Enemy.EnemyHitReceiver receiver, Transform fallbackTarget = null)
        {
            Transform target = receiver != null ? receiver.transform : fallbackTarget;
            Vector3 offset = receiver != null ? receiver.UIWorldOffset : _worldOffset;

            GameObject canvasObject = new GameObject($"EnemyWorldStatusCanvas_{target.name}");
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = offset;
            canvasObject.transform.localScale = Vector3.one * _canvasScale;
            canvasObject.layer = gameObject.layer; // 敵と同じレイヤーに設定

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            canvasObject.AddComponent<CanvasScaler>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = _canvasSize;

            var hpSlider = CreateSlider("HpBar", canvasRect, new Vector2(0f, 12f), new Color(0.1f, 0.85f, 0.25f, 1f));
            Slider gaugeSlider = null;
            Image gaugeFillImage = null;

            if (_hasBarrier)
            {
                gaugeSlider = CreateSlider("GaugeBar", canvasRect, new Vector2(0f, -12f), new Color(1f, 0.8f, 0.1f, 1f));
                gaugeFillImage = gaugeSlider.fillRect.GetComponent<Image>();
                gaugeSlider.value = 0f;
                Debug.Log($"[EnemyWorldStatusView] ゲージUIを生成しました！ target={target.name}");
            }
            else
            {
                Debug.Log($"[EnemyWorldStatusView] バリアが無いためゲージUIの生成をスキップしました。 target={target.name}");
            }

            var hpFillImage = hpSlider.fillRect.GetComponent<Image>();

            hpSlider.value = 1f;

            _uiElements.Add(new StatusUIElement
            {
                TargetTransform = target,
                Canvas = canvas,
                HpSlider = hpSlider,
                GaugeSlider = gaugeSlider,
                HpFillImage = hpFillImage,
                GaugeFillImage = gaugeFillImage,
                ElementOffset = offset
            });
        }

        /// <summary>
        /// World Space Canvas用のSliderを作る。
        /// </summary>
        /// <param name="name">GameObject名</param>
        /// <param name="parent">親RectTransform</param>
        /// <param name="anchoredPosition">配置位置</param>
        /// <param name="fillColor">Fill色</param>
        /// <returns>生成したSlider</returns>
        private static Slider CreateSlider(string name, RectTransform parent, Vector2 anchoredPosition, Color fillColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(200f, 14f);
            rootRect.anchoredPosition = anchoredPosition;

            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private void OnHealthChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            foreach (var element in _uiElements)
            {
                if (element.HpSlider != null) element.HpSlider.value = ratio;
                if (element.HpFillImage != null)
                {
                    element.HpFillImage.color = ratio > 0.5f
                        ? Color.Lerp(new Color(0.95f, 0.75f, 0.1f), new Color(0.1f, 0.85f, 0.25f), (ratio - 0.5f) * 2f)
                        : Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.95f, 0.75f, 0.1f), ratio * 2f);
                }
            }
        }

        private void OnGaugeChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            Debug.Log($"[EnemyWorldStatusView] OnGaugeChanged! current={current}, max={max}, ratio={ratio}");
            foreach (var element in _uiElements)
            {
                if (element.GaugeSlider != null) 
                {
                    element.GaugeSlider.value = ratio;
                    Debug.Log($"[EnemyWorldStatusView] ゲージスライダーの値を更新しました: {ratio}");
                }
            }
        }

        private void OnDownStarted()
        {
            foreach (var element in _uiElements)
            {
                if (element.GaugeFillImage != null)
                {
                    element.GaugeFillImage.color = new Color(1f, 0.25f, 0.1f, 1f);
                }
            }
        }

        private void OnDefeated()
        {
            foreach (var element in _uiElements)
            {
                if (element.Canvas != null)
                {
                    element.Canvas.gameObject.SetActive(false);
                }
            }
        }
    }
}
