
using April.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    /// <summary>
    /// 이름
    /// </summary>
    [SerializeField] private Text[] userName;
    /// <summary>
    /// 사용자 이미지
    /// </summary>
    [SerializeField] private Image[] userImage;

    [SerializeField] private Text[] userSemText;
    [SerializeField] Sprite UserDefaultImage;

    //[SerializeField] private PortfolioPanel portfolioPanel;

    //에피소드 버튼 터치시 오브젝트 활성, 비활성 필요.
    public GameObject objMainPanel, objSpeakDirPanel, objWritingDirPanel;
    public DirectionsPanel directionsPanel;
    public WritingCaptureController w_capture;

    private IEnumerator CurrentCoroutine;
    int _dataSize = 0;// 다운로드 받을 용량


    IEnumerator DownLoadStart()
    {
        ServerManager.Instance._isTimeOut = false;//타임아웃 재시작

        April.Common.MessageBox.ARDownload.Instance.DownloadStateTextSet(LanguageString.ApplyLearning);

        yield return new WaitForSeconds(0.15f);

        April.Common.MessageBox.ARDownload.Instance.ProgressBar(0.75f);

        yield return StartCoroutine(UserInformation());

        April.Common.MessageBox.ARDownload.Instance.ProgressBar(0.9f);

        ServerManager.Instance.lcsm.Clear();

        yield return StartCoroutine(ServerManager.Instance.CoContentDownLoad());

        //portfolioPanel.VideoListSetting(ServerManager.Instance.MeMeUserLessonData.data.lesson_data);

        April.Common.MessageBox.ARDownload.Instance.ProgressBar(1.0f);

        April.Common.MessageBox.ARDownload.Instance.Hide();

        if (GameManager._instance.navBack)
        {
            directionsPanel.gameObject.SetActive(true);
            directionsPanel.VideosBtn.onClick.Invoke();
            GameManager._instance.navBack = false;
        }

        ServerManager.Instance._isTimeOut = true;// 타임아웃 종료
    }


    public IEnumerator Start()
    {
        bool _isNotFile = false;    //파일유무 체크용
        ServerManager.Instance.IsGetServerDate = false;

        w_capture.gameObject.SetActive(false);

        //학습 레벨에 따른 에피소드 갯수 설정
        FindObjectOfType<EpisodeManager>().SetEpisodeData();

        //파일 다운로드 용량 확인
        float _byte = 0;
        for (int i = 0; i < ServerManager.Instance.MeMeUserLessonData.data.lesson_data.Count; i++)
        {
            //==============ServerManager 1166과 중복
            string keyName = null;
            string[] urlParm = null;
            string[] keyNameParm = null;

            urlParm = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[i].content_url.Split('/');
            for (int a = urlParm.Length - 4; a < urlParm.Length; a++)// url로 저장용 키 재조합 
            {
                if (a < urlParm.Length - 1)
                    keyName += urlParm[a] + "/";
            }

            keyNameParm = urlParm[urlParm.Length - 1].Split('_');
            for (int a = 0; a < keyNameParm.Length; a++)
            {
                if (a < keyNameParm.Length - 1)
                    keyName += keyNameParm[a] + "_";
            }
            //====================================

            string _savepath = Application.persistentDataPath + "/" + ServerManager.Instance.GetDirectoryPath(i);
            _dataSize = 0;

            // 파일체크
            if (System.IO.File.Exists(_savepath + "/Content.xml") == false)
            {
                _isNotFile = true;
                if (ServerManager.Instance.MeMeUserLessonData.data.lesson_data[i].content_url == string.Empty)
                {
                    Debug.LogError("Content Url Null");
                }
                else
                {
                    //print("   " + ServerManager.Instance.MeMeUserLessonData.data.lesson_data[i].content_url);
                    yield return StartCoroutine(GetHeader(ServerManager.Instance.MeMeUserLessonData.data.lesson_data[i].content_url));
                }
            }

            // 버전체크
            if (_isNotFile == false)// 파일이 있으면 체크
            {
                if (!PlayerPrefs.HasKey(keyName) || PlayerPrefs.GetString(keyName) != keyNameParm[keyNameParm.Length - 1])
                {
                    yield return StartCoroutine(GetHeader(ServerManager.Instance.MeMeUserLessonData.data.lesson_data[i].content_url));
                }
            }

            _byte += _dataSize;
        }

        int _mb = 0;
        _mb = Mathf.RoundToInt(_byte * 0.000001f);
        if (_mb == 0)
        {
            _mb = 1;
        }
        yield return null;

        if (_byte > 0)
        {
            April.Common.MessageBox.ARDownload.Instance.ShowMsg(_mb, DownloadMsgCallBack);
            ServerManager.Instance._isTimeOut = true;//잠시멈춤
        }
        else
        {
            DownloadMsgCallBack();
        }

    }
    IEnumerator GetHeader(string url)// 헤더 체크
    {
        //print("  Header  " + UnityWebRequest.Head(url));

        using (UnityWebRequest webRequest = UnityWebRequest.Head(url))// Get
        {
            yield return webRequest.SendWebRequest();
            _dataSize = int.Parse(webRequest.GetResponseHeader("Content-Length"));

            //print("  Header  " + _dataSize);
        }
    }

    public IEnumerator Cor_ContentReset()
    {
        yield return ServerManager.Instance.RestartListMeMe();
        yield return StartCoroutine(ServerManager.Instance.CoContentDownLoad());
    }

    public void DownloadMsgCallBack()
    {
        StartCoroutine(DownLoadStart());
    }

    public IEnumerator UserInformation()
    {
        April.Common.MessageBox.ARDownload.Instance.DownloadStateTextSet(LanguageString.ApplyLearning);
        //사용자 이름
        for (int i = 0; i < userName.Length; i++)
        {
            userName[i].text = ServerManager.Instance.MeMeUserData.data.std_name;
        }

        for (int i = 0; i < userSemText.Length; i++)
        {
            userSemText[i].text = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_eng_nm
                + " / " + ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm;
        }

        //사용자 이미지 변경
        if (CurrentCoroutine != null)
        {
            StopCoroutine(CurrentCoroutine);
        }
        CurrentCoroutine = UserImageLoad(ServerManager.Instance.MeMeUserData.data.std_image);
        yield return StartCoroutine(CurrentCoroutine);
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>
    /// 사용자 레벨 타이틀 변경
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    string TitleNameChange(string name)
    {
        if (name.Contains("Low"))
        {
            return "Pic Me";
        }
        else if (name.Contains("Mid"))
        {
            return "Story Time";
        }
        else if (name.Contains("High"))
        {
            return "Ring Ding Dong";
        }

        return "Error";
    }

    /// <summary>
    /// 사용자 이미지 변경
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IEnumerator UserImageLoad(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            WWW www = new WWW(path);
            while (!www.isDone) yield return null;

            if (string.IsNullOrEmpty(www.error))
            {
                if (www.texture != null)
                {
                    for (int i = 0; i < userImage.Length; i++)
                    {
                        userImage[i].sprite = TextureToSprite(www.texture);
                    }
                }
                else
                {
                    StartCoroutine(UserImageLoad(path));
                }
            }
            else
            {
                for (int i = 0; i < userImage.Length; i++)
                {
                    userImage[i].sprite = UserDefaultImage;
                }
            }
        }
        else
        {
            for (int i = 0; i < userImage.Length; i++)
            {
                userImage[i].sprite = UserDefaultImage;
            }
        }
    }

    private Sprite TextureToSprite(Texture2D tex)
    {
        Rect rect = new Rect(0, 0, tex.width, tex.height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
    }
}
