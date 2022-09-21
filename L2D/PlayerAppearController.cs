using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAppearController : MonoBehaviour
{
    [SerializeField] private RectTransform layerBotLand, layerRight;

    private WaitForEndOfFrame waitForEndOfFrame;
    private IEnumerator movePanel;
    private Vector2 changeBotLand, changeRight;
    private bool isOutPanel;
    private float time;

    private void Awake()
    {
        waitForEndOfFrame = new WaitForEndOfFrame();
        isOutPanel = false;

        changeBotLand = new Vector2(0, -layerBotLand.sizeDelta.y);
        changeRight = new Vector2(layerRight.sizeDelta.x, 0);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && isOutPanel) { AppearPanel(); }

        if (Input.GetMouseButtonUp(0)) { time = 0; }

        if (!Input.GetMouseButton(0)) { CheckTime(); }

#elif UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began && isOutPanel) { AppearPanel(); }

            if (Input.GetTouch(0).phase == TouchPhase.Ended) { time = 0; }
        }
        else { CheckTime(); }
#endif
    }

    #region _PANEL_ON_OFF
    private void CheckTime()
    {
        if (!isOutPanel)
        {
            time += Time.deltaTime;
            if (time > 6)
            {
                DisappearPanel();
            }
        }
    }

    private void AppearPanel()
    {
        if (movePanel != null)
            StopCoroutine(movePanel);

        movePanel = PanelMoving(true);
        StartCoroutine(movePanel);
        isOutPanel = false;
    }

    private void DisappearPanel()
    {
        if (movePanel != null)
            StopCoroutine(movePanel);

        movePanel = PanelMoving(false);
        StartCoroutine(movePanel);
        isOutPanel = true;
    }

    private IEnumerator PanelMoving(bool isOn)
    {
        layerBotLand.gameObject.SetActive(true);
        layerRight.gameObject.SetActive(true);

        float step = 0;
        while (step < 1f)
        {
            layerBotLand.anchoredPosition = isOn ? Vector2.Lerp(changeBotLand, Vector2.zero, step += Time.deltaTime) :
                Vector2.Lerp(Vector2.zero, changeBotLand, step += Time.deltaTime);

            layerRight.anchoredPosition = isOn ? Vector2.Lerp(changeRight, Vector2.zero, step += Time.deltaTime) :
                Vector2.Lerp(Vector2.zero, changeRight, step += Time.deltaTime);

            yield return waitForEndOfFrame;
        }
    }
    #endregion
}
