using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public Dictionary<string, List<Sprite[]>> NFTList { get => nftList; }
    public string curNFTName { get; set; }
    public NFTBox savedData;

    private Dictionary<string, List<Sprite[]>> nftList;
    private string exPath;

    private readonly string[] nftName =
        { "monkey", "cat", "owl", "dog", "penguin", "cheetah", "sheep", "bear", "fox", "deer" };
    /// <summary>
    /// Hierarchy의 순서도 배열과 동일한 순서로 맞추기
    /// </summary>
    private readonly string[] partsName =
        { "tail", "body", "propHand", "hand", "face", "mouth", "eye", "hat", "eyebrow", "propFace" };
    private readonly int partsMax = 6;
    private readonly int dataMax = 20;


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

        exPath = Path.Combine(Application.persistentDataPath, "savedNFT.json");
        StartCoroutine(InitData_LoadMainScene());
    }

    private IEnumerator InitData_LoadMainScene()
    {
        LoadLastData();

        Screen.SetResolution(1280, 720, false);
        SplashManager spManager = FindObjectOfType<SplashManager>();
        curNFTName = "monkey";

        nftList = new Dictionary<string, List<Sprite[]>>();
        spManager?.InitSlider(nftName.Length);
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < nftName.Length; i++)
        {
            List<Sprite[]> nftParts = new List<Sprite[]>();
            string animal = nftName[i];

            for (int j = 0; j < partsName.Length; j++)
            {
                Sprite[] loadSprite = new Sprite[partsMax];

                for (int k = 0; k < partsMax; k++)
                {
                    string path = "Sprites/parts/" + animal + "_" + partsName[j] + "_" + k.ToString("00");

                    loadSprite[k] = Resources.Load<Sprite>(path) ?? null;
                }

                nftParts.Add(loadSprite);
                spManager?.UpdateSlider(0.11f);
            }
            nftList.Add(nftName[i], nftParts);
        }

        spManager?.FinLoading();
        yield return new WaitForEndOfFrame();

        //result = new List<int[]>();
        //int[] order = new int[4];
        //Thread t = new Thread(delegate () { repeatPermutation(order, 0); });

        //t.Start();
        //t.Join();

        //print(result.Count);
        //string ab = "";
        //for (int i = 0; i < result.Count; i++)
        //{
        //    for (int j = 0; j < result[i].Length; j++)
        //    {
        //        ab += result[i][j] + " ";
        //    }
        //    ab += "\n";
        //}

        //print(ab);
    }

    List<int[]> result;
    int[] aa = { 0, 1, 2, 3, 4/*, 5, 6, 7, 8, 9*/ };

    private void repeatPermutation(int[] perm, int depth)
    {
        if (depth == perm.Length)
        {
            //print("b " + perm.Length + " " + depth);
            int[] ab = new int[perm.Length];
            string b = "";
            for (int i = 0; i < perm.Length; i++)
            {
                b += perm[i] + " ";
                ab[i] = perm[i];
            }
            result.Add(ab);
            print(b);

            return;
        }

        for (int i = 0; i < aa.Length; i++)
        {
            perm[depth] = aa[i];
            //print("a1 " + i + " " + perm[depth]);
            repeatPermutation(perm, depth + 1);
        }
    }

    private void LoadLastData()
    {
        if (File.Exists(exPath))
        {
            string data = File.ReadAllText(exPath);
            try
            {
                savedData = JsonUtility.FromJson<NFTBox>(data);
            }
            catch
            {
                savedData = new NFTBox();
                savedData.box = new List<GenBox>();
            }
        }
        else
        {
            savedData = new NFTBox();
            savedData.box = new List<GenBox>();
        }

        Data.GetRGen().genBox = savedData.box;

        List<GenBox> gb = Data.GetRGen().genBox;
        for (int i = 0; i < gb.Count; i++)
        {
            if (gb[i].fixImg.Count > 0)
            {
                Data.GetFPop().RunPopup("세트가 완성되지 않은 동물이 있습니다.\n미완성 데이터를 불러오겠습니까?");
                break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        List<GenBox> gb = Data.GetRGen().genBox;

        if (gb.Count == 0)
            return;

        for (int i = 0; i < gb.Count; i++)
        {
            int index = gb.Count - 1 - i;
            List<FixImage> fi = gb[index].fixImg;

            if (fi.Count > 1000)
            {
                fi.RemoveRange(0, 1000);
            }
            else if (fi.Count == 1000)
            {
                gb.RemoveAt(index);
            }
        }

        savedData.box = gb;

        string json = JsonUtility.ToJson(savedData);
        File.WriteAllText(exPath, json);
    }
}

[Serializable]
public class NFTBox
{
    public List<GenBox> box;
}