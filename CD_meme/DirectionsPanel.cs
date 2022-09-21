using April.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DirectionsPanel : MonoBehaviour
{
    /// <summary>
    /// dirPath = Application.persistentDataPath + "/" + ServerManager.Instance.GetDirectoryPath(epArrayIdx)
    /// </summary>
    private string dirPath;
    private int lessonIdx;
    private IEnumerator CurrentCoutine;

    [Header("Common")]
    [SerializeField] private GameObject objPhotoManagerNoneReset;
    [SerializeField] private ImageCropper photoCropper;
    [SerializeField] private Image settingUserFace;
    [SerializeField] private RectTransform[] epScrollPanel;
    [SerializeField] private GameObject[] epSelectObj;
    [SerializeField] private Text[] epTitle;
    [SerializeField] private Text[] epTopic;
    [SerializeField] private VideoListPanel[] videoPanel;
    public Text[] epDueDate;

    [Header("SpeakingDirections")]
    [SerializeField] private GameObject[] epiImg;
    [SerializeField] private RectTransform speakRectImageSnap; //20.08.19 sw.park : Episode 이미지 스크롤 위치 초기화를 위해 추가.
    [SerializeField] private Text speakDirectionText;
    [SerializeField] private Button btnNotes;
    [SerializeField] private Button directionBtn;
    public Button VideosBtn;

    //20.07.17 sw.park : Episode Tip 화면 추가.
    public GameObject EpisodeTipObj;

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Text playTime;
    [SerializeField] private Button playBtn;

    [Header("SpeakingSelect")]
    [SerializeField] private Image imageAvatarFace;

    [SerializeField] private Image btn_addAvatarFace, btn_endingPage;
    [SerializeField] private Sprite img_btnAdd, img_btnEdit;

    enum Photo_mode
    {
        Episode,
        Avatar
    }
    Photo_mode photoType;

    enum DownLoadType
    {
        Video,
        Image
    }

    //기본 에피소드 텍스쳐 저장.
    //[HideInInspector]
    public Texture[] baseEpisodeTexture = new Texture[5];

    private EpisodeState[] esList;
    private readonly string[] storytelling_presentation = {"Look at the <B>5 pictures</B> to tell a story. Talk about what will happen next and add your own\npicture describing it. You will have <B>100 seconds</b> to tell your story.",
        "Look at the <B>5 pictures</B> to make a presentation. At the end, give your opinion and details\nabout the topic and add a relevant picture. You will have <B>100 seconds</B> to give your\npresentation." };

    public void Show(int episode_array_idx, EpisodeState[] es)
    {
        lessonIdx = episode_array_idx;
        esList = es;
        ServerManager.Instance.lessonIdx = lessonIdx;

        dirPath = Application.persistentDataPath + "/" + ServerManager.Instance.GetDirectoryPath(lessonIdx);

        if (CurrentCoutine != null)
        {
            StopCoroutine(CurrentCoutine);
        }

        if (GameManager._instance.curContent.Equals(GameManager.ContentType.Speaking))
            speakRectImageSnap.anchoredPosition = Vector2.zero;

        CurrentCoutine = CoShow();
        StartCoroutine(CurrentCoutine);
    }

    //에피소드 Tip 버튼이 제공되는 레벨 체크
    bool checkTipButtonLevel(int ep_num)
    {
        //Low : Seedbed2, Seed1, Seed2
        //Mid : Sprout 1~3 & Episode 2,4,6
        return ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Seedbed2") ||
            ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Seed1") ||
            ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Seed2") ||
            ((ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Sprout1") ||
            ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Sprout2") ||
            ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm.Contains("Sprout3")) &&
            ep_num % 2 == 0);
    }

    IEnumerator CoShow()
    {
        //20.07.17 sw.park : seedbed2, seed1, seed2 그리고 가을 학기 이후에 Tip버튼 노출 설정
        //Debug.Log("[Debug] 현재 학기 ID : " + serverManager.MeMeUserData.data.level_data[serverManager.selectLevelData].sem_id);
        int index = (int)GameManager._instance.curContent;

        epScrollPanel[index].gameObject.SetActive(false);
        epScrollPanel[index].anchoredPosition = Vector2.zero;
        epTitle[index].text = GetEpisodeText(lessonIdx);

        string com_nm = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].com_nm.ToLower();

        string topicText = string.Empty;
        if (com_nm.Contains("writing"))
        {
            topicText = ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[0].ActInfo.PageInfo.Text_en;
        }
        else if (com_nm.Contains("storytelling"))
        {
            topicText = ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[2].ActInfo.PageInfo.Title_en;
        }
        else if (com_nm.Contains("presentation"))
        {
            topicText = ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[1].ActInfo.PageInfo.Title_en;
        }
        epTopic[index].text = topicText;

        yield return StartCoroutine(ServerManager.Instance.GetServerDate());

        string dueDate = esList == null ? string.Empty : esList[index].remainDate;
        if (dueDate.Equals(string.Empty))
        {
            epDueDate[index].text = "Due Date Error.";
        }
        else if (dueDate.Contains("_overdue"))
        {
            epDueDate[index].text = "Overdue Assignment";
        }
        else
        {
            epDueDate[index].text = "Due Date : " + dueDate.Split('_')[0];
        }

        if (GameManager._instance.curContent.Equals(GameManager.ContentType.Speaking))
        {
            bool isStory = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].com_nm.Contains("Storytelling");
            speakDirectionText.text = storytelling_presentation[isStory ? 0 : 1];
        }

        epScrollPanel[index].gameObject.SetActive(true);

        //bool _submit = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[epArrayIdx].submit_YN.Equals("Y");
        //videoPlayer.gameObject.SetActive(_submit);
        //speakScrollPanel.sizeDelta = _submit ? new Vector2(0, 1500) : new Vector2(0, 1200);
        //if (_submit)
        //    StartCoroutine(Cor_CreateThumbnailImage());
        StartCoroutine(videoPanel[index].VideoListSetting(dirPath));


        if (GameManager._instance.curContent.Equals(GameManager.ContentType.Speaking))
        {
            StartCoroutine(Epimage());
        }
    }

    public void SetDirection()
    {
        directionBtn.onClick.Invoke();
    }

    private void OnEnable()
    {
        videoPlayer.errorReceived += VideoPlayer_errorReceived;
    }
    private void OnDisable()
    {
        videoPlayer.errorReceived -= VideoPlayer_errorReceived;
    }
    private void VideoPlayer_errorReceived(VideoPlayer source, string message)
    {
        Debug.Log("MediaFileError");
        ReDownload(DownLoadType.Video);
    }

    void ReDownload(DownLoadType type)
    {
        ServerManager _server = ServerManager.Instance;
        string _videopath = Application.persistentDataPath + "/Data/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].sem_id
            + "/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].top_cors_id
            + "/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].lv_cd
            + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].g_seq
            + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].com_id
            + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].cjrn_id
            + "/" + ServerManager.Instance.memberData.std_id
            + "/" + "submit"
            ;
        string _directory = "Data/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].sem_id
        + "/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].top_cors_id
        + "/" + _server.MeMeUserData.data.level_data[_server.selectLevelData].lv_cd
        + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].g_seq
        + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].com_id
        + "/" + _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].cjrn_id
        + "/" + _server.memberData.std_id;

        DirectoryInfo di = new DirectoryInfo(_videopath);
        if (di.Exists == false)
        {
            di.Create();
        }
        if (type == DownLoadType.Video)
        {
#if UNITY_EDITOR
            Debug.Log("mp4 Download Episodepanel");
            Debug.Log(_server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].portfolio[0].std_video);
            Debug.Log(_videopath + "/submit.mp4");
            Debug.Log(_directory);
