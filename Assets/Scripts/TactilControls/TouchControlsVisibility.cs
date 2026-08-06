using UnityEngine;

public class TouchControlsVisibility : MonoBehaviour
{
    [SerializeField] private GameObject touchControlsRoot; // el panel con joystick + zona cámara
    [SerializeField] private bool forceTouch = false;

    private void Start()
    {
        bool useTouch = forceTouch || Application.isMobilePlatform;

        if (touchControlsRoot != null)
            touchControlsRoot.SetActive(useTouch);
    }
}