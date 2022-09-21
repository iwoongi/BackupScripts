using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject prefabResult;
    [SerializeField] private Transform contentPivot;
    [SerializeField] private Toggle tggFavor;
    [SerializeField] private Button btnSearch, btnCategory, btnClose;

    private readonly string frontColorTag = "<color=#FF7EF1>";
    private readonly string endColorTag = "</color>";

    private List<string> resultList;
    private InputField ipf;

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        ipf.onValueChanged.RemoveAllListeners();
        btnSearch.onClick.RemoveAllListeners();
        btnCategory.onClick.RemoveAllListeners();
        btnClose.onClick.RemoveAllListeners();
    }

    private void Init()
    {
        ResetSearchResult();

        ipf = GetComponentInChildren<InputField>();
        ipf.text = string.Empty;
        ipf.onValueChanged.AddListener(delegate
        {
            SearchMusic();
        });

        tggFavor.isOn = false;
        tggFavor.onValueChanged.AddListener(delegate
        {
            SearchMusic();
        });
        btnSearch.onClick.AddListener(() => SearchMusic());
        btnCategory.onClick.AddListener(() => Data.GetFront().EnableCategoryPanel());
        btnClose.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void SearchMusic()
    {
        ResetSearchResult();

        if (ipf.text.Equals(string.Empty))
        {
            if (tggFavor.isOn) SetFavor();

            return;
        }

        string word = ipf.text.ToLower();

        for (int i = 0; i < DanceDataManager.instance.musicList.music.Count; i++)
        {
            string title = DanceDataManager.instance.musicList.music[i].title;
            string artist = DanceDataManager.instance.musicList.music[i].artist;

            if (title.Length < word.Length || artist.Length < word.Length) continue;

            string checkTitle = title.Substring(0, word.Length);
            string checkArtist = artist.Substring(0, word.Length);
            bool isFind = false;

            if (checkTitle.ToLower().Equals(word))
            {
                title = title.Replace(checkTitle, frontColorTag + checkTitle + endColorTag);
                isFind = true;
            }
            if (checkArtist.ToLower().Equals(word))
            {
                artist = artist.Replace(checkArtist, frontColorTag + checkArtist + endColorTag);
                isFind = true;
            }

            if (tggFavor.isOn)
            {
                if (DanceDataManager.instance.musicList.music[i].favor && isFind)
                    resultList.Add(artist + " - " + title);
            }
            else
            {
                if (isFind)
                    resultList.Add(artist + " - " + title);
            }
        }

        CreateResultButtons();
    }

    private void CreateResultButtons()
    {
        for (int i = 0; i < resultList.Count; i++)
        {
            Button btn;

            if (i < contentPivot.childCount)
            {
                btn = contentPivot.GetChild(i).GetComponent<Button>();
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn = Instantiate(prefabResult, contentPivot, false).GetComponent<Button>();
                btn.onClick.AddListener(() => OnClick(btn));
            }

            btn.GetComponentInChildren<Text>().text = resultList[i];
            btn.name = RemoveTag(resultList[i]);
        }
    }

    private void SetFavor()
    {
        for (int i = 0; i < DanceDataManager.instance.musicList.music.Count; i++)
        {
            if (DanceDataManager.instance.musicList.music[i].favor)
            {
                string title = DanceDataManager.instance.musicList.music[i].title;
                string artist = DanceDataManager.instance.musicList.music[i].artist;

                resultList.Add(artist + " - " + title);
            }
        }

        CreateResultButtons();
    }

    private void OnClick(Button btn)
    {
        if (btn.name == null)
            ToastManager.instance.RunToast("TEXT ERROR");

        Data.GetMusicScroll().SearchMusic(btn.name);

        gameObject.SetActive(false);
    }

    private void ResetSearchResult()
    {
        if (resultList == null)
            resultList = new List<string>();
        else
            resultList.Clear();

        for (int i = 0; i < contentPivot.childCount; i++)
        {
            contentPivot.GetChild(i).gameObject.SetActive(false);
        }
    }

    private string RemoveTag(string send)
    {
        send = send.Replace(frontColorTag, string.Empty);
        send = send.Replace(endColorTag, string.Empty);
        return send;
    }
}
