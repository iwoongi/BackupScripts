using maxstAR;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WritingSelectManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform thisRT;
    [SerializeField] private GameObject[] stepPanel;
    [SerializeField] private StepCursorController[] stepCursorImage;

    [SerializeField] private Button btn_back, btn_exit, btn_next, btn_topic, btn_ar;
    [SerializeField] private Color active, inActive;

    [Header("Step1")]
    [SerializeField] private GameObject outlinePrefab;

    [Header("Step2")]
    [SerializeField] private GameObject inputF_prefab;
    [SerializeField] private Button btn_gCheck, btn_refresh, btn_save;
    [SerializeField] private Text txt_wordCnt, txt_wordInfo;
    private bool isRunARSetPage;

    [Header("Step3")]
    [SerializeField] private ToggleGroup arChoiceModel;
    [SerializeField] private ToggleGroup arChoiceMusic;
    [SerializeField] private InputField ipf_storyTitle, ipf_author;

    [Header("PopUp")]
    [SerializeField] private WritingPopExitController wpec; //종료팝업
    [SerializeField] private WritingPopThreeButton pop_refresh, pop_finish; //버튼이 3개 달린 팝업창 컨트롤러
    [SerializeField] private WritingPopSubmitTextOnly pop_submitTextOnly;   //결과제출 팝업: 글만제출하기, AR촬영하기 있음.
    [SerializeField] private GameObject pop_topic;
    public WritingPopComment pop_comment; //오류단어 코멘트 팝업


    /// <summary>
    /// (0:Step1)Topic (1:Step2)Story Completion (2:Step3)AR오브젝트에 따른 디자인 선택 (3:Step4)Assessment
    /// </summary>
    private Stack<int> curPage;     //현재 페이지값 관리
    private const int MAXPAGE = 3;  //Step 페이지의 최대값
    private int wordCnt = 0, wordMin, wordMax;        //G-Check 단어수
    private string outlineText;     //outline Text 보관용
    private Vector2 bgPos;  //모바일 키보드 생성 시 화면 포지션 변경을 위한 초기값
    private string[,] outlineData; //Outline데이터
    private GameManager GM;
    private SubmitManager submitManager;    //결과등록
    private WritingTextManager wtm;         //텍스트 임시저장

    private WritingOutlineController[] woc; //outline Controller 모음
    [SerializeField] private WritingGCheckController[] step2Obj;    //InputField prefab 관리용

    private enum MovePage { prev, next }
    private MovePage movePage;


    private void OnEnable()
    {
        GM = FindObjectOfType<GameManager>();
        submitManager = FindObjectOfType<SubmitManager>();
        wtm = FindObjectOfType<WritingTextManager>();

        bgPos = thisRT.anchoredPosition;
        isRunARSetPage = false;
        movePage = MovePage.next;

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
        btn_topic.onClick.AddListener(() => OnClickTopic());
        btn_gCheck.onClick.AddListener(() => OnClickGCheck());
        btn_refresh.onClick.AddListener(() => OnClickRefresh());
        btn_save.onClick.AddListener(() => OnClickSave());
        btn_ar.onClick.AddListener(() => OnClickARStart());

        btn_next.gameObject.SetActive(true);

        StartCoroutine(ChangePage(true));

        wordMax = int.Parse(ServerManager.Instance.lcsm[ServerManager.Instance.lessonIdx].ContentData.ComponentInfo[0].Word_max_count);
        wordMin = int.Parse(ServerManager.Instance.lcsm[ServerManager.Instance.lessonIdx].ContentData.ComponentInfo[0].Word_min_count);
    }


    private void OnDisable()
    {
        curPage.Clear();

        btn_back.onClick.RemoveAllListeners();
        btn_exit.onClick.RemoveAllListeners();
        btn_next.onClick.RemoveAllListeners();
        btn_gCheck.onClick.RemoveAllListeners();
        btn_refresh.onClick.RemoveAllListeners();
    }

    //Step 전환
    private IEnumerator ChangePage(bool on)
    {
        int page = curPage.Peek();

        if (on)
        {
            StepCursorViewer();
            stepPanel[page].SetActive(true);

            switch (page)
            {
                case 0:
                    StartCoroutine(SetOutlineText(page));

                    break;
                case 1:
                    isRunARSetPage = false;
                    txt_wordInfo.text = "| " + wordMin + " - " + wordMax;
                    SetGCheckText(page);

                    if (!btn_next.gameObject.activeSelf)
                        btn_next.gameObject.SetActive(true);

                    break;
                case 2:
                    btn_next.gameObject.SetActive(false);

                    break;
                case 3:

                    break;
                default:
                    Debug.Log(" page Error");

                    break;
            }
        }
        else
        {
            if (page.Equals(0))
            {
                string temp = string.Empty;
                for (int i = 0; i < woc.Length; i++)
                {
                    if (i == woc.Length - 1)
                    {
                        temp += woc[i].GetInputFieldText();
                    }
                    else
                    {
                        temp += woc[i].GetInputFieldText() + "\n";
                    }
                }

                wtm.SetText1(temp);
            }

            //Step2에서 Step3로 넘어갈때 전체 문장 G-Check 실행.
            if (page.Equals(1) && movePage.Equals(MovePage.next))
            {
                StartCoroutine(CheckWordCorrect(() =>
                {
                    bool answer = true;
                    //for (int i = 0; i < step2Obj.Length; i++)
                    //{
                    //    answer &= step2Obj[i].isCorrect;
                    //}

                    string temp = string.Empty;
                    for (int i = 0; i < woc.Length; i++)
                    {
                        if (i == woc.Length - 1)
                        {
                            temp += step2Obj[i].GetInputFieldText();
                        }
                        else
                        {
                            temp += step2Obj[i].GetInputFieldText() + "\n";
                        }
                    }
                    wtm.SetText2(temp);

                    if (answer)
                    {
                        if (!isRunARSetPage)
                        {
                            OnClickStep2Finish();
                            return;
                        }
                    }
                    else
                    {
                        wpec.Init(April.Common.LanguageString.WritingPopupNotClearGCheck, false, SetOffExitPanel);
                        wpec.gameObject.SetActive(true);
                        return;
                    }
                }));

                if (!isRunARSetPage)
                    yield break;
            }

            stepPanel[page].SetActive(false);
        }
    }


    private string[] outlineKey = { "Characters", "Setting", "Beginning", "Middle", "End" };

    //Step1 Outline
    private IEnumerator SetOutlineText(int page)
    {
        Transform pivot = stepPanel[page].GetComponent<ScrollRect>().content.transform;

        if (wtm.IsExistText1())
        {
            CheckChildObject(pivot, 0);

            print("   yd");

            woc = new WritingOutlineController[outlineKey.Length];

            string[] text = wtm.GetText1().Split('\n');

            for (int i = 0; i < woc.Length; i++)
            {
                woc[i] = Instantiate(outlinePrefab, pivot, false).GetComponent<WritingOutlineController>();
                woc[i].SetText(true, outlineKey[i], text[i]);
            }

            btn_next.interactable = true;

            yield break;
        }

        yield return StartCoroutine(CallOutLineAPI(done =>
        {
            if (done == null)
            {
                CheckChildObject(pivot, 0);

                woc = new WritingOutlineController[outlineKey.Length];

                for (int i = 0; i < woc.Length; i++)
                {
                    woc[i] = Instantiate(outlinePrefab, pivot, false).GetComponent<WritingOutlineController>();
                    woc[i].SetText(true, outlineKey[i], string.Empty);
                }

                btn_next.interactable = true;

                return;
            }

            if (CheckChildObject(pivot, done.GetLength(0)))
            {
                woc = new WritingOutlineController[done.GetLength(0)];

                for (int i = 0; i < done.GetLength(0); i++)
                {
                    woc[i] = Instantiate(outlinePrefab, pivot, false).GetComponent<WritingOutlineController>();
                    woc[i].SetText(false, done[i, 0], done[i, 1]);
                }
            }

            btn_next.interactable = true;
        }));
    }

    //Live class에서 작성한 글 불러옴
    private IEnumerator CallOutLineAPI(Action<string[,]> done)
    {
        WWWForm form = new WWWForm();
        form.AddField("sem_id", ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_id);
        form.AddField("top_cors_id", ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].top_cors_id);
        form.AddField("lv_cd", ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_cd);

        if (ServerManager.Instance._isTestURL)
        {
            form.AddField("g_seq", 1);
            form.AddField("com_id", 1);
        }
        else
        {
            form.AddField("g_seq", ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.selectLevelData].g_seq);
            form.AddField("com_id", ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.selectLevelData].com_id);
        }

        UnityWebRequest uwr = UnityWebRequest.Post(ServerManager.Instance._BASE_HTTPS_URL_MeMe_CLASSOUTLINE, form);
        uwr.SetRequestHeader("X-Audience", "bouncy");
        uwr.SetRequestHeader("X-Auth", ServerManager.Instance.token);
        if (ServerManager.Instance._isTestURL)
        {
            uwr.SetRequestHeader("std_id", "1592580");
        }

        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError || uwr.isHttpError)
        {
            Debug.Log(uwr.error);
        }
        else
        {
            outlineText = uwr.downloadHandler.text;

            JObject json = JObject.Parse(uwr.downloadHandler.text);
            string data = json["data"].ToString();

            if (data.Length < 1)
            {
                done(null);
                yield break;
            }

            int where = data.IndexOf('{');
            data = data.Remove(0, where + 1);
            where = data.IndexOf('}');
            data = data.Remove(where, data.Length - where);

            string[] dataSplit = data.Split('\n'); //줄바꿈 별 문장 분류
            List<string> dataReplace = new List<string>(); //줄바꿈된 dataSplit에서 빈 데이터 제거
            for (int i = 0; i < dataSplit.Length; i++)
            {
                string temp = Regex.Replace(dataSplit[i], @"[^a-zA-Z]", string.Empty);
                if (temp.Equals(string.Empty))
                    continue;

                dataReplace.Add(dataSplit[i]);
            }

            outlineData = new string[dataReplace.Count, 2];
            for (int i = 0; i < dataReplace.Count; i++)
            {
                dataReplace[i] = Regex.Replace(dataReplace[i], @"[^a-zA-Z:, ]", string.Empty);

                string[] result = dataReplace[i].Split(':');
                for (int j = 0; j < result.Length; j++)
                {
                    result[j] = Regex.Replace(result[j], @"[^a-zA-Z, ]", string.Empty);
                    result[j] = result[j].Replace(',', ' ').Trim();

                    outlineData[i, j] = result[j];
                }
            }

            done(outlineData);
        }

        yield return null;
    }

    private void CheckOutlineEmpty()
    {
        Transform pivot = stepPanel[0].GetComponent<ScrollRect>().content.transform;
        bool isEmpty = false;

        CustomInputField[] ipfs = pivot.GetComponentsInChildren<CustomInputField>();
        for (int i = 0; i < ipfs.Length; i++)
        {
            if (ipfs[i].text.Equals(string.Empty))
            {
                isEmpty = true;
                break;
            }
        }

        btn_next.interactable = !isEmpty;
    }


    //Step2 Story Completion: step1에서 입력된 텍스트를 받아옴.
    private void SetGCheckText(int page)
    {
        List<string> splitData = new List<string>();
        if (movePage.Equals(MovePage.prev))
        {
            splitData = wtm.GetText2().Split('\n').ToList();
        }
        else
        {
            for (int i = 0; i < woc.Length; i++)
            {
                string tempString = woc[i].GetInputFieldText();
                if (tempString.Contains("\n"))
                {
                    string[] temp = tempString.Split('\n');
                    for (int j = 0; j < temp.Length; j++)
                    {
                        splitData.Add(temp[j]);
                    }
                }
                else
                {
                    splitData.Add(tempString);
                }
            }
        }

        Transform pivot = stepPanel[page].GetComponent<ScrollRect>().content.transform;

        if (CheckChildObject(pivot, splitData.Count))
        {
            step2Obj = new WritingGCheckController[splitData.Count];

            for (int i = 0; i < splitData.Count; i++)
            {
                GameObject go_ipf = Instantiate(inputF_prefab, pivot, false);
                step2Obj[i] = go_ipf.GetComponent<WritingGCheckController>();
                step2Obj[i].Init(splitData[i], this);
            }
        }
        else
        {
            for (int i = 0; i < splitData.Count; i++)
            {
                step2Obj[i].Init(splitData[i], this);
            }
        }

        WordCount();
    }

    //전체 문장의 단어수 카운트 - inputfield의 on value change로 실행 (청담에서는 입력하는 매순간 실시간으로 카운트 가능하냐고함)
    private void WordCount()
    {
        List<string> data = new List<string>();

        for (int i = 0; i < step2Obj.Length; i++)
        {
            string temp = step2Obj[i].GetComponent<CustomInputField_TMP>().text;
            data.Add(temp);
        }

        wordCnt = 0;

        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].Contains("<color="))
            {
                data[i].Replace("<color=#ff0000><U>", string.Empty);
                data[i].Replace("</U></color>", string.Empty);
            }

            List<string> temp = data[i].Split(' ').ToList();

            for (int j = 0; j < temp.Count; j++)
            {
                temp[j] = Regex.Replace(temp[j], @"[^a-zA-Z0-9힣]", "", RegexOptions.Singleline);
            }

            for (int j = temp.Count - 1; j >= 0; j--)
            {
                if (temp[j].Equals(""))
                    temp.RemoveAt(j);
            }

            wordCnt += temp.Count;
        }

        txt_wordCnt.text = "<color=#ff7b91>" + wordCnt + "</color> words";

        btn_next.interactable = wordCnt >= wordMin;  //단어 제한 임시데이터
    }

    //전체문장 G-Check실행
    private IEnumerator CheckWordCorrect(Action done)
    {
        for (int i = 0; i < step2Obj.Length; i++)
        {
            step2Obj[i].StartGCheck();
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(0.3f);

        done();
    }

    //Step3 Output Creation
    private void SetARWritingValues()
    {
        SetWritingText(ipf_storyTitle.text, ipf_author.text);

        string curModel = "";
        foreach (Toggle tgg in arChoiceModel.ActiveToggles())
        {
            curModel = tgg.name;
        }

        switch (curModel)
        {
            case "ar_sketchbook":
                GM.currentobj = GM.sketchbook;
                GM.objType = GameManager.objectType.Sketchbook;

                break;
            case "ar_chalkboard":
                GM.currentobj = GM.chalkboard;
                GM.objType = GameManager.objectType.Chalkboard;

                break;
            case "ar_pad":
                GM.currentobj = GM.drawingPad;
                GM.objType = GameManager.objectType.DrawingPad;

                break;
            default:
                Debug.Log("  curModel Erorr");
                break;
        }

        string curMusic = "";
        foreach (Toggle tgg in arChoiceMusic.ActiveToggles())
        {
            curMusic = tgg.name;
        }

        curMusic = curMusic.Replace("arSub_bgm", string.Empty);
        int musicIndex = int.Parse(curMusic);

        GM.currentBGM = GM.MidBGMs[musicIndex];
        GM.SelectBGMPlay();
    }

    //작성된 제목, 작성자, 글 GameManager로 보관
    public void SetWritingText(string title, string author)
    {
        string sendResult = string.Empty;
        for (int i = 0; i < step2Obj.Length; i++)
        {
            sendResult += step2Obj[i].GetComponent<CustomInputField_TMP>().text + "\n";
        }

        GM.writingTitle = title;
        GM.writingAuthor = author;
        GM.writingText = sendResult;
    }


    /// <summary>
    /// InputField prefab생성상태 확인
    /// 오브젝트가 있을경우 파괴
    /// </summary>
    private bool CheckChildObject(Transform tr, int length)
    {
        int wocCnt = tr.childCount;

        if (wocCnt == length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < wocCnt; i++)
            {
                Destroy(tr.GetChild(i).gameObject);
            }

            return true;
        }
    }

    #region ONCLICK_FUNTIONS
    private void OnClickBack()
    {
        movePage = MovePage.prev;

        if (curPage.Peek().Equals(0))
        {
            OnClickExit();
        }
        else
        {
            StartCoroutine(ChangePage(false));

            curPage.Pop();
            StartCoroutine(ChangePage(true));
        }
    }

    private void OnClickNext()
    {
        movePage = MovePage.next;

        if (curPage.Peek().Equals(MAXPAGE))
        {
            print("LastPage");
        }
        else
        {
            StartCoroutine(ChangePage(false));

            if (!isRunARSetPage && curPage.Peek().Equals(1))
                return;

            curPage.Push(curPage.Peek() + 1);
            StartCoroutine(ChangePage(true));
        }
    }

    private void OnClickExit()
    {
        wpec.Init(April.Common.LanguageString.WritingPopupExitLesson, true, SetOffStep2Panal);
        wpec.gameObject.SetActive(true);
    }

    public void SetOffStep2Panal()
    {
        gameObject.SetActive(false);
    }

    private void SetOffExitPanel()
    {
        wpec.gameObject.SetActive(false);
    }

    private void OnClickSave()
    {
        wpec.Init(April.Common.LanguageString.WritingPopupSaveTextTemp, false, null);
        wpec.gameObject.SetActive(true);
    }

    private void OnClickTopic()
    {
        pop_topic.SetActive(true);
    }

    private void OnClickRefresh()
    {
        pop_refresh.Init("refresh", RunRefresh, RunReset);
        pop_refresh.gameObject.SetActive(true);
    }

    private void RunRefresh()
    {
        for (int i = 0; i < step2Obj.Length; i++)
        {
            step2Obj[i].ResetText();
        }
    }

    private void RunReset()
    {
        for (int i = 0; i < step2Obj.Length; i++)
        {
            step2Obj[i].ClearText();
        }
    }

    private void OnClickStep2Finish()
    {
        //REMAIN_TASK: 제출된 학습의 경우 팝업제공없이 Step3로 이동

        pop_finish.Init("finish", SubmitTextOnly, RunARRecordPage);
        pop_finish.gameObject.SetActive(true);
    }

    private void SubmitTextOnly()
    {
        //REMAIN_TASK: 제출기한 체크: EpisodeManager에서 만료여부 데이터로 토스트메시지 출력 후 제출되는 기능은 막기

        pop_finish.gameObject.SetActive(false);
        pop_submitTextOnly.Init();
        pop_submitTextOnly.gameObject.SetActive(true);
    }

    private void RunARRecordPage()
    {
        isRunARSetPage = true;
        OnClickNext();
    }

    private void OnClickGCheck()
    {
        btn_gCheck.interactable = false;
        btn_next.interactable = false;
        btn_refresh.interactable = false;

        StartCoroutine(CheckWordCorrect(() =>
        {
            btn_gCheck.interactable = true;
            btn_next.interactable = true;
            btn_refresh.interactable = true;
        }));
    }

    private void OnClickARStart()
    {
        SetARWritingValues();
        SubmitData();

        GM.PlayARScene();
    }

    #endregion

    // 상단의 커서 아이콘 현재상황 표시
    private void StepCursorViewer()
    {
        int index = curPage.Peek();

        GameObject stepParent = stepCursorImage[0].transform.parent.gameObject;
        stepParent.SetActive(false);

        for (int i = 0; i < stepCursorImage.Length; i++)
        {
            stepCursorImage[i].ChangeActive(i.Equals(index), i < index);
        }
        stepParent.SetActive(true);
    }

    //글만제출하기, AR모드 진입시 실행
    //글만제출하기로 진입할때는 서버전송까지 완료.
    public void SubmitData()
    {
        submitManager.WritingInit();

        WritingData wd = new WritingData
        {
            sem_id = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_id,
            top_cors_id = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].top_cors_id,
            com_id = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].com_id,
            g_seq = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].g_seq,
            app_ver = Application.version,
            manufacturer = "",  //제조사를 어떻게 아니?
            model = SystemInfo.deviceModel,
            submit_seq = "1",   //임시
            score = "",         //임시
            study_time = 240,   //임시
            word_count = wordCnt,
            title = GM.writingTitle,
            author = GM.writingAuthor,
            std_email_to = "",  //모름
            std_capture_image = "https://ail.chungdahm.com/april_il_sw/study_result/593/791/92561/1050/1422842/1422842_capture_20210714085010010.png",
            std_upload_image = "https://ail.chungdahm.com/april_il_sw/study_result/593/791/92561/1050/1422842/1422842_upload_20210714085010020.png",
            std_video = "https://april30ctpstorage.blob.core.windows.net/mov/real/586/866/678439_1618291804902.mp4",             //not yet
            std_video_thumbnail = "https://april30ctpstorage.blob.core.windows.net/mov/real/586/866/678439_1618291804902.jpg",   //not yet
            std_video_share = 0,     //결과제출에서 사용되는 데이터가 아님. AR비디오 촬영 모두 끝나고 Submit할때 얻을수 있는 데이터
            word_max_count = 200,    //임시
            word_recomm_count = 65,  //임시
            word_min_count = 45,     //임시
            prompt_eng = "Imagine that you wanted to try the pickle experiment. Write a story about what happened.",
            prompt_kor = "여러분이 피클 실험을 직접 해 보고 싶었다고 상상해 보고, 어떤 일이 일어났는지 이야기를 쓰세요.",
            std_outline = outlineText,
            stdContent = GM.writingText,
            ginger_revisionText = "hi im. im gukgukguk.<br>long ago in korea, kings and queens were buried their gold cloths and their servan. <ginger>And</ginger> some people say that ant to be buried alone. in my opinion, i would like to buried with all my <ginger>trophies</ginger> medals <ginger>when</ginger> i die.<br>first, i have recorded all .<br>",   //??
            ginger_revisionData = null,
            act_data = null
        };

        submitManager.SetWdata(wd);
        //submitManager.SendData(null);
    }

    #region KEYBOARD_MOVING
    //InputField 활성화시 화면을 상단으로 이동. (이동하는 중에 하단영역에서 뒷쪽 레이어화면이 보인다고 수정해달라고함)
    public void KeyBoardOn(bool on, float y)
    {
        if (!on)
        {
            if (curPage.Peek().Equals(1))
                WordCount();
            else if (curPage.Peek().Equals(0))
                CheckOutlineEmpty();
        }

        StartCoroutine(MoveRectPos(on ? new Vector2(bgPos.x, y) : bgPos));
    }

    private IEnumerator MoveRectPos(Vector2 next)
    {
        float step = 0;
        while (step < 0.5f)
        {
            thisRT.anchoredPosition = Vector2.Lerp(thisRT.anchoredPosition, next, step += Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
    }
    #endregion

    private Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null)
            return null;

        Rect rect = new Rect(0, 0, tex.width, tex.height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
    }
}