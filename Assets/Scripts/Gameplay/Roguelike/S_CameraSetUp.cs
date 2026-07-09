using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering.Universal;


public class S_CameraSetUp : MonoBehaviour
{
    [Header("使用するCanvas")]
    [SerializeField] private Canvas _frontCanvas;
    [SerializeField] private Canvas _backCanvas;

    [Header("生成するカメラの名前")]
    [SerializeField] private string _backUILayerName = "BackUI";
    [SerializeField] private string _middleLayerName = "MiddleObject";
    [SerializeField] private string _frontUILayerName  = "UI";

    private Camera _backUICam;
    private Camera _middleCam;
    private Camera _frontUICam;
    private Camera _mainCam;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject mainCamObj = GameObject.Find("CameraRoot");

        if(mainCamObj == null)
        {
            Debug.LogError("[Roguelike::S_CameraSetUp] CameraRootが見つかりません");
            return;
        }
        _mainCam = mainCamObj.GetComponent<Camera>();

        _backUICam = CreateOverlayCamera("Cam_BackUI", _backUILayerName);
        _middleCam = CreateOverlayCamera("Cam_Middle", _middleLayerName);
        _frontUICam = CreateOverlayCamera("Cam_FrontUI", _frontUILayerName);


        var baseCamData = _mainCam.GetUniversalAdditionalCameraData();
//        baseCamData.cameraStack.Clear();
        baseCamData.cameraStack.Add(_backUICam);
        baseCamData.cameraStack.Add(_middleCam);
        baseCamData.cameraStack.Add(_frontUICam);

        // Canvasに設定
        AssignCameraToCanvas(_frontCanvas, _frontUICam);
        AssignCameraToCanvas(_backCanvas, _backUICam);

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 camPos = _mainCam.gameObject.transform.position;
        Quaternion camRot = _mainCam.gameObject.transform.rotation;

        _backUICam.transform.position = camPos;
        _backUICam.transform.rotation = camRot;

        _middleCam.transform.position = camPos;
        _middleCam.transform.rotation = camRot;

        _frontUICam.transform.position = camPos;
        _frontUICam.transform.rotation = camRot;

    }

    public void SceneEnd()
    {
        Destroy(_backUICam.gameObject);
        Destroy(_middleCam.gameObject);
        Destroy(_frontUICam.gameObject);
    }

    private void AssignCameraToCanvas(Canvas canvas, Camera targetCam)
    {
        if(canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        canvas.worldCamera = targetCam;
    }

    private Camera CreateOverlayCamera(string objName, string layerName)
    {
        // GameObject生成
        GameObject camObj = new GameObject(objName);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;

        // Cameraコンポーネント追加
        Camera cam = camObj.AddComponent<Camera>();

        // URPのOverlay用データを追加
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderType = CameraRenderType.Overlay;

        // CullingMaskをレイヤー名から設定
        int layer = LayerMask.NameToLayer(layerName);
        if(layer == 1)
        {
            Debug.LogError($"[S_CameraSetUp] Layer '{layerName}' が見つかりません");
        }
        else
        {
            cam.cullingMask = 1 << layer;
        }

        cam.clearFlags = CameraClearFlags.Nothing;
        cam.fieldOfView = 45;

        return cam;
    }

}
