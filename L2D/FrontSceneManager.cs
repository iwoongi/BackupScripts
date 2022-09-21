using DanielLochner.Assets.SimpleScrollSnap;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FrontSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject introPanel, searchPanel, categoryPanel;
    [SerializeField] private Transform mainPivot;
    [SerializeField] private Image introLogo;
    [SerializeField] private TMP_Text introText;
    [SerializeField] private Toggle tggAR, tggTracking, tggFavor;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundMode;

    private Button[] mainBtns;

    public void SetFavorChoosed(bool on)
    {
        tggFavor.isOn = on;
    }

    public void EnableCategoryPanel()
    {
        categoryPanel.SetActive(true);
        searchPanel.SetActive(false);
    }

    public void OnClickToggles()
    {
        audioSource.PlayOneShot(soundMode);
    }

    private void Start()
    {
        Init();
    }

    private void OnDisable()
    {
        tggAR?.onValueChanged.RemoveAllListeners();
        tggTracking.onValueChanged.RemoveAllListeners();
        tggFavor.onValueChanged.RemoveAllListeners();

        for (int i = 0; i < mainBtns.Length; i++)
        {
            Button btn = mainBtns[i];
            btn.onClick.RemoveAllListeners();
        }
    }

    private void Init()
    {
        if (introPanel.activeSelf)
            introPanel.SetActive(false);

        PlayDirManager.instance.EnableModel(false);

        mainBtns = mainPivot.GetComponentsInChildren<Button>();
        for (int i = 0; i < mainBtns.Length; i++)
        {
            Button btn = mainBtns[i];
            btn.onClick.AddListener(() => OnClickMain(btn));
        }

        SetToggleMode(tggTracking);
        tggFavor.onValueChanged.AddListener(delegate { OnClickFavor(); });
        if (tggAR != null) SetToggleMode(tggAR);

        Data.GetMusicScroll().Init();

        searchPanel.SetActive(false);
    }

    private void SetToggleMode(Toggle tgg)
    {
        tgg.isOn = tgg.name.Equals("tgg_mode_ar")
                 ? PlayerPrefs.GetInt("modeAR", 0).Equals(1)
                 : PlayerPrefs.GetInt("modeTracking", 0).Equals(1);
    }

    private void OnClickMain(Button btn)
    {
        switch (btn.name)
        {
            case "btn_Start":
                OnClickStart();

                break;
            case "btn_search":
                Data.GetMusicScroll().sampleAudio.Stop();
                searchPanel.SetActive(true);

                break;
            case "btn_category":
                Data.GetMusicScroll().sampleAudio.Stop();
                categoryPanel.SetActive(true);

                break;
        }
    }

    private void OnClickFavor()
    {
        Data.GetMusicScroll().SetFavorData(tggFavor.isOn);
    }

    private void OnClickStart()
    {
        PlayerPrefs.SetInt("modeTracking", tggTracking.isOn ? 1 : 0);
        if (tggAR != null) PlayerPrefs.SetInt("modeAR", tggAR.isOn ? 1 : 0);

        SetDataBeforeStart();
    }

    private void SetDataBeforeStart()
    {
        int curIndex = Data.GetMusicScroll().musicScroll.TargetPanel;
        DanceDataManager.instance.curIndex = curIndex;

        if (DanceDataManager.instance.GetCurMusic().timeline == null)
        {
            ToastManager.instance.RunToast("미구현! 업데이트 예정입니다!");
            return;
        }

        DanceDataManager.instance.isTracking = tggTracking.isOn;
        if (tggAR != null) DanceDataManager.instance.isAR = tggAR.isOn;

        StartCoroutine(LoadPlayScene());
    }

    // 0:Permission, 1:Splash, 2:FrontScene, 3:PlayerScene, 4:AR
    private IEnumerator LoadPlayScene()
    {
        int sceneNum = DanceDataManager.instance.isAR ? 4 : 3;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneNum);
        op.allowSceneActivation = false;

        //Set Intro Panel
        introLogo.sprite = Data.GetMusicScroll().imgAlbum.sprite;
        introText.text = DanceDataManager.instance.GetCurMusicText();
        introPanel.SetActive(true);

        bool isLoading = true;
        while (isLoading)
        {
            if (op.progress >= 0.9f)
                isLoading = false;

            yield return null;
        }

        yield return new WaitForSeconds(1.3f);

        Data.GetMusicScroll().sampleAudio.Stop();
        op.allowSceneActivation = true;
    }
}