using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewerManager : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] protected string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
    [SerializeField] protected Camera modelCam;
    [SerializeField] protected Transform modelPivot, followPivot, onOffPivot;
    [SerializeField] protected RectTransform rectGame, rectCam;
    [SerializeField] protected Image img_similar;
    [SerializeField] protected Text txt_similar;
    [SerializeField] protected Color[] dancerColor;

    [SerializeField] private GameObject prefab_modelBtn, prefab_modelTgg;
    //public string dataString;

    #region _PROTECTED_VARIABLES
    protected RawImage camBackground;
    protected WebCamTexture webcamTexture;

    protected PoseNet poseNet;
    protected PrimitiveDraw draw;
    protected UniTask<bool> task;
    protected PoseNet.Result[] results;
    protected CancellationToken cancellationToken;
    protected Vector3[] corners = new Vector3[4];
    protected List<float> pointSimilarityValues;

    protected Vector3 firstPoint, secondPoint;
    protected float yAngle, yAngleTemp, yPos, yPosTemp, camRectScale, totalScore;
    protected float threshold = 0.3f, lineThickness = 0.3f, bound = 2f;
    protected bool isTouchUI, isCamRunning, isFlip;
    protected const float scaleSpeed = 0.005f, panSpeed = 0.06f, posSpeed = 0.001f, maxScale = 7f, minScale = 1.5f;
    #endregion



    protected void Awake()
    {
        //dataString = "";
        InitCamPanel();
        SetModelButton();
    }

    protected virtual void OnDestroy()
    {
        webcamTexture?.Stop();
        poseNet?.Dispose();
        draw?.Dispose();
    }

    protected virtual void Update()
    {
#if UNITY_EDITOR
        if (isTouchUI = EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0))
        {
            ModelRotation();
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            if (isTouchUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            switch (Input.touchCount)
            {
                case 1: ModelRotation(); break;
                case 2: ModelZoom(); break;
                default: break;
            }
        }
#endif

        if (isCamRunning)
        {
            //if (Data.GetTimeline().playableDir == null || Data.GetTimeline().playableDir.playableGraph.GetRootPlayable(0).GetSpeed() == 0) return;
            if (Data.GetPlayer().isDrag) return;

            if (task.Status.IsCompleted())
            {
                task = InvokeAsync();
            }

            if (results != null)
            {
                DrawResult();
            }
        }
    }


    #region _SCREEN_LAYER
    protected virtual void InitCamPanel()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        poseNet = new PoseNet(path);

        modelPivot = PlayDirManager.instance.EnableModel(true);

        camBackground = rectCam.GetComponentInChildren<RawImage>();
        transform.localPosition = Vector3.zero;

        camRectScale = 640f;
        totalScore = 0;
        yAngleTemp = 0;
        yPosTemp = 0;
        yPos = 0;

        isCamRunning = false;
        isTouchUI = false;
        isFlip = false;

        rectCam.GetComponentInChildren<AspectRatioFitter>().aspectRatio = (float)(Screen.width * 0.5f) / Screen.height;
        if (!DanceDataManager.instance.isAR)
        {
            rectGame.GetComponentInChildren<AspectRatioFitter>().aspectRatio = (float)Screen.width / Screen.height;
            rectGame.sizeDelta = new Vector2(camRectScale, 0);
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            ToastManager.instance.RunToast("No Camera");
            return;
        }

        int isFrontIndex = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                isFrontIndex = i;
                break;
            }
        }

        webcamTexture = new WebCamTexture(devices[isFrontIndex].name, 640, 480, 30);

        if (webcamTexture == null)
        {
            ToastManager.instance.RunToast("Unable to find camera.");
        }
    }

    public void RunTracking(bool on)
    {
        isCamRunning = on;
    }

    public void CamPanelOn(bool on)
    {
        bool isAR = DanceDataManager.instance.isAR;

        if (!isAR)
            rectGame.sizeDelta = on ? Vector2.zero : new Vector2(camRectScale, 0);

        if (on)
        {
            lineThickness = isAR ? 0.002f : 0.3f;
            bound = isAR ? 0.02f : 2f;

            rectCam.gameObject.SetActive(true);
            PlayWebCam();
        }
        else
        {
            isCamRunning = false;
            webcamTexture?.Stop();
            rectCam.gameObject.SetActive(false);
        }

        SetSimilarIcon(on);
    }

    protected void PlayWebCam()
    {
        webcamTexture.Play();
        camBackground.texture = webcamTexture;

        //Color color = new Color(100, 181, 246, 255);
        Color color = Color.yellow;

        draw = new PrimitiveDraw()
        {
            color = color,
        };
        cancellationToken = this.GetCancellationTokenOnDestroy();

        isCamRunning = true;
    }

    public bool FlipModelRender()
    {
        isFlip = !isFlip;
        rectGame.GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(isFlip ? -1 : 1, 1, 1);

        return isFlip;
    }

    public void SetSimilarIcon(bool on)
    {
        img_similar.gameObject.SetActive(on);
    }
    #endregion


    #region _BODYTRACKING
    protected void DrawResult()
    {
        var rect = camBackground.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        if (pointSimilarityValues == null)
        {
            pointSimilarityValues = new List<float>();
        }
        else
        {
            pointSimilarityValues.Clear();
        }

        var connections = PoseNet.Connections;
        int len = connections.GetLength(0);
        for (int i = 0; i < len; i++)
        {
            var a = results[(int)connections[i, 0]];
            var b = results[(int)connections[i, 1]];
            if (a.confidence >= threshold && b.confidence >= threshold)
            {
                if (i == 2)
                {
                    draw.Point(MathTF.Lerp(min, max, new Vector3(a.x, 1f - a.y, 0)), bound);
                }
                else if (i > 3)
                {
                    draw.Point(MathTF.Lerp(min, max, new Vector3(a.x, 1f - a.y, 0)), bound);
                    draw.Point(MathTF.Lerp(min, max, new Vector3(b.x, 1f - b.y, 0)), bound);
                    draw.Line3D(
                        MathTF.Lerp(min, max, new Vector3(a.x, 1f - a.y, 0)),
                        MathTF.Lerp(min, max, new Vector3(b.x, 1f - b.y, 0)),
                        lineThickness
                    );

                    if (i < len - 2)
                    {
                        CalResult(a, b, i);
                    }
                }
            }
        }
        draw.Apply();

        float sumPoints = 0;
        for (int i = 0; i < pointSimilarityValues.Count; i++)
        {
            sumPoints += pointSimilarityValues[i];
        }
        float result = pointSimilarityValues.Count == 0 ? 0 : sumPoints / pointSimilarityValues.Count;
        //print(sumPoints + " / " + pointSimilarityValues.Count + " = " + result + "\n>> " + totalScore);

        img_similar.fillAmount = result;
        int score = Mathf.CeilToInt(result * 100);
        txt_similar.text = score.ToString("00");

        SumScore(result);
    }

    private void CalResult(PoseNet.Result a, PoseNet.Result b, int i)
    {
        Vector3 va = new Vector3(a.x, a.y, 0);
        Vector3 vb = new Vector3(b.x, b.y, 0);
        Vector3 value = va - vb;

        ModelDataManager mdm = modelData[curModelIndex == modelData.Length - 1 ? 0 : curModelIndex];
        if (mdm == null)
            return;

        Vector3 sendVector = Vector3.zero;
        string msg = string.Empty;
        switch (i)
        {
            case 4:
                sendVector = mdm.GetShoulder();
                msg = mdm.GetShoulder().ToString("F2");
                break;
            case 5:
                sendVector = mdm.GetUpperLeftArm();
                msg = mdm.GetUpperLeftArm().ToString("F2");
                break;
            case 6:
                sendVector = mdm.GetLowerLeftArm();
                msg = mdm.GetLowerLeftArm().ToString("F2");
                break;
            case 7:
                sendVector = mdm.GetUpperRightArm();
                msg = mdm.GetUpperRightArm().ToString("F2");
                break;
            case 8:
                sendVector = mdm.GetLowerRightArm();
                msg = mdm.GetLowerRightArm().ToString("F2");
                break;
            case 9:
                sendVector = mdm.GetHip();
                msg = mdm.GetHip().ToString("F2");
                break;
            case 10:
                sendVector = mdm.GetUpperLeftLeg();
                msg = mdm.GetUpperLeftLeg().ToString("F2");
                break;
            case 11:
                sendVector = mdm.GetLowerLeftLeg();
                msg = mdm.GetLowerLeftLeg().ToString("F2");
                break;
            case 12:
                sendVector = mdm.GetUpperRightLeg();
                msg = mdm.GetUpperRightLeg().ToString("F2");
                break;
            case 13:
                sendVector = mdm.GetLowserRightLeg();
                msg = mdm.GetLowserRightLeg().ToString("F2");
                break;
        }

        //dataString += i + "," + value.ToString("F2") + "," + msg + ",";

        pointSimilarityValues.Add(CosineSimilarity(value, sendVector));
        //print(i + ">> a-b: " + value.ToString("F2") + " / " + msg);
    }

    private float CosineSimilarity(Vector3 v1, Vector3 v2)
    {
        v1.Normalize();
        v2.Normalize();

        //print(v1.ToString("F2") + "  / 22 /   " + v2.ToString("F2"));

        float returnValue = Vector3.Dot(v1, v2);
        returnValue = (float)Math.Round(returnValue, 2);

        //dataString += v1 + "," + v2 + "," + returnValue + "\n";
        //print("   value : " + returnValue);

        return returnValue <= 0 ? 0 : returnValue;
    }

    protected async UniTask<bool> InvokeAsync()
    {
        results = await poseNet.InvokeAsync(webcamTexture, cancellationToken);
        camBackground.material = poseNet.transformMat;
        return true;
    }
    #endregion


    #region _TOTAL_SCORE
    private void SumScore(float result)
    {
        if (!Data.GetPlayer().isCollectScore) return;

        float score = result * 100f;

        if (pointSimilarityValues.Count > 2)
        {
            totalScore = totalScore == 0 ? score : (totalScore + score) * 0.5f;
            //print("  ///  " + totalScore);
        }
    }

    public string GetTotalScore()
    {
        return totalScore.ToString("00");
    }

    public void ResetTotalScore()
    {
        totalScore = 0;
    }
    #endregion


    #region _MODEL_CONTROLL
    private int curModelIndex = 0;
    private ModelDataManager[] modelData;
    private List<Button> choiceBtns;
    private IEnumerator realTimeCameraPos;
    private bool isSyncCam = false;

    private void SetModelButton()
    {
        modelData = modelPivot.GetComponentsInChildren<ModelDataManager>();
        choiceBtns = new List<Button>();

        if (DanceDataManager.instance.isAR)
        {
            modelData[0].EffectOn(true);
        }
        else
        {
            for (int i = 0; i < modelData.Length + 1; i++)
            {
                bool isNotOutIndex = i < modelData.Length;

                Button btn = Instantiate(prefab_modelBtn, followPivot, false).GetComponent<Button>();
                btn.name = isNotOutIndex ? i.ToString() : "X";
                Text txtBtn = btn.GetComponentInChildren<Text>();
                txtBtn.text = isNotOutIndex ? (i + 1).ToString() : "C";
                txtBtn.color = dancerColor[i];
                btn.onClick.AddListener(() => OnClickModelSelect(btn));
                choiceBtns.Add(btn);

                if (isNotOutIndex)
                {
                    Toggle tgg = Instantiate(prefab_modelTgg, onOffPivot, false).GetComponent<Toggle>();
                    tgg.name = i.ToString();
                    Text txtTgg = tgg.GetComponentInChildren<Text>();
                    txtTgg.text = (i + 1).ToString();
                    txtTgg.color = dancerColor[i];
                    tgg.isOn = true;
                    tgg.onValueChanged.AddListener(delegate
                    {
                        OnToggleModelSelect(tgg);
                    });

                    modelData[i].EffectOn(curModelIndex.Equals(i));
                }
            }
        }
    }

    private void OnToggleModelSelect(Toggle tgg)
    {
        int dancerNum = int.Parse(tgg.name);
        modelData[dancerNum].gameObject.SetActive(tgg.isOn);
        choiceBtns[dancerNum].interactable = tgg.isOn;

        if (!tgg.isOn)
        {
            if (dancerNum.Equals(curModelIndex))
            {
                modelData[dancerNum].EffectOn(false);
                for (int i = 0; i < modelData.Length; i++)
                {
                    if (modelData[i].gameObject.activeSelf && !modelData[i].ReturnActiveEffect())
                    {
                        modelData[i].EffectOn(true);
                        curModelIndex = i;
                        break;
                    }
                }
            }
        }
        else
        {
            bool isActive = false;
            for (int i = 0; i < modelData.Length; i++)
            {
                if (modelData[i].ReturnActiveEffect())
                {
                    isActive = true;
                    break;
                }
                else
                    isActive = false;
            }

            if (!isActive)
            {
                modelData[dancerNum].EffectOn(true);
                curModelIndex = dancerNum;
            }
        }

    }

    private void OnClickModelSelect(Button btn)
    {
        if (btn.name.Equals("X"))
        {
            isSyncCam = false;

            if (realTimeCameraPos != null)
            {
                StopCoroutine(realTimeCameraPos);
            }

            transform.localPosition = Vector3.zero;
        }
        else
        {
            curModelIndex = int.Parse(btn.name);
            isSyncCam = true;

            if (realTimeCameraPos == null)
                realTimeCameraPos = SyncCameraPos();

            StartCoroutine(realTimeCameraPos);
        }

        SetEffect();
    }

    private void SetEffect()
    {
        for (int i = 0; i < modelData.Length; i++)
        {
            modelData[i].EffectOn(curModelIndex.Equals(i));
        }
    }

    private IEnumerator SyncCameraPos()
    {
        WaitForEndOfFrame wi = new WaitForEndOfFrame();

        while (isSyncCam)
        {
            Vector3 newPos = modelData[curModelIndex].pivot.position;
            transform.localPosition = new Vector3(newPos.x, transform.localPosition.y, newPos.z);

            yield return wi;
        }
        yield return null;
    }
    #endregion


    #region _TOUCH_CONTROLL
    protected void ModelRotation()
    {
        if (isTouchUI || Data.GetPlayer().isDrag) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            firstPoint = Input.mousePosition;
            yAngleTemp = yAngle;
            yPosTemp = yPos;
        }

        if (Input.GetMouseButton(0))
        {
            secondPoint = Input.mousePosition;
            ChangeRotationCamera();
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            firstPoint = Input.GetTouch(0).position;
            yAngleTemp = yAngle;
            yPosTemp = yPos;
        }

        if (Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            secondPoint = Input.GetTouch(0).position;
            ChangeRotationCamera();
        }
