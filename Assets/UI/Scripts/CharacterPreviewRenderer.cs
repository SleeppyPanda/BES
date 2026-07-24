using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class CharacterPreviewRenderer : MonoBehaviour
    {
        [SerializeField] RawImage targetImage;
        [SerializeField] int textureSize = 512;

        Camera previewCamera;
        RenderTexture renderTexture;
        Transform previewPivot;

        void Awake()
        {
            CreatePreviewRig();
        }

        void CreatePreviewRig()
        {
            var rig = new GameObject("CharacterPreviewRig");
            rig.transform.SetParent(transform, false);
            rig.hideFlags = HideFlags.HideAndDontSave;

            previewPivot = new GameObject("Pivot").transform;
            previewPivot.SetParent(rig.transform, false);

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.2f, -3f);
            camGo.transform.LookAt(new Vector3(0f, 1f, 0f));
            previewCamera = camGo.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0, 0, 0, 0);
            previewCamera.cullingMask = ~0;
            previewCamera.enabled = false;

            renderTexture = new RenderTexture(textureSize, textureSize, 16);
            previewCamera.targetTexture = renderTexture;
            if (targetImage != null)
                targetImage.texture = renderTexture;
        }

        public void SetPreviewTarget(Transform target)
        {
            if (previewPivot == null || target == null)
                return;

            target.SetParent(previewPivot, false);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
        }

        public void RenderFrame()
        {
            if (previewCamera != null)
                previewCamera.Render();
        }

        void OnDestroy()
        {
            if (renderTexture != null)
                renderTexture.Release();
        }
    }
}
