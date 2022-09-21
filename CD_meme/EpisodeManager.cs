using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class EpisodeManager : MonoBehaviour
{
    /// <summary>
    /// 서버에서 받아온 lessonData중 week_data
    /// </summary>
    private List<ServerManager.WeekData> weekData;
    private List<ServerManager.LessonData> lessonData;
    /// <summary>
    /// activeEpisodeData의 각 인덱스는 Writing과 Speaking으로 두개의 EpisodtState객체를 포함. (EpisodeState의 배열 개수는 2개)
    /// Writing과 Speaking가 한 묶음으로 1week의 데이터를 포함.
    /// </summary>
    public List<EpisodeState[]> activeEpisodeData;
    /// <summary>
    /// activeEpisodeData로 셋팅된 prefab 클론의 게임오브젝트 모음.
    /// count = 활성화된 week의 개수
    /// </summary>
    private List<GameObject> contentBoxs;

    [SerializeField] private GameObject weekTggPrefab, weekTab, weekContentPrefab, weekContent;
    [SerializeField] private Color color_active, color_inActive;

    private const int MAXWEEK = 12;

    //메인화면 첫 진입시 실행. (from LobbyPanel.cs)
    public void SetEpisodeData()
    {
        lessonData = ServerManager.Instance.MeMeUserLessonData.data.lesson_data;

        SetContentData();

        CreateEpisodeGroup();
    }

    //메인화면의 Speaking, Writing 버튼과 토글버튼 초기화
    private void CreateEpisodeGroup()
    {
        int lastIndex = 0;
        contentBoxs = new List<GameObject>();

        if (ServerManager.Instance.IsGetServerDate == false)
        {
            ServerManager.Instance.IsGetServerDate = true;
            StartCoroutine(ServerManager.Instance.GetServerDate());
        }

        for (int i = 0; i < MAXWEEK; i++)
        {
            GameObject tab = Instantiate(weekTggPrefab, weekTab.transform, false);

            bool isActive = i < activeEpisodeData.Count;

            if (isActive)
            {
                GameObject content = Instantiate(weekContentPrefab, weekContent.transform, false);
                content.GetComponent<WeekContentController>().SetContentData(activeEpisodeData[i]);
                content.SetActive(false);
                contentBoxs.Add(content);
            }

            Toggle tgg = tab.GetComponent<Toggle>();
            tgg.GetComponent<WeekToggleController>().ToggleInit(i, isActive);
            tgg.onValueChanged.AddListener(delegate { ContentBoxOn(tgg); });

            //if (i >= activeEpisodeData.Count)
            //{
            //    tgg.interactable = false;
            //}

            if (isActive && i < contentBoxs.Count)
            {
                tab.GetComponent<Toggle>().isOn = true;
                lastIndex = i;
            }
        }

        contentBoxs[lastIndex].SetActive(true);
    }

    private void ContentBoxOn(Toggle tgg)
    {
        int index = int.Parse(tgg.name);

        if (index < contentBoxs.Count)
        {
            for (int i = 0; i < contentBoxs.Count; i++)
            {
                contentBoxs[i].SetActive(false);
            }
            contentBoxs[index].SetActive(true);
        }
    }

    //서버데이터로 활성화 일자 및 종료일 등 계산 (Old Code)
    //private WeekState DueDateText(int week)
    //{
    //    DateTime endDate = DateTime.Parse(lessonData[week].s_end);
    //    DateTime curDate = DateTime.Parse(ServerManager.Instance._currentDate);
    //    curDate = new DateTime(curDate.Year, curDate.Month, curDate.Day, 0, 0, 0);
    //    DateTime startDate = DateTime.Parse(lessonData[week].s_start);
    //    TimeSpan remainDate = endDate - curDate;
    //    TimeSpan passDate = curDate - startDate;

    //    WeekState state = new WeekState
    //    {
    //        isActive = passDate.TotalDays >= 0,
    //    };

    //    if (remainDate.TotalDays < 0) //종료일 초과
    //    {
    //        state.remainDate = endDate.ToString("yyyy.MM.dd") + "_overdue";
    //    }
    //    else
    //    {
    //        state.remainDate = endDate.ToString("yyyy.MM.dd") + "_" + remainDate.Days;
    //    }

    //    return state;
    //}

    //activeEpisodeData에 데이터값 추가.
    private void SetContentData()
    {
        activeEpisodeData = new List<EpisodeState[]>();
        EpisodeState[] es = new EpisodeState[2];
        for (int i = 0; i < lessonData.Count; i++)
        {
            int index = i % 2;

            if (index == 0)
                es = new EpisodeState[2];

            string com_nm_new = lessonData[i].com_nm.Replace("MeMe (", string.Empty);
            com_nm_new = com_nm_new.Replace(")", string.Empty);

            //WeekState ws = DueDateText(i);

            DateTime endDate = DateTime.Parse(lessonData[i].s_end);
            DateTime curDate = DateTime.Parse(ServerManager.Instance._currentDate);
            curDate = new DateTime(curDate.Year, curDate.Month, curDate.Day, 0, 0, 0);
            DateTime startDate = DateTime.Parse(lessonData[i].s_start);
            TimeSpan remainDate = endDate - curDate;
            TimeSpan passDate = curDate - startDate;
            string remainText;

            if (remainDate.TotalDays < 0) //종료일 초과
            {
                remainText = endDate.ToString("yyyy.MM.dd") + "_overdue";
            }
            else
            {
                remainText = endDate.ToString("yyyy.MM.dd") + "_" + remainDate.Days;
            }

            es[index] = new EpisodeState
            {
                title = com_nm_new,
                lecture = lessonData[i].lesson_title,
                isActive = passDate.TotalDays >= 0,
                week = i / 2,
                remainDate = remainText,
                submit_date = lessonData[i].submit_date,
                e_end = lessonData[i].e_end,
                isSubmit = lessonData[i].submit_YN == "Y"
            };

            if (index == 1)
                activeEpisodeData.Add(es);
        }

        //for (int i = 0; i < activeEpisodeData.Count; i++)
        //{
        //    print(activeEpisodeData[i][0].lecture + "    " + activeEpisodeData[i][0].remainDate);
        //    print(activeEpisodeData[i][1].lecture + "    " + activeEpisodeData[i][1].remainDate);
        //}
        //print(activeEpisodeData.Count);
    }
}

[Serializable]
public class WeekState
{
    public bool isActive;
    public string remainDate; //날짜_(남은일수or상태)
}

[Serializable]
public class EpisodeState
{
    public string title;
    public string lecture;
    public bool isActive;
    public int week;
    public string remainDate; //날짜_(남은일수or상태)
    public string submit_date;
    public string e_end;
    public bool isSubmit;
}