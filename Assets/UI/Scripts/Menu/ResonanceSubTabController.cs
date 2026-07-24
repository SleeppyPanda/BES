using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class ResonanceSubTabBinding
    {
        public string tabName;
        public Button button;
        public GameObject listRoot;
        public GameObject selectedState;
        public UnityEvent onSelected;
    }

    public class ResonanceSubTabController : MonoBehaviour
    {
        [SerializeField] int initialTab;
        [SerializeField] List<ResonanceSubTabBinding> tabs = new();
        [SerializeField] bool resolveBindingsByIndexedNames = true;
        [SerializeField] UnityEvent<int> onTabChanged;
        public int CurrentTab { get; private set; }

        void Awake()
        {
            if (resolveBindingsByIndexedNames) ResolveBindings();
            for (var i = 0; i < tabs.Count; i++)
            {
                var index = i;
                var binding = tabs[i];
                if (binding != null && binding.button != null)
                    binding.button.onClick.AddListener(() => SelectTab(index));
            }
            SelectTab(Mathf.Clamp(initialTab, 0, Mathf.Max(0, tabs.Count - 1)));
        }

        void OnEnable()
        {
            // Restore the currently selected list whenever the parent tab is reopened.
            SelectTab(Mathf.Clamp(CurrentTab, 0, Mathf.Max(0, tabs.Count - 1)));
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;
            CurrentTab = index;
            for (var i = 0; i < tabs.Count; i++)
            {
                var binding = tabs[i];
                if (binding == null) continue;
                var selected = i == index;
                if (binding.listRoot != null) binding.listRoot.SetActive(selected);
                if (binding.selectedState != null) binding.selectedState.SetActive(selected);
                if (selected) binding.onSelected?.Invoke();
            }
            onTabChanged?.Invoke(index);
        }

        void ResolveBindings()
        {
            const int expectedTabCount = 4;
            while (tabs.Count < expectedTabCount) tabs.Add(new ResonanceSubTabBinding());

            for (var i = 0; i < expectedTabCount; i++)
            {
                tabs[i] ??= new ResonanceSubTabBinding();
                var buttonRoot = FindDescendant("SubTab_" + i, false);
                var listRoot = FindDescendant("TabList_" + i + "_", true);
                if (buttonRoot != null) tabs[i].button = buttonRoot.GetComponent<Button>();
                if (listRoot != null) tabs[i].listRoot = listRoot.gameObject;

                var selectedState = buttonRoot != null ? FindChild(buttonRoot, "SelectedState") : null;
                tabs[i].selectedState = selectedState != null ? selectedState.gameObject : null;
            }
        }

        Transform FindDescendant(string value, bool prefix)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if ((!prefix && child.name == value) ||
                    (prefix && child.name.StartsWith(value, StringComparison.Ordinal)))
                    return child;
            }
            return null;
        }

        static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root)
                if (child.name == childName) return child;
            return null;
        }
    }
}
