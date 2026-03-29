using UnityEngine;

namespace SandRush.UI
{
    public class MaskSelectionWheel : MonoBehaviour
    {
        [System.Serializable]
        public class MaskWheelOption
        {
            [Tooltip("The GameObject for the default mask option (e.g., MaskChoose1)")]
            public GameObject NormalGraphic;
            [Tooltip("The GameObject for the chosen mask option (e.g., MaskChoosen1)")]
            public GameObject ChosenGraphic;
            [Tooltip("The GameObject for the description text (e.g., OpisMasek1)")]
            public GameObject DescriptionGraphic;
            [Tooltip("The Sprite to swap into the main HUD when this is selected")]
            public Sprite HUDIcon;
            [Tooltip("The ability provided by this mask")]
            public Player.MaskAbilityType AbilityType;
        }

        [Header("Wheel UI Settings")]
        [Tooltip("The container that holds the mask options (e.g., ChooseMasks)")]
        [SerializeField] private GameObject _wheelContainer;
        
        [Header("Mask Options")]
        [SerializeField] private MaskWheelOption[] _options;

        private void Start()
        {
            // Initialize the wheel state
            foreach (var opt in _options)
            {
                if (opt.ChosenGraphic != null)
                {
                    // Start with all chosen graphics disabled
                    opt.ChosenGraphic.SetActive(false);
                }

                if (opt.DescriptionGraphic != null)
                {
                    // Start with descriptions hidden
                    opt.DescriptionGraphic.SetActive(false);
                }

                if (opt.NormalGraphic != null)
                {
                    // Attempt to get or add MaskOption script to the normal graphics
                    MaskOption maskOptionScript = opt.NormalGraphic.GetComponent<MaskOption>();
                    if (maskOptionScript == null)
                    {
                        maskOptionScript = opt.NormalGraphic.AddComponent<MaskOption>();
                    }

                    // Pass reference back to the wheel
                    maskOptionScript.Initialize(this, opt);
                }

                if (opt.ChosenGraphic != null)
                {
                    // Do the exact same for the chosen graphics to support hover when selected
                    MaskOption maskOptionScript = opt.ChosenGraphic.GetComponent<MaskOption>();
                    if (maskOptionScript == null)
                    {
                        maskOptionScript = opt.ChosenGraphic.AddComponent<MaskOption>();
                    }
                    maskOptionScript.Initialize(this, opt);
                }
            }
            
            // Ensure the container is initially hidden
            if (_wheelContainer != null)
            {
                _wheelContainer.SetActive(false);
            }
        }

        private void Update()
        {
            // Listening for the 'Q' key to toggle the wheel visual
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (_wheelContainer != null)
                {
                    _wheelContainer.SetActive(true);
                }
                Player.IsUIModeActive = true;
            }
            else if (Input.GetKeyUp(KeyCode.Q))
            {
                if (_wheelContainer != null)
                {
                    _wheelContainer.SetActive(false);
                }
                Player.IsUIModeActive = false;
            }
        }

        private void OnDisable()
        {
            Player.IsUIModeActive = false;
        }

        public void OnMaskOptionClicked(MaskWheelOption selectedOption)
        {
            // Hide all chosen graphics first
            foreach (var opt in _options)
            {
                if (opt.ChosenGraphic != null)
                {
                    opt.ChosenGraphic.SetActive(false);
                }
            }

            // Show the chosen graphic for the selected option
            if (selectedOption != null)
            {
                if (selectedOption.ChosenGraphic != null)
                {
                    selectedOption.ChosenGraphic.SetActive(true);
                }
                
                Player player = FindFirstObjectByType<Player>();
                if (player != null && selectedOption.HUDIcon != null)
                {
                    player.SwitchMask(selectedOption.HUDIcon, selectedOption.AbilityType);
                }
                
                // You can add additional logic here to notify the Player or Game logic about the chosen mask.
                if (selectedOption.NormalGraphic != null)
                {
                    Debug.Log($"[MaskWheel] Selected mask option updated to {selectedOption.NormalGraphic.name}");
                }
            }
        }

        public void OnMaskOptionHovered(MaskWheelOption hoveredOption)
        {
            if (hoveredOption != null && hoveredOption.DescriptionGraphic != null)
            {
                hoveredOption.DescriptionGraphic.SetActive(true);
            }
        }

        public void OnMaskOptionHoverExited(MaskWheelOption exitedOption)
        {
            if (exitedOption != null && exitedOption.DescriptionGraphic != null)
            {
                exitedOption.DescriptionGraphic.SetActive(false);
            }
        }
    }
}
