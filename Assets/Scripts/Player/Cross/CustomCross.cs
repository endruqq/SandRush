using UnityEngine;
using UnityEngine.UI;

public class CursorCross : MonoBehaviour
{
    [SerializeField] private RectTransform _cursorImage;

    [SerializeField] private bool _turnOnCursor;

    void Start()
    {
        if (_turnOnCursor == true)
        {
            Cursor.visible = false;
        }
        else Cursor.visible = true;
    }
    void Update()
    {
        if (_turnOnCursor == false) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_cursorImage.parent as RectTransform, Input.mousePosition, null, out Vector2 pos);
        _cursorImage.localPosition = pos;
    }
}
