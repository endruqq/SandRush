using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardButton : MonoBehaviour
{
    [Header("UI Bindings")]
    [Tooltip("The main button component on this card to trigger selection.")]
    public Button Button;

    [Tooltip("The text component displaying the card title.")]
    public TextMeshProUGUI TitleText;

    [Tooltip("The text component displaying the card description.")]
    public TextMeshProUGUI DescriptionText;

    [Header("Hover Sprite Customization")]
    [Tooltip("Optional: The background Image component of the card.")]
    public Image CardBackgroundImage;

    [Tooltip("Optional: The background Sprite to display on hover.")]
    public Sprite HoverBackgroundSprite;
}
