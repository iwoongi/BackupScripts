using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    #region _VARIABLES
    public bool isDrag, isCollectScore;

    [Header("UI_player")]
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject controllPanel, speedPanel, fadeOutPanel, scorePanel;
    [SerializeField] private Image btnRepeat, btnPlay, btnMirror;
    [SerializeField] private Text txt_speed, txt_repeatMode, txt_Time;
    [SerializeField] private TMP_Text txt_Title;
    [SerializeField] private Sprite[] repeatImage, playImage, mirrorImage;
    [SerializeField] private Sprite sliderImage;
    [SerializeField] private Button btnPlayFirst;
    [SerializeField] private AudioSource audioSource;

    private PlayableDirector playableDir;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    private const float TimeJumpValue = 5f;
    private TimelineAsset timeline;
    private Button[] playBtns, speedBtns;
    private bool isPlay;

    private enum PlayState { play, pause }
    private PlayState curPlayState;       //현재 재생 상태
    private enum RepeatMode { none, all }
    private RepeatMode curRepeatMode;     //현재 반복 모드

    [Header("UI_Score")]
    [SerializeField] private TMP_Text scoreP_Title;
    [SerializeField] private Text scoreP_Score, scoreP_Time;
    private Button[] scoreBtns;
    #endregion

    private void Start()
    {
        Init();
    }

    private void OnDisable()
    {
        for (int i = 0; i < playBtns.Length; i++)
        {
            Button btn = playBtns[i];
            btn.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < scoreBtns.Length; i++)
        {
            Button btn = scoreBtns[i];
            btn.onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        if (isPlay)
        {
            float time = (float)playableDir.time;

            if (!isDrag)
            {
                if (curRepeatState.Equals(RepeatState.ing) && playableDir.time >= repeatEndTime)
                {
                    PlayRepeat();
                }

                slider.value = time;
            }

            txt_Time.text = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
        }
    }

    private void Init()
    {
        isPlay = true;
        isDrag = false;
        isCollectScore = false;
        speedValue = 1;
        txt_speed.text = speedValue.ToString("0.0");

        curPlayState = PlayState.pause;
        curRepeatState = RepeatState.exit;
        curRepeatMode = RepeatMode.none;
        txt_repeatMode.text = "X";

        playableDir = PlayDirManager.instance.playableDir;

        playBtns = controllPanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < playBtns.Length; i++)
        {
            Button btn = playBtns[i];
            btn.onClick.AddListener(() => OnClick(btn));

            if (btn.name.Equals("btn_mirror"))
            {
                btn.gameObject.SetActive(!DanceDataManager.instance.isAR);
            }
            if (btn.name.Equals("btn_moveModel"))
            {
                btn.gameObject.SetActive(DanceDataManager.instance.isAR);
            }
        }

        scoreBtns = scorePanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < scoreBtns.Length; i++)
        {
            Button btn = scoreBtns[i];
            btn.onClick.AddListener(() => OnClick(btn));
        }

        btnPlayFirst.onClick.AddListener(() => OnClick(btnPlayFirst));
        btnPlayFirst.gameObject.SetActive(true);
        btnPlayFirst.interactable = false;

        if (!DanceDataManager.instance.isAR)
        {
            Data.GetViewer().CamPanelOn(DanceDataManager.instance.isTracking);
        }
        else
        {
            Data.GetViewerAR().CamPanelOn(DanceDataManager.instance.isTracking);
        }

        scorePanel.SetActive(false);
        fadeOutPanel.SetActive(false);

        //Data.GetUI().InitOrientation();
        ReadyToPlay();
    }

    private void ReadyToPlay()
    {
        timeline = DanceDataManager.instance.GetCurMusic().timeline;
        //timeline의 트랙과 오브젝트 binding
        PlayDirManager.instance.BindingObject(timeline);
        playableDir.transform.localEulerAngles = new Vector3(0, DanceDataManager.instance.GetCurMusic().rootRot, 0);

        playableDir.Stop();
        playableDir.Play(timeline);
        playableDir.playableGraph.GetRootPlayable(0).SetSpeed(0);

        slider.maxValue = (float)playableDir.duration;
        repeatEndTime = slider.maxValue;

        txt_Title.text = DanceDataManager.instance.GetCurMusicText();

        btnPlayFirst.interactable = true;
    }

    public void CheckTimelineFinish()
    {
        isPlay = false;

        if (curRepeatState != RepeatState.exit)
        {
            if (curRepeatState.Equals(RepeatState.start))
            {
                curRepeatState = RepeatState.ing;
                btnRepeat.sprite = repeatImage[1];
            }
            playableDir.time = repeatStartTime;
            isPlay = true;

            return;
        }

        switch (curRepeatMode)
        {
            case RepeatMode.none:
                OnClickPlay(false);
                SetScorePanel(true);

                break;
            case RepeatMode.all:
                playableDir.time = 0;
                isPlay = true;

                break;
        }
    }

    private void SetScorePanel(bool on)
    {
        if (on)
        {
            if (curPlayState.Equals(PlayState.play))
                OnClickPlay(false);

            scoreP_Score.text = DanceDataManager.instance.isTracking
               ? DanceDataManager.instance.isAR ? Data.GetViewerAR().GetTotalScore() : Data.GetViewer().GetTotalScore()
               : "Fin.";
            scoreP_Title.text = txt_Title.text;
            scoreP_Time.text = "Play Time  " + (playTime / 60).ToString("00") + " : " + (playTime % 60).ToString("00");

            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }

        if (DanceDataManager.instance.isTracking)
            Data.GetViewer().RunTracking(!on);

        scorePanel.SetActive(on);
    }


    private void OnClick(Button btn)
    {
        switch (btn.name)
        {
            case "btn_PlayFullRect": OnClickFirstPlay(); break;
            case "btn_play": OnClickPlay(curPlayState != PlayState.play); break;
            case "btn_prev": OnClickJumpTime(false); break;
            case "btn_next": OnClickJumpTime(true); break;
            case "btn_sectionRepeat": OnClickRepeat(); break;
            case "btn_mirror":; OnClickModelMirror(); break;
            case "btn_repeat": OnClickRepeatMode(); break;

            case "btn_speed": OnClickSpeedPanel(true); break;
            case "btn_speedUp": OnClickSpeed(true); break;
            case "btn_speedDown": OnClickSpeed(false); break;

            case "btn_back": OnClickBack(); break;
            case "btn_home": OnClickHome(); break;
            case "btn_reTry": OnClickReTry(true); break;
            case "btn_resume": OnClickReTry(false); break;
        }
    }


    #region _ONCLICK_METHODS
    private void OnClickFirstPlay()
    {
        btnPlayFirst.gameObject.SetActive(false);
        OnClickPlay(true);

        ReadyRecordTime();
    }

    private void OnClickPlay(bool on)
    {
        isPlay = false;
        isCollectScore = on;

        if (on)
        {
            if (playableDir.time >= (slider.maxValue - 0.1f) && curPlayState.Equals(PlayState.pause))
            {
                playableDir.time = 0;
            }

            curPlayState = PlayState.play;
            btnPlay.sprite = playImage[1];
        }
        else
        {
            curPlayState = PlayState.pause;
            btnPlay.sprite = playImage[0];
        }
        playableDir.playableGraph.GetRootPlayable(0).SetSpeed(on ? speedValue : 0);

        isPlay = true;
    }

    private void OnClickJumpTime(bool isNext)
    {
        isDrag = true;
        float sumTime = isNext ? slider.value + TimeJumpValue : slider.value - TimeJumpValue;

        if (curRepeatState != RepeatState.exit)
        {
            if (isNext)
            {
                if (curRepeatState.Equals(RepeatState.ing))
                    playableDir.time = sumTime > repeatEndTime ? repeatEndTime : sumTime;
                else
                    playableDir.time = sumTime > slider.maxValue ? slider.maxValue : sumTime;
            }
            else
                playableDir.time = sumTime < repeatStartTime ? repeatStartTime : sumTime;
        }
        else
        {
            if (isNext)
                playableDir.time = sumTime > slider.maxValue ? slider.maxValue : sumTime;
            else
                playableDir.time = sumTime < 0 ? 0 : sumTime;
        }

        isDrag = false;
    }

    private void OnClickRepeatMode()
    {
        switch (curRepeatMode)
        {
            case RepeatMode.none:
                txt_repeatMode.text = "";
                curRepeatMode = RepeatMode.all;

                break;
            case RepeatMode.all:
                txt_repeatMode.text = "X";
                curRepeatMode = RepeatMode.none;

                break;
        }
    }

    private void OnClickModelMirror()
    {
        bool isOn = Data.GetViewer().FlipModelRender();
        btnMirror.sprite = isOn ? mirrorImage[0] : mirrorImage[1];
    }

    private void OnClickReTry(bool isReset)
    {
        SetScorePanel(false);

        if (isReset)
        {
            if (DanceDataManager.instance.isAR)
                Data.GetViewerAR().ResetTotalScore();
            else
                Data.GetViewer().ResetTotalScore();

            playableDir.time = 0;
            ReadyRecordTime();
        }

        OnClickPlay(curPlayState != PlayState.play);
    }

    private void OnClickBack()
    {
        SetScorePanel(true);
    }

    private void OnClickHome()
    {
        if (DanceDataManager.instance.isTracking)
        {
            string score = DanceDataManager.instance.isAR
                ? Data.GetViewerAR().GetTotalScore()
                : Data.GetViewer().GetTotalScore();
            string result = score + "/" + DateTime.Now;


            //플레이타임이 1분 이상일 경우 스코어 기록
            if (playTime > 60f && int.Parse(score) > 0)
            {
                DanceDataManager.instance.SetScore(result);
                print("  >> " + result);
            }
        }

        StartCoroutine(LoadFrontScene());
    }

    private IEnumerator LoadFrontScene()
    {
        fadeOutPanel.SetActive(true);
        AsyncOperation op = SceneManager.LoadSceneAsync(2);
        op.allowSceneActivation = false;

        bool isLoading = true;
        while (isLoading)
        {
            if (op.progress >= 0.9f)
                isLoading = false;

            yield return waitForEndOfFrame;
        }

        yield return new WaitForSeconds(1f);

        op.allowSceneActivation = true;
    }
    #endregion


    #region _SECTION_REPEAT
    private List<GameObject> repeat_sections;   //구간반복 ui표시
    private float repeatStartTime, repeatEndTime;
    private RepeatState curRepeatState;       //구간반복 상태
    private enum RepeatState { start, ing, exit }

    private void OnClickRepeat()
    {
        int imgNum = 0;

        if (repeat_sections == null)
        {
            repeat_sections = new List<GameObject>();
        }

        switch (curRepeatState)
        {
            case RepeatState.exit:
                curRepeatState = RepeatState.start;
                SetSliderBackground(false);
                imgNum = 0;

                break;
            case RepeatState.start:
                SetSliderBackground(true);

                playableDir.time = repeatStartTime;
                curRepeatState = RepeatState.ing;
                imgNum = 1;

                break;
            case RepeatState.ing:
                imgNum = EndRepeat();

                break;
        }

        btnRepeat.sprite = repeatImage[imgNum];
        OnClickPlay(true);
    }

    private int EndRepeat()
    {
        curRepeatState = RepeatState.exit;

        repeatStartTime = 0;
        repeatEndTime = slider.maxValue;

        for (int i = 0; i < repeat_sections.Count; i++)
        {
            Destroy(repeat_sections[i]);
        }
        repeat_sections.Clear();

        btnRepeat.sprite = repeatImage[2];

        return 2;
    }

    private void SetSliderBackground(bool isEnd)
    {
        OnClickPlay(false);

        GameObject gameBox = new GameObject("cut", typeof(RectTransform), typeof(Image));
        RectTransform rect = gameBox.GetComponent<RectTransform>();
        Image image = gameBox.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.sprite = sliderImage;

        rect.SetParent(slider.transform.GetChild(0), false);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 setAnchorMin, setAnchorMax;
        if (isEnd)
        {
            repeatEndTime = slider.value;
            setAnchorMin = new Vector2(slider.normalizedValue, 0f);
            setAnchorMax = Vector2.one;
            image.color = Color.cyan;
        }
        else
        {
            repeatStartTime = slider.value;
            setAnchorMin = Vector2.zero;
            setAnchorMax = new Vector2(slider.normalizedValue, 1f);
            image.color = Color.cyan;
        }

        rect.anchorMin = setAnchorMin;
        rect.anchorMax = setAnchorMax;
        rect.offsetMin = new Vector2(0f, 0f);
        rect.offsetMax = new Vector2(0f, 0f);

        repeat_sections.Add(gameBox);
    }

    private void PlayRepeat()
    {
        isPlay = false;

        playableDir.time = repeatStartTime;

        if (curRepeatState.Equals(RepeatState.start))
            curRepeatState = RepeatState.ing;

        isPlay = true;
    }
    #endregion


    #region _SLIDER_CONTROLL
    public void OnTouchDown()
    {
        isDrag = true;
    }

    public void OnTouchUp()
    {
        isDrag = false;
    }

    public void OnTouchDrag()
    {
        if (curRepeatState != RepeatState.exit)
        {
            if (slider.value < repeatStartTime)
            {
                slider.value = repeatStartTime;
                playableDir.time = repeatStartTime;
            }
            else if (slider.value > repeatEndTime)
            {
                slider.value = repeatEndTime;
                playableDir.time = repeatEndTime;
            }
        }

        playableDir.time = slider.value;
    }
    #endregion


    #region _PLAY_SPEED
    private float speedValue = 1f;
    private const float maxSpeed = 3f;
    private const float minSpeed = 0.1f;

    private void OnClickSpeed(bool isUp)
    {
        float changeValue;

        if (isUp)
            changeValue = speedValue >= 1 ? 0.5f : 0.1f;
        else
            changeValue = speedValue > 1 ? 0.5f : 0.1f;

        speedValue = Mathf.Clamp(isUp ? speedValue + changeValue : speedValue - changeValue, minSpeed, maxSpeed);

        txt_speed.text = "x" + speedValue.ToString("0.0");
        playableDir.playableGraph.GetRootPlayable(0).SetSpeed(speedValue);
    }

    private void OnClickSpeedPanel(bool on)
    {
        speedPanel.SetActive(on);
    }

    public void SetSpeedValue(float value)
    {
        speedValue = value;
        txt_speed.text = "x" + speedValue.ToString("0.0");
        playableDir.playableGraph.GetRootPlayable(0).SetSpeed(speedValue);

        OnClickSpeedPanel(false);
    }
    #endregion


    #region _RECORD_TIME
    private IEnumerator recordTime;
    private float playTime = 0;

    private void ReadyRecordTime()
    {
        if (recordTime == null)
        {
            recordTime = StartRecordTime();
        }
        playTime = 0;

        StopCoroutine(recordTime);
        StartCoroutine(recordTime);
    }

    private IEnumerator StartRecordTime()
    {
        while (true)
        {
            yield return new WaitWhile(() => isDrag || curPlayState == PlayState.pause);

            playTime += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
    }
    #endregion
}
