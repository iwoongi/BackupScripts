using maxstAR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpeakSelectManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] stepPanel;
    [SerializeField] private StepCursorController[] stepCursorImage;
    [SerializeField] private Text topText;

    [SerializeField] private Button btn_back, btn_exit, btn_next, btn_ar;
    [SerializeField] private Color active, inActive;

    [Header("Step1")]
    [SerializeField] private Image[] storyImage;
    [SerializeField] private Button btn_addPic;

    [Header("Step2")]
    [SerializeField] private GameObject[] step2ChildPanel;
    [SerializeField] private ToggleGroup[] arChoiceGroup;
    [SerializeField] private string curChoice;
    [SerializeField] private int arIndex;


    /// <summary>
    /// (0:Step1)엔딩사진 추가 (1:Step2)AR오브젝트 선택 (2:Step2)AR오브젝트에 따른 디자인 선택 (3:Step2)애니메이션 효과 선택 (4:Step2)얼굴사진 선택 (5:Step3)Assessment
    /// </summary>
    private Stack<int> curPage;
    private const int MAXPAGE = 5;

    private readonly string[] stepText = {"Choose an AR object for your MeMe video.", "Choose the background music for Story Concert.",
        "Choose the MeMe Book design.","Choose the Screen-Toon Layout.","Choose an animation effect for the AR object.",
        "Add your face to see yourself in a funny character animation!" };

    private GameManager GM;
    private DirectionsPanel DP;
    private SubmitManager submitManager;    //결과등록

    private void OnEnable()
    {
        GM = FindObjectOfType<GameManager>();
        DP = FindObjectOfType<DirectionsPanel>();
        submitManager = FindObjectOfType<SubmitManager>();

        curPage = new Stack<int>();
        curPage.Push(0);

        for (int i = 0; i < stepPanel.Length; i++)
        {
            if (stepPanel[i] != null)
                stepPanel[i].SetActive(false);
        }

        btn_back.onClick.AddListener(() => OnClickBack());
        btn_exit.onClick.AddListener(() => OnClickExit());
        btn_next.onClick.AddListener(() => OnClickNext());
        btn_ar.onClick.AddListener(() => OnClickAR());
        btn_addPic.onClick.AddListener(() => OnClickAddPicture());

        DP.CheckEpisodeEndingAndAvatarImage();

        ChangePage(true);
    }


    private void OnDisable()
    {
        curPage.Clear();

        btn_back.onClick.RemoveAllListeners();
        btn_exit.onClick.RemoveAllListeners();
        btn_next.onClick.RemoveAllListeners();
        btn_ar.onClick.RemoveAllListeners();
        btn_addPic.onClick.RemoveAllListeners();
    }

    //Step전환
    private void ChangePage(bool on)
    {
        int page = curPage.Peek();

        if (page > 0 && page < 4 && !on)
            CheckToggleGroup();

        stepPanel[page].SetActive(on);
        StepCursorViewer();

        switch (page)
        {
            case 0:
                for (int i = 0; i < DP.baseEpisodeTexture.Length; i++)
                {
                    storyImage[i].sprite = TextureToSprite((Texture2D)DP.baseEpisodeTexture[i]);
                }
                topText.text = string.Empty;

                break;
            case 1:
                topText.text = stepText[0];
                step2ChildPanel[arIndex].SetActive(false);

                break;
            case 2:
                topText.text = stepText[1];
                step2ChildPanel[arIndex].SetActive(true);

                break;
            case 3:
                topText.text = stepText[4];
                step2ChildPanel[arIndex].SetActive(false);

                btn_next.gameObject.SetActive(true);

                break;
            case 4:
                topText.text = stepText[5];

                // 제출되지 않았을때 처리
                btn_next.gameObject.SetActive(false);

                break;
            case 5:
                topText.text = "평가페이지";

                break;
            default:
                Debug.Log(" page Error");

                break;
        }
    }

    //토글 그룹에서 활성화된 토글 체크
    private void CheckToggleGroup()
    {
        int index = 0;
        for (int i = 0; i < arChoiceGroup.Length; i++)
        {
            if (arChoiceGroup[i].gameObject.activeSelf)
                index = i;
        }

        foreach (Toggle tgg in arChoiceGroup[index].ActiveToggles())
        {
            curChoice = tgg.name;
        }

        string[] splitTggName = curChoice.Split('_');

        switch (splitTggName[0])
        {
            case "ar":
                arIndex = splitTggName[1].Equals("concert") ? 0 : splitTggName[1].Equals("book") ? 1 : 2;
                GM.objType = (GameManager.objectType)arIndex;

                break;
            case "arSub":
                ChoiceARsubObject(splitTggName[1]);

                break;
            case "effect":
                int indexEffect = splitTggName[1].Equals("particle") ? 0 : splitTggName[1].Equals("bounce") ? 1 : 2;
                GM.EffectMode = indexEffect;

                break;
        }
    }

    #region AR_OBJECT_SETTING
    //선택된 AR Object 셋팅 (GameManager에서 관리)
    private void ChoiceARsubObject(string ar)
    {
        int type;
        if (ar.Contains("book"))
        {
            type = int.Parse(ar.Replace("book", string.Empty));
            GM.currentobj = GM.Book[type];
        }
        else if (ar.Contains("bgm"))
        {
            type = int.Parse(ar.Replace("bgm", string.Empty));
            GM.currentobj = GM.Concert;
            GM.currentBGM = GM.MidBGMs[type];
            GM.SelectBGMPlay();
        }
        else
        {
            type = int.Parse(ar.Replace("toon", string.Empty));
            GM.currentobj = GM.Toon[type];
        }
        GM.FrameType = type;
    }
    #endregion

    private void OnClickBack()
    {
        if (curPage.Peek().Equals(0))
        {
            OnClickExit();
        }
        else
        {
            ChangePage(false);
            curPage.Pop();

            ChangePage(true);
        }
    }

    private void OnClickNext()
    {
        if (curPage.Peek().Equals(MAXPAGE))
        {
            print("LastPage");
        }
        else
        {
            ChangePage(false);
            curPage.Push(curPage.Peek() + 1);

            ChangePage(true);
        }
    }

    private void OnClickExit()
    {
        gameObject.SetActive(false);
    }

    private void OnClickAR()
    {
        SubmitData();

        GM.PlayARScene();
    }

    private void OnClickAddPicture()
    {
        DP.OnButtonChangeEpisodeImage();
    }

    //Submit Data 취합
    private void SubmitData()
    {
        submitManager.SpeakingInit();

        SpeakingData sd = new SpeakingData
        {
            sem_id = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_id,
            top_cors_id = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].top_cors_id,
            com_id = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].com_id,
            g_seq = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].g_seq,
            app_ver = Application.version,
            manufacturer = "",  //제조사를 어떻게 아니?
            model = SystemInfo.deviceModel,
            submit_seq = "1",   //임시
            study_time = 240,   //임시
            std_email_to = "",  //모름
            std_capture_image = "",
            std_ending_image = "",
            std_upload_image = "",
            std_video = "http://",
            std_video_thumbnail = "http://",
            std_video_share = 0,     //결과제출에서 사용되는 데이터가 아님. AR비디오 촬영 모두 끝나고 Submit할때 얻을수 있는 데이터
            prompt_eng = "Explain what Race for Survival is. Use the outline to complete your response.",
            prompt_kor = "Race for Survival이 무엇인지 설명하세요. Outline을 사용하여 여러분의 대답을 완성하세요.",
            act_data = null
        };

        submitManager.SetSdata(sd);
        //submitManager.SendData();
    }


    // 현재 스텝 상단표시
    private void StepCursorViewer()
    {
        int index = curPage.Peek();

        if (index >= MAXPAGE)
        {
            index = 2;
        }
        else if (index > 0)
        {
            index = 1;
        }

        GameObject stepParent = stepCursorImage[0].transform.parent.gameObject;
        stepParent.SetActive(false);

        for (int i = 0; i < stepCursorImage.Length; i++)
        {
            stepCursorImage[i].ChangeActive(i.Equals(index), i < index);
        }
        stepParent.SetActive(true);
    }

    //Texture2D를 Sprite로 변환
    private Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null)
            return null;

        Rect rect = new Rect(0, 0, tex.width, tex.height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
    }

}
