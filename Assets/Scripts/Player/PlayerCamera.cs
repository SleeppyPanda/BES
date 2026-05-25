using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform mainCamera;
    public float sensitivity = 0.1f;
    public float zoomSpeed = 8f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    PlayerState state;

    void Start()
    {
        state = GetComponent<PlayerState>();
        if (state != null)
        {
            state.currentZoom = Mathf.Clamp(state.currentZoom, minZoom, maxZoom);
            state.yRotation = transform.eulerAngles.y;
        }
    }

    void Update()
    {
        if (cameraPivot == null || mainCamera == null || state == null)
            return;

        Vector2 mouseDelta = BESInputReader.GetMouseDelta();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        state.xRotation -= mouseY;
        state.xRotation = Mathf.Clamp(state.xRotation, -40f, 60f);
        state.yRotation += mouseX;

        cameraPivot.rotation = Quaternion.Euler(state.xRotation, state.yRotation, 0);

        float scroll = BESInputReader.GetScrollNormalized();
        state.currentZoom = Mathf.Clamp(state.currentZoom - scroll * zoomSpeed, minZoom, maxZoom);
        mainCamera.localPosition = new Vector3(0, 0, -state.currentZoom);
    }
}
