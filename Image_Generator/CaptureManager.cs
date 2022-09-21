using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CaptureManager : MonoBehaviour
{
    private readonly int max = 1000;
    private WaitForEndOfFrame waitForEndOfFrame;
    private string curPath;
    //private readonly string pathBasic = "_Basic";

    public void InitFolder()
    {
        curPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\sajang";
        if (!Directory.Exists(curPath))
        {
            Directory.CreateDirectory(curPath);
        }
        Data.GetUI().folderPath.text = curPath;

        waitForEndOfFrame = new WaitForEndOfFrame();
    }

    public IEnumerator TakeScreenShot(Action<Texture2D> callback)
    {
        yield return waitForEndOfFrame;
        var result = ScreenCapture.CaptureScreenshotAsTexture();

        yield return waitForEndOfFrame;
        Texture2D newTex = CaptureScreen(result);

        callback(newTex);
    }

    public void OnClickSetFolder()
    {
        var path = StandaloneFileBrowser.OpenFolderPanel("Select Folder", curPath, false);

        if (path.Length > 0)
        {
            //print("   >>   " + path[0]);
            curPath = path[0];
            Data.GetUI().folderPath.text = curPath;
        }
    }

    public void SaveToPng(PathInfo pi, int pathIndex, byte[] tex)
    {
        //string addGradeName = pathIndex > 0 ? pathIndex - 1 + "_" : string.Empty;
        string fileName = "#" + pathIndex + "_" + pi.fileNum.ToString("00000");

        //string folderName = pathIndex > 0 ? pi.path : pi.path + pathBasic;
        var path = Path.Combine(pi.path, fileName + ".png");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, tex);
        }
        //print(pi.animalName + "  Save at " + path);
    }

    /// <summary>
    /// imgNum = -1 이면 디렉토리의 파일개수 체크, 아니면 덮어씌우기
    /// </summary>
    /// <param name="animalName"></param>
    /// <param name="imgNum"></param>
    /// <returns></returns>
    public PathInfo CheckDirectory(string animalName, int imgNum, bool isLoad)
    {
        int cnt = 0;
        string[] dirs = Directory.GetDirectories(curPath);
        for (int i = 0; i < dirs.Length; i++)
        {
            int index = dirs.Length - 1 - i;
            if (dirs[index].Contains(animalName))
            {
                //print(dirs[index] + "    " + dirs[index].Substring(dirs[index].Length - 1, 1));
                string pathName = dirs[index].Split('_')[0];
                cnt = int.Parse(pathName.Substring(pathName.Length - 1, 1));
                break;
            }
        }

        string curDir = Path.Combine(curPath, animalName + cnt);
        PathInfo pi;
        if (cnt == 0 || !isLoad)
        {
            pi = CreateNewDirectory(cnt, animalName);
        }
        else
        {
            int fileCnt = Directory.GetFiles(curDir).Length;
            int fileNum = imgNum == -1 ? NumberByAnimal(animalName) + fileCnt + 1 : imgNum;

            if (fileCnt >= max)
            {
                pi = CreateNewDirectory(cnt, animalName);
            }
            else
            {
                pi = new PathInfo()
                {
                    animalName = animalName,
                    path = curDir,
                    fileNum = fileNum
                };
            }
        }

        return pi;
    }

    public void OpenInWinFileBrowser()
    {
        bool openInsidesOfFolder = false;

        //string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

        if (Directory.Exists(curPath)) // if path requested is a folder, automatically open insides of that folder
            openInsidesOfFolder = true;

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + curPath);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            // tried to open win explorer in mac
            // just silently skip error
            // we currently have no platform define for the current OS we are in, so we resort to this
            e.HelpLink = ""; // do anything with this variable to silence warning about not using it
        }
    }

    private PathInfo CreateNewDirectory(int cnt, string animalName)
    {
        string curDir = Path.Combine(curPath, animalName + (cnt + 1));

        Directory.CreateDirectory(curDir);
        //Directory.CreateDirectory(curDir + pathBasic);

        PathInfo pi = new PathInfo()
        {
            path = curDir,
            fileNum = NumberByAnimal(animalName) + 1
        };

        return pi;
    }

    private int NumberByAnimal(string animalName)
    {
        int num = 0;
        switch (animalName)
        {
            case "cat": num = 1000; break;
            case "owl": num = 2000; break;
            case "dog": num = 3000; break;
            case "penguin": num = 4000; break;
            case "cheetah": num = 5000; break;
            case "sheep": num = 6000; break;
            case "bear": num = 7000; break;
            case "fox": num = 8000; break;
            case "deer": num = 9000; break;
        }

        return num;
    }

    private Texture2D CaptureScreen(Texture2D tex)
    {
        RectTransform iRect = Data.GetRGen().GetImageRect();

        int width = Mathf.FloorToInt((tex.width * iRect.anchorMax.x) - (tex.width * iRect.anchorMin.x));
        int height = Mathf.FloorToInt(tex.height * (1 - iRect.anchorMin.y));
        int startX = Mathf.FloorToInt(tex.width * iRect.anchorMin.x);
        int startY = tex.height - height;

        Color[] c = tex.GetPixels(startX, startY, width, height);
        Texture2D cropTex = new Texture2D(width, height);

        cropTex.SetPixels(c);
        cropTex.Apply();

        return cropTex;
    }


    #region 텍스쳐 리사이징 테스트코드
    //private readonly int resizeValue = 1000;
    //private Texture2D ResizeTexture(Texture2D source)
    //{
    //    Texture2D result = new Texture2D(resizeValue, resizeValue, source.format, true);
    //    Color[] rpixels = result.GetPixels(0);
    //    float incX = (1.0f / resizeValue);
    //    float incY = (1.0f / resizeValue);
    //    for (int px = 0; px < rpixels.Length; px++)
    //    {
    //        rpixels[px] = source.GetPixelBilinear(incX * ((float)px % resizeValue), incY * ((float)Mathf.Floor(px / resizeValue)));
    //    }
    //    result.SetPixels(rpixels, 0);
    //    result.Apply();
    //    return result;
    //}

    //private readonly Vector2 resizingVec = new Vector2(1000, 1000);
    //private Texture2D ResizeTexture(Texture2D source)
    //{
    //    Color[] aSourceColor = source.GetPixels(0);
    //    Vector2 vSourceSize = new Vector2(source.width, source.height);
    //    int xWidth = (int)resizingVec.x;
    //    int xHeight = (int)resizingVec.y;

    //    Texture2D newTex = new Texture2D(xWidth, xHeight, source.format, false);

    //    int xLength = xWidth * xHeight;
    //    Color[] newColor = new Color[xLength];

    //    Vector2 vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);

    //    Vector2 vCenter = new Vector2();
    //    for(int i = 0; i < xLength; i++)
    //    {
    //        float xX = (float)i % xWidth;
    //        float xY = Mathf.Floor((float)i / xWidth);

    //        vCenter.x = (xX / xWidth) * vSourceSize.x;
    //        vCenter.y = (xY / xHeight) * vSourceSize.y;

    //        int xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - (vPixelSize.x * 0.5f)), 0);
    //        int xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + (vPixelSize.x * 0.5f)), vSourceSize.x);
    //        int xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - (vPixelSize.y * 0.5f)), 0);
    //        int xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + (vPixelSize.y * 0.5f)), vSourceSize.y);

    //        Color oColorTemp = new Color();
    //        float xGridCount = 0;
    //        for(int j = xYFrom; j < xYTo; j++)
    //        {
    //            for(int k = xXFrom; k < xXTo; k++)
    //            {
    //                oColorTemp += aSourceColor[(int)(((float)j * vSourceSize.x) + k)];

    //                xGridCount++;
    //            }
    //        }

    //        newColor[i] = oColorTemp / (float)xGridCount;
    //    }

    //    newTex.SetPixels(newColor);
    //    newTex.Apply();

    //    return newTex;
    //}
    #endregion
}

public class PathInfo
{
    public string animalName;
    public string path;
    public int fileNum;
}
