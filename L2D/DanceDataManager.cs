using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class DanceDataManager : MonoBehaviour
{
    [SerializeField] private Sprite defaulAlbumImage;
    private string exPath;
    private readonly string titleTag_start = "<i><size=80%><#FF7EF1>";
    private readonly string titleTag_end = "</color></size></i>";

    public static DanceDataManager instance;
    public MusicList musicList;
    public List<CompInfo> compInfo;

    public int curIndex { get; set; }
    public bool isTracking { get; set; }
    public bool isAR { get; set; }

    public Music GetCurMusic()
    {
        return musicList.music[curIndex];
    }

    public string GetCurMusicText()
    {
        return GetCurMusic().artist + " - " + titleTag_start + GetCurMusic().title + titleTag_end;
    }

    public void SetScore(string st)
    {
        print(st);

        string beforeScore = musicList.music[curIndex].bestScoreNDate;
        if (beforeScore == string.Empty)
        {
            musicList.music[curIndex].bestScoreNDate = st;
        }
        else
        {
            int getScore = int.Parse(beforeScore.Split('/')[0]);
            int curScore = int.Parse(st.Split('/')[0]);

            if (getScore < curScore)
            {
                musicList.music[curIndex].bestScoreNDate = st;
            }
        }

        KeepData();
    }

    private void KeepData()
    {
        string json = JsonUtility.ToJson(musicList);
        File.WriteAllText(exPath, json);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            KeepData();
        }
    }

    private void OnApplicationQuit()
    {
        KeepData();
    }

    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Application.backgroundLoadingPriority = ThreadPriority.Low;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        StartCoroutine(InitData_LoadMainScene());
    }

    private IEnumerator InitData_LoadMainScene()
    {
        musicList = null;
        curIndex = 0;
        SplashManager spManager = FindObjectOfType<SplashManager>();
        compInfo = new List<CompInfo>();

        exPath = Path.Combine(Application.persistentDataPath, "refData.json");
        string inPath = Path.Combine(Application.streamingAssetsPath, "music.json");
        //string bundleInPath = Path.Combine(Application.streamingAssetsPath, "l2d");

        // copy from streamingAssetsPath
        if (File.Exists(exPath))
        {
            StartCoroutine(ReadJsonFromPath(inPath, done =>
            {
                CheckJsonData(done);
            }));
        }
        else
        {
            print(">> first run.");
            StartCoroutine(ReadJsonFromPath(inPath, done =>
            {
                musicList = done;
            }));
        }

        yield return new WaitUntil(() => musicList != null);

        //yield return File.Exists(bundleInPath);
        //UnityWebRequest webr = UnityWebRequestAssetBundle.GetAssetBundle(bundleInPath);
        //webr.SendWebRequest();

        //while (!webr.isDone) yield return new WaitForEndOfFrame();

        //AssetBundle bundles = DownloadHandlerAssetBundle.GetContent(webr);

        spManager.InitSlider(musicList.music.Count);
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < musicList.music.Count; i++)
        {
            string artist = musicList.music[i].artist;
            string findString = artist + "-" + musicList.music[i].title;
            spManager.UpdateLoadingText(findString);

            string comp = musicList.music[i].comp;
            if (comp != "")
            {
                SetCompInfo(artist, comp);
            }

            ResourceRequest loadAudio = Resources.LoadAsync<AudioClip>("Musics/" + findString);
            yield return new WaitUntil(() => loadAudio.isDone);

            spManager.UpdateSlider(0.3f);
            ResourceRequest loadTimeline = Resources.LoadAsync<TimelineAsset>("Timelines/" + findString);
            yield return new WaitUntil(() => loadTimeline.isDone);

            spManager.UpdateSlider(0.3f);
            ResourceRequest loadSprite = Resources.LoadAsync<Sprite>("Sprites/AlbumCover/" + artist);
            yield return new WaitUntil(() => loadSprite.isDone);

            musicList.music[i].clip = loadAudio.asset as AudioClip;
            musicList.music[i].timeline = loadTimeline.asset as TimelineAsset;
            musicList.music[i].coverImg = loadSprite.asset as Sprite ?? defaulAlbumImage;

            spManager.UpdateSlider(0.4f);
        }
        Resources.UnloadUnusedAssets();

        spManager.FinLoading();
        spManager = null;
    }

    private void CheckJsonData(MusicList inMI)
    {
        string exJson = File.ReadAllText(exPath);
        MusicList exMI = JsonUtility.FromJson<MusicList>(exJson);
        print(inMI.version + " <in:out> " + exMI.version);

        if (inMI.version != exMI.version)
        {
            for (int i = 0; i < inMI.music.Count; i++)
            {
                for (int j = 0; j < exMI.music.Count; j++)
                {
                    if (inMI.music[i].title.Equals(exMI.music[j].title))
                    {
                        inMI.music[i].favor = exMI.music[j].favor;
                        inMI.music[i].bestScoreNDate = exMI.music[j].bestScoreNDate;

                        break;
                    }
                }
            }

            print(">> Merge new data.");
            musicList = inMI;
        }
        else
        {
            print(">> no new data.");
            musicList = exMI;
        }
    }

    private IEnumerator ReadJsonFromPath(string path, Action<MusicList> done)
    {
        string json;

#if UNITY_EDITOR || UNITY_IOS
        json = File.ReadAllText(path);

        yield return null;
#elif UNITY_ANDROID
        UnityWebRequest reader = UnityWebRequest.Get(path);
        yield return reader.SendWebRequest();

        json = reader.downloadHandler.text;
#endif
        done(JsonUtility.FromJson<MusicList>(json));
    }

    private void SetCompInfo(string artist, string comp)
    {
        CompInfo ci = new CompInfo
        {
            comp = comp,
            artist = artist,
            img = Resources.Load<Sprite>("Sprites/AlbumCover/" + artist)
        };
        compInfo.Add(ci);
    }
}

[Serializable]
public class MusicList
{
    /// <summary>
    /// version = 날짜조합으로 비교
    /// </summary>
    public string version;
    public List<Music> music;
}

[Serializable]
public class Music
{
    public string artist;
    public string title;
    public bool favor;
    public float rootRot;
    public string comp;
    public int dancerCnt;
    public int member;
    /// <summary>
    /// score/Date
    /// </summary>
    public string bestScoreNDate;
    public AudioClip clip;
    public Sprite coverImg;
    public TimelineAsset timeline;
}

[Serializable]
public class CompInfo
{
    public string comp;
    public string artist;
    public Sprite img;
}