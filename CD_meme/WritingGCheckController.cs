using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class WritingGCheckController : MonoBehaviour
{
    private WritingSelectManager wsm;
    private CustomInputField_TMP ipf;
    private readonly string frontColorTag = "<link=*><color=#ff0000><U>";
    private readonly string endColorTag = "</link></U></color>";

    [SerializeField] private List<GCheckData> gCheckData;

    public bool isCorrect;
    public string finalText, initText;


    private void Awake()
    {
        ipf = this.GetComponent<CustomInputField_TMP>();
        ipf.enabled = false;
    }

    public void Init(string msg, WritingSelectManager wsm)
    {
        this.wsm = wsm;
        isCorrect = false;
        finalText = msg;
        initText = msg;

        ipf.text = finalText;

        StartGCheck();
    }

    public string GetInputFieldText()
    {
        return ipf.text;
    }

    public void ResetText()
    {
        ipf.text = initText;
    }

    public void ClearText()
    {
        ipf.text = string.Empty;
    }

    public void StartGCheck()
    {
        if (ipf.text.Equals(string.Empty))
        {
            //Debug.Log("   EMPTY TEXT!!");
            return;
        }

        if (ipf.text.Equals(finalText) && isCorrect)
        {
            //Debug.Log("   SAME TEXT!!");
            return;
        }

        if (gCheckData == null)
            gCheckData = new List<GCheckData>();

        string sendText = CheckRedText(ipf.text);
        //Debug.Log("  send:  " + sendText);

        StartCoroutine(RequestToPlatform(sendText, response =>
        {
            StartCoroutine(SendGCheckAPI(sendText, response));

            MatchCollection matches = Regex.Matches(response, "Mistakes");
            int misCnt = matches.Count;

            print("MisCount  " + misCnt + "\n" + response);

            if (misCnt.Equals(0))
            {
                isCorrect = true;
                return;
            }

            if (gCheckData != null)
            {
                gCheckData.Clear();
            }

            JObject json = JObject.Parse(response);
            for (int i = 0; i < misCnt; i++)
            {
                GCheckData gcd = new GCheckData
                {
                    type = int.Parse(json["Corrections"][i]["Type"].ToString()),
                    startIndex = int.Parse(json["Corrections"][i]["Mistakes"][0]["From"].ToString()),
                    endIndex = int.Parse(json["Corrections"][i]["Mistakes"][0]["To"].ToString()),
                    suggestions = json["Corrections"][i]["Suggestions"][0]["Text"].ToString(),
                    lrnCatId = int.Parse(json["Corrections"][i]["Suggestions"][0]["LrnCatId"].ToString()),
                    lrnFrg = CheckLrnFrg(json["Corrections"][i]["LrnFrg"]),
                    lrnStartIndex = CheckLrnIndex(json["Corrections"][i]["LrnFrgOrigIndxs"]),
                    lrnEndIndex = CheckLrnIndex(json["Corrections"][i]["LrnFrgOrigIndxs"])
                };
                gcd.errorText = sendText.Substring(gcd.startIndex, gcd.endIndex - gcd.startIndex + 1);
                gcd.errorTag = frontColorTag.Replace("*", i.ToString());

                gCheckData.Add(gcd);
            }

            for (int i = 0; i < gCheckData.Count; i++)
            {
                int index = gCheckData.Count - i - 1;

                sendText = sendText.Insert(gCheckData[index].endIndex + 1, endColorTag);
                sendText = sendText.Insert(gCheckData[index].startIndex, gCheckData[index].errorTag);
            }

            isCorrect = false;
            ipf.text = sendText;
            finalText = sendText;
        }));
    }

    private string CheckLrnFrg(JToken jt)
    {
        return jt == null ? string.Empty : jt.ToString();
    }

    private int CheckLrnIndex(JToken jt)
    {
        return jt.ToString() == "[]" ? 0 : int.Parse(jt.ToString());
    }

    public string CheckRedText(string sendText)
    {
        if (gCheckData.Count.Equals(0))
            return sendText;

        for (int i = 0; i < gCheckData.Count; i++)
        {
            sendText = sendText.Replace(gCheckData[i].errorTag, string.Empty);
            sendText = sendText.Replace(endColorTag, string.Empty);
        }

        return sendText;
    }

    private int clickedIndex;
    public void OnClickErrorText(int index)
    {
        clickedIndex = index;
        wsm.pop_comment.Init(gCheckData[index], SetChangeText);
        wsm.pop_comment.gameObject.SetActive(true);
    }

    private void SetChangeText()
    {
        string temp = CheckRedText(ipf.text);
        temp = temp.Remove(gCheckData[clickedIndex].startIndex, gCheckData[clickedIndex].endIndex - gCheckData[clickedIndex].startIndex + 1);
        temp = temp.Insert(gCheckData[clickedIndex].startIndex, gCheckData[clickedIndex].suggestions);

        ipf.text = temp;
        StartGCheck();
    }

    private IEnumerator SendGCheckAPI(string text, string json)
    {
        WWWForm form = new WWWForm();
        form.AddField("sem_id", ServerManager.Instance.MeMeUserData.data.level_data[ServerManager.Instance.selectLevelData].sem_id);
        form.AddField("std_id", ServerManager.Instance.memberData.std_id);
        form.AddField("g_seq", ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].g_seq);
        form.AddField("com_id", ServerManager.Instance.MeMeUserLessonData.data.lesson_data[ServerManager.Instance.lessonIdx].com_id);
        form.AddField("text", text);
        form.AddField("ginger_data", json);

        UnityWebRequest uwr = UnityWebRequest.Post(ServerManager.Instance._BASE_HTTPS_URL_MeMe_GINGERINOUTLOG, form);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError || uwr.isHttpError)
        {
            Debug.Log(uwr.error);
        }
        //else
        //{
        //    Debug.Log(uwr.downloadHandler.text);
        //}
    }

    //한국어 입력여부 체크
    public void CheckKor()
    {
        if (Regex.IsMatch(ipf.text, @"[ㄱ-ㅎ가-힣]"))
        {
            April.Common.MessageBox.ARToast.Instance.SystemShow(April.Common.MessageBox.SystemToastState.notUseKor);
            ipf.text = finalText;
        }
    }

    private IEnumerator RequestToPlatform(string text, Action<string> response)
    {
        string url = "";
        string endUrl = "&lang=US";
        string combineUrl = url + text + endUrl;

        UnityWebRequest request = UnityWebRequest.Get(combineUrl);

        yield return request.SendWebRequest();

        //에러 발생시
        if (request.isNetworkError)
        {
            Debug.Log("Error While Sending: " + request.error);
            response(request.error);
        }
        else
        {
            //Debug.Log("Received: " + request.downloadHandler.text);
            response(request.downloadHandler.text);
        }
    }
}

[Serializable]
public class GCheckData
{
    public int type;
    public int startIndex;
    public int endIndex;
    public string suggestions;
    public int lrnCatId;
    public string lrnFrg;
    public int lrnStartIndex;
    public int lrnEndIndex;
    public string errorText;
    public string errorTag;
}
