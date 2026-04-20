// ------------------------------------------------------------
// File		: UIManager.cs
// Summary	: UIの管理を行うクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - Namespaceおよびリファクタリングします。
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;


namespace CreatorKousien.View.UI
{
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// UIのレイヤーを定義するクラス
        /// </summary>
        [System.Serializable]
        private class ViewEntry
        {
            public ViewType viewType;
            [Tooltip("シーン上のオブジェクトではなく、プレファブをセット！")]
            public GameObject Prefab; // プレファブから生成するように変更 (4/20)
        }

        /// <summary>
        /// UIの状態と、その状態でアクティブにするViewTypeのリストを定義するクラス
        /// </summary>
        [System.Serializable]
        private class StateDefinition
        {
            public UIState state;
            public List<ViewType> ActiveViews = new List<ViewType>();
        }

        [SerializeField] private List<ViewEntry> viewEntries = new List<ViewEntry>();
        [SerializeField] private List<StateDefinition> stateDefinitions = new List<StateDefinition>();

        private readonly Dictionary<ViewType, GameObject> _viewMap = new Dictionary<ViewType, GameObject>();
        private readonly Dictionary<UIState, List<ViewType>> _stateMap = new Dictionary<UIState, List<ViewType>>();

        private UIState _currentState = UIState.None;

        /// <summary>
        /// GameManagerから呼ばれる初期化処理。ここでUIを一気に生成する！
        /// </summary>
        public void Initialize()
        {
            BuildAndInstantiateViews();
            BuildStateMap();

            // 初期状態は InGame にする
            ChangeState(UIState.InGame);
        }

        /// <summary>
        /// viewEntriesのプレファブから実際のUIオブジェクトを生成し、viewMapに登録する。
        /// </summary>
        private void BuildAndInstantiateViews()
        {
            _viewMap.Clear();
            foreach (ViewEntry entry in viewEntries)
            {
                if (entry == null || entry.Prefab == null || _viewMap.ContainsKey(entry.viewType))
                {
                    continue;
                }

                // UIManager(Canvas)の子オブジェクトとしてプレファブを生成！
                GameObject instance = Instantiate(entry.Prefab, this.transform);
                instance.SetActive(false); // 一旦すべて非表示
                _viewMap.Add(entry.viewType, instance);
            }
        }


        /// <summary>
        /// stateDefinitionsからstateMapを構築する
        /// </summary>
        private void BuildStateMap()
        {
            _stateMap.Clear();
            foreach (StateDefinition def in stateDefinitions)
            {
                if (def != null && !_stateMap.ContainsKey(def.state))
                {
                    _stateMap.Add(def.state, def.ActiveViews);
                }
            }
        }


        /// <summary>
        /// UIの状態を切り替えるメソッド
        /// </summary>
        /// <param name="newState"></param>
        public void ChangeState(UIState newState)
        {
            _currentState = newState;

            // 一旦すべて消す
            foreach (var pair in _viewMap)
            {
                if (pair.Value != null) pair.Value.SetActive(false);
            }

            // 必要なものだけ表示する
            if (_stateMap.TryGetValue(newState, out List<ViewType> activeViews) && activeViews != null)
            {
                foreach (ViewType type in activeViews)
                {
                    OpenView(type);
                }
            }
        }


        /// <summary>
        /// 現在のUIの状態を取得するメソッド
        /// </summary>
        /// <returns>現在のUIの状態</returns>
        public UIState GetCurrentState() => _currentState;


        /// <summary>
        /// 現在のUIの状態がInGameかどうかを判定するメソッド。InGameのときだけ、手入力を許可するために使う。
        /// </summary>
        /// <returns>現在のUIの状態がInGameであればtrue、それ以外はfalse</returns>
        public bool IsHandInputAllowed() => _currentState == UIState.InGame;


        /// <summary>
        /// 特定のViewTypeを表示するメソッド。UIの状態とは関係なく、個別に表示したいときに使う。
        /// </summary>
        /// <param name="type">表示したいViewType</param>
        public void OpenView(ViewType type)
        {
            if (_viewMap.TryGetValue(type, out GameObject obj) && obj != null) obj.SetActive(true);
        }


        /// <summary>
        /// 特定のViewTypeを非表示にするメソッド。UIの状態とは関係なく、個別に非表示にしたいときに使う。
        /// </summary>
        /// <param name="type">非表示にしたいViewType</param>
        public void CloseView(ViewType type)
        {
            if (_viewMap.TryGetValue(type, out GameObject obj) && obj != null) obj.SetActive(false);
        }

        /// <summary>
        /// 生成されたUIの中から、特定のコンポーネント(HpGaugeViewなど)を取得する！
        /// </summary>
        /// <param name="type">取得したいViewType</param>
        /// <returns>取得したコンポーネント</returns>
        public T GetView<T>(ViewType type) where T : Component
        {
            if (_viewMap.TryGetValue(type, out GameObject obj) && obj != null)
            {
                return obj.GetComponentInChildren<T>(true);
            }
            return null;
        }
    }
}


