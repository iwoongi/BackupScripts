using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class RandomGenerator : MonoBehaviour
{
    public bool isBreak;
    public List<GenBox> genBox;

    [SerializeField] private RectTransform imgPivot;
    [SerializeField] private Sprite[] gradeImage;
    [SerializeField] private Image bg, grade;

    private WaitForEndOfFrame waitForEndOfFrame;
    private readonly int randomMax = 5;
    private ClickImageController[] parts;
    private Sprite[] ssrProps;
    private int[] curNum;
    private int editBoxIndex, editImgIndex;
    private Color curCol;

    public bool IsEdit { get; set; }
    public bool IsLoad { get; set; }
    public RectTransform GetImageRect() { return imgPivot; }

    public void OnClickConfirmEdit()
    {
        StartCoroutine(ConfirmEditImage());
    }

    public void OnClickAnimal()
    {
        SetImageLayer(DataManager.instance.curNFTName, curCol);
    }

    public void SetEditeImage(string index)
    {
        IsEdit = true;

        string[] indexSt = index.Split('_');
        editBoxIndex = int.Parse(indexSt[0]);
        editImgIndex = int.Parse(indexSt[1]);

        curNum = genBox[editBoxIndex].fixImg[editImgIndex].code;
        genBox[editBoxIndex].fixImg[editImgIndex].isSaved = false;
        SetImageLayer(genBox[editBoxIndex].animalName, genBox[editBoxIndex].fixImg[editImgIndex].color);
    }

    //public void SetGradeAll(int percent, string placeStyle)
    //{
    //    for (int i = 0; i < genBox.Count; i++)
    //    {
    //        GenBox gi = genBox[i];
    //        int srCnt = Mathf.FloorToInt(gi.fixImg.Count * (percent / 100f));

    //        if (placeStyle.Equals("2"))
    //        {
    //            List<int> temp = new List<int>();
    //            for (int j = 0; j < gi.fixImg.Count; j++)
    //            {
    //                temp.Add(j);
    //            }

    //            int[] tempIndex = new int[srCnt];
    //            for (int j = 0; j < srCnt; j++)
    //            {
    //                int pickNum = UnityEngine.Random.Range(0, temp.Count);
    //                tempIndex[j] = temp[pickNum];
    //                temp.RemoveAt(pickNum);
    //            }

    //            for (int j = 0; j < srCnt; j++)
    //            {
    //                gi.fixImg[tempIndex[j]].grade = 1;
    //            }
    //        }
    //        else
    //        {
    //            //print(gi.fixImg.Count + "   " + percent + "%   " + srCnt);
    //            for (int j = 0; j < srCnt; j++)
    //            {
    //                int index = placeStyle.Equals("0") ? j : gi.fixImg.Count - 1 - j;
    //                gi.fixImg[index].grade = 1;

    //                //print(gi.fixImg[index].imgNum + "   " + gi.fixImg[index].grade);
    //            }
    //        }
    //    }
    //    Data.GetUI().RunResultPanel();
    //}

    //public void SetReplaceByGrade()
    //{
    //    for (int i = 0; i < genBox.Count; i++)
    //    {
    //        GenBox gi = genBox[i];

    //        for (int j = 0; j < gi.fixImg.Count; j++)
    //        {
    //            FixImage pickSR = new FixImage();
    //            int pickIndex = 0;
    //            bool isExist = false;

    //            for (int k = j; k < gi.fixImg.Count; k++)
    //            {
    //                FixImage fi = gi.fixImg[k];
    //                if (fi.grade.Equals(1))
    //                {
    //                    pickSR = fi;
    //                    pickIndex = k;
    //                    isExist = true;
    //                    break;
    //                }
    //            }

    //            if (isExist)
    //            {
    //                int temp = gi.fixImg[j].imgNum;
    //                gi.fixImg[j].imgNum = pickSR.imgNum;
    //                gi.fixImg[pickIndex].imgNum = temp;

    //                gi.fixImg[j].isSaved = false;
    //                pickSR.isSaved = false;

    //                genBox[i].fixImg[pickIndex] = gi.fixImg[j];
    //                genBox[i].fixImg[j] = pickSR;

    //            }
    //        }
    //    }
    //    Data.GetUI().RunResultPanel();
    //    Data.GetUI().SetScrollTop(true);
    //}

    public void RunGenerateImage()
    {
        isBreak = false;

        if (IsEdit)
        {
            StartCoroutine(GenerateEditImage());
        }
        else
        {
            StartCoroutine(GenerateImage(() =>
            {
                Data.GetUI().SetGeneratingPanel(false);
                Data.GetUI().RunResultPanel();
                Data.GetUI().SetScrollTop(false);
                DisableMarkers(true);
            }));
        }
    }

    public IEnumerator RunSaveAllImage()
    {
        Data.GetUI().SetResultPanel(false);
        //Block interactions during generating
        Data.GetUI().SetGeneratingPanel(true);
        DisableMarkers(false);
        grade.gameObject.SetActive(true);

        for (int i = 0; i < genBox.Count; i++)
        {
            GenBox gi = genBox[i];
            Data.GetUI().SetGeneratingTitle("Save \"" + gi.animalName + "\" Images...");

            for (int j = 0; j < gi.fixImg.Count; j++)
            {
                FixImage fi = gi.fixImg[j];
                if (fi.isSaved)
                    continue;

                PathInfo pi = Data.GetCapture().CheckDirectory(gi.animalName, fi.imgNum, true);
                yield return pi;

                curNum = fi.code;
                curCol = fi.color;

                SetImageLayer(gi.animalName, curCol);
                yield return waitForEndOfFrame;

                for (int k = 0; k < gradeImage.Length; k++)
                {
                    //ssr전용 handprop 적용
                    if (k.Equals(gradeImage.Length - 1))
                        parts[2].partImage.sprite = ssrProps[Mathf.FloorToInt(j / 20)];

                    grade.sprite = gradeImage[k];

                    Texture2D tempTex = null;
                    yield return StartCoroutine(Data.GetCapture().TakeScreenShot(result =>
                    {
                        tempTex = result;
                        Data.GetUI().SetGeneratingText(j + "/" + gi.fixImg.Count);
                    }));

                    byte[] tex = tempTex.EncodeToPNG();
                    yield return tex;

                    Thread t = new Thread(() => Data.GetCapture().SaveToPng(pi, k, tex));
                    t.Start();
                    t.Join();
                }

                fi.isSaved = true;
            }
            yield return waitForEndOfFrame;

            Resources.UnloadUnusedAssets();
        }

        Data.GetUI().SetGeneratingPanel(false);
        Data.GetUI().RunResultPanel();
        DisableMarkers(true);
        grade.gameObject.SetActive(false);

        Data.GetCapture().OpenInWinFileBrowser();
    }


    private void Awake()
    {
        parts = imgPivot.GetComponentsInChildren<ClickImageController>();
        curNum = new int[parts.Length];
        curCol = Color.white;

        IsEdit = false;
        grade.gameObject.SetActive(false);
        waitForEndOfFrame = new WaitForEndOfFrame();
        ssrProps = Resources.LoadAll<Sprite>("Sprites/handPropSSR");
    }

    private int SetRandomNum() { return UnityEngine.Random.Range(0, randomMax); }
    private int SetCurNum(int i) { return curNum[i]; }

    private IEnumerator GenerateImage(Action done)
    {
        int genCnt = Data.GetUI().ImageCnt;
        //Block interactions during generating
        Data.GetUI().SetGeneratingPanel(true);
        DisableMarkers(false);

        int curBoxIndex = -1;
        PathInfo pi = Data.GetCapture().CheckDirectory(DataManager.instance.curNFTName, -1, IsLoad);
        List<FixImage> fixImg = new List<FixImage>();
        for (int i = 0; i < genBox.Count; i++)
        {
            if (genBox[i].animalName.Equals(DataManager.instance.curNFTName))
            {
                fixImg = genBox[i].fixImg;
                curBoxIndex = i;
                break;
            }
        }

        Data.GetUI().SetGeneratingTitle("Generate \"" + DataManager.instance.curNFTName + "\" Images...");
        int fixLastIndex = fixImg.Count == 0 ? pi.fileNum : fixImg[fixImg.Count - 1].imgNum + 1;

        for (int j = 0; j < genCnt; j++)
        {
            bool isSame = false;
            int[] mixNum;

            do
            {
                mixNum = new int[curNum.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    mixNum[i] = SetRandomNum();
                }

                for (int i = 0; i < fixImg.Count; i++)
                {
                    if (mixNum.SequenceEqual(fixImg[i].code))
                    {
                        isSame = true;
                    }
                }
            } while (isSame);

            curNum = mixNum;
            curCol = UnityEngine.Random.ColorHSV(0f, 1f, 0.1f, 1f, 0.5f, 1f);

            SetImageLayer(DataManager.instance.curNFTName, curCol);
            yield return waitForEndOfFrame;

            yield return StartCoroutine(Data.GetCapture().TakeScreenShot(result =>
            {
                FixImage img = new FixImage()
                {
                    isSaved = false,
                    imgNum = fixLastIndex + j,
                    code = curNum,
                    color = curCol,
                    texture = result
                };
                fixImg.Add(img);
                Data.GetUI().SetGeneratingText(j + "/" + genCnt);
            }));
            yield return waitForEndOfFrame;

            if (isBreak)
            {
                KeepGenData(curBoxIndex, pi, fixImg);
                done();
                yield break;
            }
        }

        KeepGenData(curBoxIndex, pi, fixImg);
        done();
    }

    private void KeepGenData(int curBoxIndex, PathInfo pi, List<FixImage> fixImg)
    {
        if (curBoxIndex == -1)
        {
            GenBox gb = new GenBox()
            {
                animalName = DataManager.instance.curNFTName,
                path = pi.path,
                fixImg = fixImg
            };
            genBox.Add(gb);
        }
        else
        {
            genBox[curBoxIndex].path = pi.path;
            genBox[curBoxIndex].fixImg = fixImg;
        }
    }

    private void SetImageLayer(string animal, Color color)
    {
        if (DataManager.instance.NFTList.ContainsKey(animal))
        {
            for (int i = 0; i < parts.Length; i++)
            {
                DataManager.instance.NFTList.TryGetValue(animal, out List<Sprite[]> value);

                parts[i].partImage.sprite = value[i][curNum[i]] ?? gradeImage[0];
            }

            bg.color = color;
        }
    }


    #region EDIT_IMAGE_N_GENERATE
    private IEnumerator GenerateEditImage()
    {
        bool isSame = true;
        int[] mixNum;
        List<FixImage> fixImg = genBox[editBoxIndex].fixImg;
        List<int[]> compareList = new List<int[]>();

        int choiceCnt = CheckChoiceCount();

        for (int i = 0; i < fixImg.Count; i++)
        {
            for (int j = 0; j < parts.Length; j++)
            {
                if (choiceCnt != 0)
                {
                    if (ReturnIsChoice(j)) continue;

                    if (fixImg[i].code[j] != curNum[j])
                    {
                        isSame = false;
                        break;
                    }
                }
                else
                {
                    if (!ReturnIsLock(j)) continue;

                    if (fixImg[i].code[j] != curNum[j])
                    {
                        isSame = false;
                        break;
                    }
                }
            }

            if (isSame)
            {
                compareList.Add(fixImg[i].code);
            }

            isSame = true;
        }

        //print(compareList.Count);
        if (compareList.Count == 1) compareList.RemoveAt(0);
        isSame = false;

        do
        {
            mixNum = new int[curNum.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (choiceCnt != 0)
                {
                    mixNum[i] = ReturnIsChoice(i) ? SetRandomNum() : SetCurNum(i);
                }
                else
                {
                    mixNum[i] = ReturnIsLock(i) ? SetCurNum(i) : SetRandomNum();
                }
            }

            for (int i = 0; i < compareList.Count; i++)
            {
                if (mixNum.SequenceEqual(compareList[i]))
                {
                    isSame = true;
                }
            }
        } while (isSame);

        curNum = mixNum;

        SetImageLayer(genBox[editBoxIndex].animalName, bg.color);
        yield return waitForEndOfFrame;
    }

    private IEnumerator ConfirmEditImage()
    {
        DisableMarkers(false);
        yield return StartCoroutine(Data.GetCapture().TakeScreenShot(result =>
        {
            FixImage img = new FixImage()
            {
                isSaved = false,
                imgNum = genBox[editBoxIndex].fixImg[editImgIndex].imgNum,
                code = curNum,
                color = bg.color,
                texture = result
            };
            genBox[editBoxIndex].fixImg[editImgIndex] = img;

            Data.GetUI().SetGeneratingPanel(false);
            Data.GetUI().RunResultPanel();
            DisableMarkers(true);
        }));
    }

    private bool ReturnIsChoice(int index)
    {
        return parts[index].isChoice;
    }

    private bool ReturnIsLock(int index)
    {
        return parts[index].isLock;
    }

    public int CheckChoiceCount()
    {
        int cnt = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (ReturnIsChoice(i))
                cnt += 1;
        }

        return cnt;
    }

    public int CheckLockCount()
    {
        int cnt = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (ReturnIsLock(i))
                cnt += 1;
        }

        return cnt;
    }
    #endregion


    #region FIRST_LOAD_GENBOX
    public IEnumerator GetTextureLastData()
    {
        Data.GetUI().SetGeneratingPanel(true);

        for (int i = 0; i < genBox.Count; i++)
        {
            List<FixImage> fis = genBox[i].fixImg;
            Data.GetUI().SetGeneratingTitle("Load \"" + genBox[i].animalName + "\" Images...");

            for (int j = 0; j < fis.Count; j++)
            {
                FixImage fi = fis[j];
                if (DataManager.instance.NFTList.ContainsKey(genBox[i].animalName))
                {
                    for (int k = 0; k < parts.Length; k++)
                    {
                        DataManager.instance.NFTList.TryGetValue(genBox[i].animalName, out List<Sprite[]> value);

                        parts[k].partImage.sprite = value[k][fi.code[k]] ?? gradeImage[0];
                    }

                    bg.color = fi.color;
                }
                yield return waitForEndOfFrame;

                yield return StartCoroutine(Data.GetCapture().TakeScreenShot(result =>
                {
                    fi.texture = result;

                    Data.GetUI().SetGeneratingText(j + "/" + fis.Count);
                }));
                yield return waitForEndOfFrame;
            }

            DataManager.instance.curNFTName = genBox[i].animalName;
        }

        Data.GetUI().SetGeneratingPanel(false);
    }

    #endregion


    public void RemoveAllMarker()
    {
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].ResetChoice();
        }
    }

    public void DisableMarkers(bool on)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].DisableView(on);
        }
    }
}

[Serializable]
public class GenBox
{
    public string animalName;
    public string path;
    public List<FixImage> fixImg;
}

[Serializable]
public class FixImage
{
    public bool isSaved;
    public int imgNum;
    public int[] code;
    public Color color;
    public Texture2D texture;
}


