using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform mainCamera;
    public float sensitivity = 200f;
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
        if (cameraPivot == null || mainCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        state.xRotation -= mouseY;
        state.xRotation = Mathf.Clamp(state.xRotation, -40f, 60f);
        state.yRotation += mouseX;

        cameraPivot.rotation = Quaternion.Euler(state.xRotation, state.yRotation, 0);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        state.currentZoom = Mathf.Clamp(state.currentZoom - scroll * zoomSpeed, minZoom, maxZoom);
        mainCamera.localPosition = new Vector3(0, 0, -state.currentZoom);
    }
}
