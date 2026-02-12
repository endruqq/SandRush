using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to any UI Button to play FMOD sounds on hover and click.
/// Works with Unity's EventSystem (no code needed in other scripts).
/// </summary>
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("FMOD Events")]
    [SerializeField] private string _hoverEvent = "event:/UI_Button_Pointing";
    [SerializeField] private string _clickEvent = "event:/UI_Click_Button";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(_hoverEvent))
        {
            FMODHelper.PlayOneShot2D(_hoverEvent);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(_clickEvent))
        {
            FMODHelper.PlayOneShot2D(_clickEvent);
        }
    }
}
