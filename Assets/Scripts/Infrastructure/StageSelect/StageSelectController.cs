using Game.Infrastructure.Loading;
using Game.WaveSystem;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;


public class StageSelectController : MonoBehaviour
{
    [SerializeField] private string _titleSceneName = "Title";
    [SerializeField] private string _loadingSceneName = "Loading";



    [SerializeField] private SelectSceneButtonBase[] _stageButtons;
    [SerializeField] private InputAction _cursorInput;
    [SerializeField] private InputAction _clickInput;

    [SerializeField] private float maxKeyWaitTime;

    [SerializeField] private int defaultSelectCount;

    private int select;

    private float keyWaitTime = 0;

    private bool isSelectedStage = false;

    private void OnEnable()
    {
        _cursorInput.Enable();
        _clickInput.Enable();
    }

    private void OnDisable()
    {
        _cursorInput.Disable();
        _clickInput.Disable();
    }

    private void Start()
    {
        select = defaultSelectCount;
        if (_stageButtons.Length > select) _stageButtons[select].OnSelectCursor();
        else _stageButtons[0].OnSelectCursor();
            keyWaitTime = 0;
    }

    void Update()
    {
        if (_stageButtons.Length == 0) return;

        Vector2 axis = _cursorInput.ReadValue<Vector2>();

        if (keyWaitTime > 0)
        {
            keyWaitTime -= Time.deltaTime;
            keyWaitTime = Mathf.Clamp(keyWaitTime, 0, maxKeyWaitTime);
        }
        if (axis.x != 0.0f && keyWaitTime <= 0.0f)
        {
            keyWaitTime = maxKeyWaitTime;

            Debug.Log(axis.x);
            float sign = Mathf.Sign(axis.x);
            select += (int)(1.0f * sign);
            select = Math.Clamp(select, 0, _stageButtons.Length - 1);

            _stageButtons[select].OnSelectCursor();
        }

        if(_clickInput.triggered)
        {
            _stageButtons[select].OnClick();
        }

    }


    public void LoadGameScene(StageDataSO stage)
    {
        if (isSelectedStage) return;
        isSelectedStage = true;

        // ロードシーンを生成
        Debug.Log(stage.StageName + "が選ばれました");
        Debug.Log("Loadを開始 シーン遷移: " + _loadingSceneName);
        StartCoroutine(StageLoad(stage));

    }

    public　void OnBackButtonClicked()
    {
        if (isSelectedStage) return;
        isSelectedStage = true;
        // タイトルシーンへ移行
        Debug.Log("タイトルシーンへ移行します。");
        SceneManager.LoadScene(_titleSceneName);
    }

    private IEnumerator StageLoad(StageDataSO stage)
    {
        // Scemeをロードする
        AsyncOperation bootLoad = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
        yield return bootLoad;

        // 生成が完了次第、ステージデータを渡してロードを起動
        LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
        loadingFlowController.LoadBootScene(stage);
        // 現シーンを削除する
        Scene currentSceneName = gameObject.scene;
        SceneManager.UnloadSceneAsync(currentSceneName);
    }
}
