using UnityEngine;

/// <summary>
/// Rotates a World Space Canvas to always face the main camera.
/// </summary>
public class BillboardCanvas : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // Face toward camera
        transform.forward = cam.transform.forward;
    }
}
