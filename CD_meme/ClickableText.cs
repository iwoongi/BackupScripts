using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ClickableText : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField textField;

    public void OnPointerClick(PointerEventData eventData)
    {
        var text = GetComponent<TextMeshProUGUI>();

#if UNITY_EDITOR
        if (eventData.button == PointerEventData.InputButton.Left)
        {
#else
        if (Input.touchCount > 0)
        {        
#endif
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, null);
            if (linkIndex > -1)
            {
                var linkInfo = text.textInfo.linkInfo[linkIndex];
                var linkId = linkInfo.GetLinkID();

                //Debug.Log(linkId.ToString());
                textField.GetComponent<WritingGCheckController>().OnClickErrorText(int.Parse(linkId));

                textField.enabled = false;
            }
            else
            {
                textField.enabled = true;
                textField.Select();
            }
        }
    }
}