#endif

            _server.BlobStorageDonload(_server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].portfolio[0].std_video, _videopath + "/submit.mp4", _directory, VideoReset);

        }
        else if (type == DownLoadType.Image)
        {
#if UNITY_EDITOR
            Debug.Log("png Download Episodepanel");
            Debug.Log(_server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].portfolio[0].std_video);
            Debug.Log(_videopath + "/submit.png");
            Debug.Log(_directory);
#endif

            string _url = _server.MeMeUserLessonData.data.lesson_data[_server.lessonIdx].portfolio[0].std_video.Replace("mp4", "png");
            _server.BlobStorageDonload(_url, _videopath + "/submit.png", _directory, ImageReset);
        }
    }

    public void VideoReset()
    {
        videoPlayer.url = Application.persistentDataPath + "/" + GetVideoPath();
        videoPlayer.Prepare();
    }

    public void ImageReset()
    {
        StartCoroutine(Cor_ImageReset());
    }

    IEnumerator Cor_ImageReset()
    {
#if UNITY_EDITOR && !UNITY_OSX && !UNITY_IOS
        WWW www = new WWW(dirPath + "/submit/submit.png");
#elif !UNITY_EDITOR && UNITY_ANDROID
        WWW www = new WWW("file:///"+ dirPath + "/submit/submit.png");    
#elif UNITY_IOS || UNITY_OSX
        WWW www = new WWW("file://" + dirPath + "/submit/submit.png");
#endif

        if (System.IO.File.Exists(dirPath + "/submit/submit.png"))
        {
            //이미지
            while (!www.isDone) yield return null;
            if (www.texture != null)
            {
                videoPlayer.GetComponentInChildren<RawImage>().texture = www.texture;
            }
            else
            {
                ReDownload(DownLoadType.Image);
            }
        }
        else
        {
            ReDownload(DownLoadType.Image);
        }
    }

    /// <summary>
    /// DashData(submit완료한 영상 있을경우) 섬네일 셋팅
    /// </summary>
    /// <returns></returns>
    IEnumerator Cor_CreateThumbnailImage()
    {
        yield return Cor_ImageReset();

        // 동영상 정보 획득 도중 파일에 에러가 발생하면 VideoPlayer_errorReceived(VideoPlayer source, string message) 로그 전달받아서 동영상 다운로드 다시 요청 리셋 시작
        if (System.IO.File.Exists(dirPath + "/submit/submit.mp4"))
        {
            videoPlayer.url = dirPath + "/submit/submit.mp4";
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            playBtn.interactable = true;
            while (!videoPlayer.isPlaying)
            {
                yield return null;
            }
            playTime.text = (int)((int)videoPlayer.frameCount / videoPlayer.frameRate) + "s";
            videoPlayer.Stop();
        }
        else
        {
            ReDownload(DownLoadType.Video);
        }
    }

    public void OnClickVideoPlay()
    {
        GameManager._instance.ButtonSound(0);
        GameManager._instance.SelectVideo = GetVideoPath();
        videoPanel[(int)GameManager._instance.curContent].VideoPlay();
    }

    IEnumerator Epimage()
    {

#if UNITY_EDITOR && !UNITY_OSX && !UNITY_IOS
        string _savepath = dirPath;
#elif !UNITY_EDITOR && UNITY_ANDROID
        string _savepath = "file:///" + dirPath;
#elif UNITY_IOS || UNITY_OSX
        string _savepath = "file://" + dirPath;
#endif

        for (int i = 0; i < epiImg.Length - 1; i++)
        {
            yield return null;
            if (i < ImageCount())
            {
                baseEpisodeTexture[i] = null;

                epiImg[i].SetActive(true);

                WWW www;
                Texture temp_texture;
                string com_nm = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].com_nm.ToLower();
                int comInfoIndex = com_nm.Contains("writing") ? 0 : com_nm.Contains("storytelling") ? 2 : 1;

                switch (i)
                {
                    case 0:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image1);
                        break;
                    case 1:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image2);
                        break;
                    case 2:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image3);
                        break;
                    case 3:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image4);
                        break;
                    case 4:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image5);
                        break;
                    default:
                        www = new WWW(_savepath + "/" + ServerManager.Instance.lcsm[lessonIdx].ContentData.ComponentInfo[comInfoIndex].ActInfo.PageInfo.Image1);
                        Debug.Log("epi Img index error");
                        break;
                }

                baseEpisodeTexture[i] = www == null ? null : www.texture;

                int spIndex = ServerManager.Instance.lessonIdx / 2;
                // ★ GameManager._instance.episode_Select
                if (EditEpisodeTexture.Instance.isEditEpisodeTexture(spIndex, i))
                    temp_texture = EditEpisodeTexture.Instance.getEditEpisodeTexture(spIndex, i);
                else
                    temp_texture = baseEpisodeTexture[i];

                epiImg[i].GetComponent<RawImage>().texture = temp_texture;
            }
            else
            {
                epiImg[i].SetActive(false);
            }
        }

        //경민 추가, 그림 이미지를 GamaManager 에 저장해두고 추후 증강씬에서 그림들을 연결해준다.
        GameManager._instance.epiImgTexture = null; //초기화
        GameManager._instance.epiImgTexture = new Texture[ImageCount()];
        for (int i = 0; i < GameManager._instance.epiImgTexture.Length; i++)
        {
            GameManager._instance.epiImgTexture[i] = epiImg[i].GetComponent<RawImage>().texture;
        }
    }

    public void OnClickCreate()
    {
        GameManager._instance.ButtonSound(1);

        if (GameManager._instance.curContent.Equals(GameManager.ContentType.Speaking))
        {
            if (VideoCount >= 10)
            {
                April.Common.MessageBox.ARVideoCreate.Instance.Show(CreateNoCallBack, CreateYesCallBack);
            }
            else
            {
                CheckEpisodeEndingAndAvatarImage();
            }
        }

        epSelectObj[(int)GameManager._instance.curContent].SetActive(true);
    }

    public Texture GetEpisodeImage(int page)
    {
        return epiImg[page].GetComponent<RawImage>().texture;
    }

    private void CreateYesCallBack()
    {
        VideosBtn.onClick.Invoke();
        videoPanel[(int)GameManager._instance.curContent].ShowDeletePanel();
    }

    private void CreateNoCallBack()
    {
        epSelectObj[(int)GameManager._instance.curContent].SetActive(false);
        gameObject.SetActive(true);
    }

    int VideoCount
    {
        get
        {
            int _count = 0;
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(dirPath);
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".mp4") == 0)
                {
                    _count++;
                }
            }

            string _path = Application.persistentDataPath + "/Data/"
                      + ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_id
                + "/" + ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].top_cors_id
                + "/" + ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_cd
                + "/" + ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].g_seq
                + "/" + ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].com_id
                + "/" + ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx].cjrn_id
                + "/" + ServerManager.Instance.MeMeUserData.data.std_name;

            di = new System.IO.DirectoryInfo(_path);
            if (di.Exists)
            {
                foreach (System.IO.FileInfo File in di.GetFiles())
                {
                    if (File.Extension.ToLower().CompareTo(".mp4") == 0)
                    {
                        _count++;
                    }
                }
            }
            return _count;
        }
    }

    private string DueDateText()
    {
        ServerManager.LessonData lesson = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[lessonIdx];

        DateTime t1 = DateTime.Parse(lesson.s_end);
        DateTime t2 = DateTime.Parse(ServerManager.Instance._currentDate);
        t2 = new DateTime(t2.Year, t2.Month, t2.Day, 0, 0, 0);
        TimeSpan ts = t2 - t1;
        string _text;

        if (lesson.submit_YN == "N")
        {
            if (ts.TotalDays > 0)
            {
                _text = "This episode is overdue";
            }
            else if (ts.TotalDays == 0)
            {
                _text = "Due Date:" + t1.ToString("yyyy.MM.dd") + " (D-day)";
            }
            else
            {
                _text = "Due Date:" + t1.ToString("yyyy.MM.dd") + " (D" + ts.Days.ToString() + ")";
            }
        }
        else
        {
            DateTime s1 = DateTime.Parse(lesson.submit_date);
            _text = "Submission Date:" + s1.ToString("yyyy.MM.dd");
        }

        return _text;
    }

    private string GetEpisodeText(int episode_array_idx)
    {
        string _value = ServerManager.Instance.MeMeUserLessonData.data.lesson_data[episode_array_idx].lesson_title;

        return _value;
    }

    public int ImageCount()
    {
        int _num = 0;

        string lv_name = ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].lv_nm;

        if (lv_name.Contains("Rookie1") || lv_name.Contains("Seedbed1"))
        {
            _num = 1;
        }
        else if (lv_name.Contains("Seedbed2"))
        {
            _num = 2;
        }
        else if (lv_name.Contains("Seed1") || lv_name.Contains("Seed2"))
        {
            _num = 3;
        }
        else if (lv_name.Contains("Sprout1") || lv_name.Contains("Sprout2") || lv_name.Contains("Sprout3"))
        {
            _num = 6;
        }
        else if (lv_name.Contains("Sapling1") || lv_name.Contains("Sapling2") || lv_name.Contains("Junior Master1") || lv_name.Contains("Junior Master2"))
        {
            _num = 0;
        }

        return _num;
    }

    string GetVideoPath()
    {
        return ServerManager.Instance.GetDirectoryPath(lessonIdx) + "/submit/submit.mp4";
    }

    public void OnButtonShowEpisodeTip()
    {
        EpisodeTipObj.SetActive(true);
    }

    public void OnButtonChangeEpisodeImage()
    {
        photoType = Photo_mode.Episode;
        objPhotoManagerNoneReset.SetActive(true);
        // ★
        //buttonResetImage.interactable = EditEpisodeTexture.Instance.isEditEpisodeTexture(ServerManager.Instance.Wk_seq, changeImageIdx);
    }

    public void OnButtonSetAvatarPhoto()
    {

#if UNITY_EDITOR
        Debug.Log("   Run>> OnButtonSetAvatarPhoto()  in Editor.");
#else
        photoType = Photo_mode.Avatar;
        objPhotoManagerNoneReset.SetActive(true);
#endif
    }

    public void OnClick_CameraImage()
    {
        NativeCamera.Permission permission = NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                FileInfo file = new FileInfo(path);// 폴더 생성 및 파일 복사
                string photoPath = Application.persistentDataPath + "/MeMePhoto/";
                string photoName = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";

                if (!Directory.Exists(photoPath))
                    Directory.CreateDirectory(photoPath);

                file.CopyTo(photoPath + "/" + photoName, true);
                photoPath = photoPath + "/" + photoName;

                Texture2D NativeImage = NativeCamera.LoadImageAtPath(path, -1, false, false, false);
                if (NativeImage == null)
                {
                    Debug.Log("Couldn't load texture from " + path);
                    return;
                }

                Crop(NativeImage);
            }
        }, -1, true, NativeCamera.PreferredCamera.Rear);
    }

    public void OnClick_GalleryImage()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                FileInfo file = new FileInfo(path);// 폴더 생성 및 파일 복사
                string load_photo_path = Application.persistentDataPath + "/MeMePhoto/";

                if (!Directory.Exists(load_photo_path))
                    Directory.CreateDirectory(load_photo_path);

                file.CopyTo(load_photo_path + "/edit_photo.jpg", true);
                load_photo_path += "/edit_photo.jpg";

                path = load_photo_path;

                Texture2D NativeImage = NativeGallery.LoadImageAtPath(path);
                if (NativeImage == null)
                {
                    Debug.Log("Couldn't load texture from " + path);
                    return;
                }

                Crop(NativeImage);
            }
        }, "Select a image", "image/*");
    }


    float cropAspectRatio = 0;
    bool isOvalSelection;

    public void Crop(Texture photo_image)
    {
        if (photoType == Photo_mode.Episode)
        {
            cropAspectRatio = 1.33f;
            isOvalSelection = false;
        }
        else
        {
            cropAspectRatio = 0.77f;
            isOvalSelection = true;
        }

        photoCropper.gameObject.SetActive(true);
        StartCoroutine(GetPhotoAndCrop(photo_image));
    }


    private IEnumerator GetPhotoAndCrop(Texture photo_image)
    {
        yield return new WaitForEndOfFrame();

        photoCropper.Show(photo_image, (bool result, Texture originalImage, Texture2D croppedImage) =>
        {
            // If screenshot was cropped successfully
            if (result)
            {
                // Assign cropped texture to the RawImage
                if (photoType == Photo_mode.Episode)
                {
                    // ★
                    EditEpisodeTexture.Instance.setEditEpisodeTexture(ServerManager.Instance.lessonIdx, epiImg.Length - 1, croppedImage);
                    Texture2D epiTex = EditEpisodeTexture.Instance.getEditEpisodeTexture(ServerManager.Instance.lessonIdx, epiImg.Length - 1);

                    epiImg[epiImg.Length - 1].GetComponent<RawImage>().texture = epiTex;
                    GameManager._instance.epiImgTexture[GameManager._instance.epiImgTexture.Length - 1] = epiTex;

                    CheckEpisodeEndingAndAvatarImage();
                    photoCropper.gameObject.SetActive(false);
                }
                else
                {
                    int model_type = (int)GameManager._instance.objType;
                    Sprite getSprite = EditEpisodeTexture.Instance.MakeAvatarFaceSprite(model_type, croppedImage);

                    imageAvatarFace.sprite = getSprite;
                    imageAvatarFace.color = ChangeAlpha(imageAvatarFace.color, false);
                    CheckEpisodeEndingAndAvatarImage();

                    //settingUserFace.sprite = getSprite;

                    EditEpisodeTexture.Instance.MakeAvatarFaceInSpine(model_type, croppedImage);

                    photoCropper.gameObject.SetActive(false);
                }
            }
            else
            {
                //croppedImageHolder.enabled = false;
            }
        },
        settings: new ImageCropper.Settings()
        {
            ovalSelection = isOvalSelection,
            autoZoomEnabled = false,
            imageBackground = Color.clear, // transparent background
            selectionMinAspectRatio = cropAspectRatio,
            selectionMaxAspectRatio = cropAspectRatio
        },
        croppedImageResizePolicy: (ref int width, ref int height) =>
        {
            // uncomment lines below to save cropped image at half resolution
            //width /= 2;
            //height /= 2;
        });
    }

    public void CheckEpisodeEndingAndAvatarImage()
    {
        RawImage ri = epiImg[epiImg.Length - 1].GetComponent<RawImage>();
        bool isEpiImg = ri.texture == null;
        btn_endingPage.sprite = isEpiImg ? img_btnAdd : img_btnEdit;
        ri.color = ChangeAlpha(ri.color, isEpiImg);

        bool isAvatarImg = imageAvatarFace.sprite == null;
        btn_addAvatarFace.sprite = isAvatarImg ? img_btnAdd : img_btnEdit;
        imageAvatarFace.color = ChangeAlpha(imageAvatarFace.color, isAvatarImg);
    }


    private Color ChangeAlpha(Color img, bool isClear)
    {
        Color col = new Color(img.r, img.g, img.b, isClear ? 0 : 1);
        return col;
    }

    public void OnClickPortfolio()
    {
        April.Common.MessageBox.ARRotete.Instance.Show(null);
    }
}