#endif
    }

    protected void ChangeRotationCamera()
    {
        if (DanceDataManager.instance.isAR)
        {
            yPos = yPosTemp + ((secondPoint.y - firstPoint.y) * posSpeed);
            //yAngle = yAngleTemp + ((secondPoint.x - firstPoint.x) * posSpeed);
            //float changePosZ = Mathf.Clamp(modelPivot.localPosition.z + yAngle, -10f, 1.5f);
            modelPivot.localPosition = new Vector3(modelPivot.localPosition.x, Mathf.Clamp(yPos, -5f, 1.5f), modelPivot.localPosition.z);
        }
        else
        {
            yAngle = isFlip ? yAngleTemp + ((firstPoint.x - secondPoint.x) * panSpeed) : yAngleTemp + ((secondPoint.x - firstPoint.x) * panSpeed);
            yPos = yPosTemp + ((firstPoint.y - secondPoint.y) * posSpeed);

            transform.localEulerAngles = new Vector3(0, yAngle, 0.0f);
            transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Clamp(yPos, -0.9f, 1.5f), transform.localPosition.z);
        }
    }

    protected void ModelZoom()
    {
        Touch firstTouch = Input.GetTouch(0);
        Touch secondTouch = Input.GetTouch(1);

        Vector2 firstPreviousPosition = firstTouch.position - firstTouch.deltaPosition;
        Vector2 secondPreviousPosition = secondTouch.position - secondTouch.deltaPosition;

        float previousPositionDistance = (firstPreviousPosition - secondPreviousPosition).magnitude;
        float currentPositionDistance = (firstTouch.position - secondTouch.position).magnitude;

        float scaleValue = (firstTouch.deltaPosition - secondTouch.deltaPosition).magnitude * scaleSpeed;

        Transform moveCam = DanceDataManager.instance.isAR ? modelPivot.transform : modelCam.transform;

        if (previousPositionDistance < currentPositionDistance)
            moveCam.localPosition -= new Vector3(0, 0, scaleValue);
        else if (previousPositionDistance > currentPositionDistance)
            moveCam.localPosition += new Vector3(0, 0, scaleValue);

        if (moveCam.localPosition.z >= maxScale)
            moveCam.localPosition = new Vector3(moveCam.localPosition.x, moveCam.localPosition.y, maxScale);
        else if (moveCam.localPosition.z <= minScale)
            moveCam.localPosition = new Vector3(moveCam.localPosition.x, moveCam.localPosition.y, minScale);
    }
    #endregion
}
