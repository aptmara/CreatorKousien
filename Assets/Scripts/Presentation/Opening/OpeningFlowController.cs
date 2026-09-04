// ------------------------------------------------------------
// File		: OpeningFlowController.cs
// Summary	: オープニングフローを制御するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using Game.Infrastructure.Loading;
using Game.WaveSystem;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;



namespace Game.Presentation.Opening
{
    [DisallowMultipleComponent]
    public sealed class OpeningFlowController : MonoBehaviour
    {
        [Header("--- 遷移設定 ---")]
        [Tooltip("ローディング画面のシーン名")]
        [SerializeField] private string _loadingSceneName = "Loading";

        [Tooltip("オープニング後に起動するBootシーン。チュートリアルへ入るならTutorialBoot")]
        [SerializeField] private string _bootSceneName = "TutorialBoot";

        [Tooltip("起動するステージのデータ")]
        [SerializeField] private StageDataSO _stageData;

        [Header("--- 仮実装: 本編はいるまでの待機時間(秒) ---")]
        [SerializeField, Min(0f)] private float _placeholderDuration = 3f;


        [Header("--- シナリオ ---")]
        [SerializeField] private SO_OpeningScenario _scenario;
        [SerializeField] private OpeningTextPresenter _textPresenter;


        [Header("--- 入力 ---")]
        [Tooltip("決定。文字送り中は全文表示")]
        [SerializeField] private InputAction _submitAction;


        [Header("--- スライド演出 ---")]
        [Tooltip("シナリオのスライドと同じ順番・同じ数で並べる")]
        [SerializeField] private OpeningSlideView[] _slideViews;


        [Header("--- 開始待ち ---")]
        [Tooltip("シーンに入ってから紙芝居が始まるまでの待ち時間(秒)")]
        [SerializeField, Min(0f)] private float _startDelay = 0.8f;


        private bool _transitionStarted;


        private void OnEnable()
        {
            _submitAction.Enable();
        }


        private void OnDisable()
        {
            _submitAction.Disable();
        }


        private void Awake()
        {
            // 開始待ちの間に絵が出ていないよう、最初のフレームで隠しておく
            HideAllSlides();
        }


        private IEnumerator Start()
        {
            // ポーズ中にタイトルへ戻った場合でも止まらないようにする
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_scenario != null && !string.IsNullOrEmpty(_scenario.BgmName) && SoundManager.instance != null)
            {
                SoundManager.instance.PlayBGM(_scenario.BgmName);
            }

            // 開始待ち
            yield return new WaitForSecondsRealtime(_startDelay);

            yield return PlayScenarioRoutine();

            yield return EnterLoadingRoutine();
        }


        /// <summary>
        /// シナリオを再生するコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayScenarioRoutine()
        {
            if (_scenario == null || _textPresenter == null)
            {
                Debug.LogError("[Opening] シナリオまたはテキストプレゼンターが設定されていません。");
                yield return new WaitForSecondsRealtime(_placeholderDuration);
                yield break;
            }

            HideAllSlides();

            for (int slideIndex = 0; slideIndex < _scenario.Slides.Count; slideIndex++)
            {
                OpeningSlideScript slide = _scenario.Slides[slideIndex];
                OpeningSlideView view = GetSlideView(slideIndex);

                // 絵を出す
                if (view != null)
                {
                    view.gameObject.SetActive(true);
                    yield return view.PlayEnterRoutine();
                }

                // 座布団は最初のスライドの絵が出そろってから出し、以降は出しっぱなし
                if (slideIndex == 0)
                {
                    yield return _textPresenter.ShowPlateRoutine();
                }

                for (int lineIndex = 0; lineIndex < slide.Lines.Count; lineIndex++)
                {
                    yield return _textPresenter.ShowLineRoutine(slide.Lines[lineIndex], slide, WasSubmitPressed);
                }

                // 絵をひっこめる！
                if (view != null)
                {
                    yield return view.PlayExitRoutine();
                    view.gameObject.SetActive(false);
                }
            }

            yield return _textPresenter.HidePlateRoutine();
        }


        private void HideAllSlides()
        {
            if (_slideViews == null)
            {
                return;
            }

            for (int i = 0; i < _slideViews.Length; i++)
            {
                if (_slideViews[i] != null)
                {
                    _slideViews[i].gameObject.SetActive(false);
                }
            }
        }


        private OpeningSlideView GetSlideView(int slideIndex)
        {
            if (_slideViews == null || slideIndex < 0 || slideIndex >= _slideViews.Length)
            {
                return null;
            }

            return _slideViews[slideIndex];
        }


        private bool WasSubmitPressed()
        {
            return _submitAction != null && _submitAction.WasPressedThisFrame();
        }


        private IEnumerator EnterLoadingRoutine()
        {
            if (_transitionStarted)
            {
                yield break;
            }
            _transitionStarted = true;

            if (_stageData == null)
            {
                Debug.LogError("[Opening] StageDataSOが設定されていません。");
                yield break;
            }

            AsyncOperation loadingLoad = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);

            if (loadingLoad == null)
            {
                Debug.LogError($"[Opening] ローディングシーンの読み込みに失敗しました。シーン名: {_loadingSceneName}");
                yield break;
            }

            yield return loadingLoad;

            LoadingFlowController loadingFlow = Object.FindFirstObjectByType<LoadingFlowController>();
            if (loadingFlow == null)
            {
                Debug.LogError("[Opening] LoadingFlowControllerが見つかりません。");
                yield break;
            }

            loadingFlow.LoadBootScene(_bootSceneName, _stageData);

            SceneManager.UnloadSceneAsync(gameObject.scene);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_slideViews == null || _scenario == null)
            {
                return;
            }

            if (_slideViews.Length != _scenario.Slides.Count)
            {
                Debug.LogWarning($"[Opening] スライド演出の数({_slideViews.Length})とシナリオのスライド数({_scenario.Slides.Count})が一致していません。", this);
            }
        }
#endif
    }
}


