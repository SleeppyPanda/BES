using UnityEngine;

namespace BES.UI
{
    public abstract class UIScreenBase : MonoBehaviour
    {
        [SerializeField] protected GameObject root;

        public bool IsOpen => root != null && root.activeSelf;

        public virtual void Show()
        {
            if (root != null)
                root.SetActive(true);
            Refresh();
        }

        public virtual void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public virtual void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        public abstract void Refresh();
    }
}
