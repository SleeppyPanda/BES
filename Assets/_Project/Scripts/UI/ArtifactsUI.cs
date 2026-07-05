using System.Collections.Generic;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class ArtifactsUI : UIScreenBase
    {
        [SerializeField] ArtifactDatabase database;
        [SerializeField] Transform gridContainer;
        [SerializeField] TMP_Text detailNameText;
        [SerializeField] TMP_Text detailDescText;
        [SerializeField] Button equipButton;
        [SerializeField] Button closeButton;

        readonly List<GameObject> slots = new();
        string selectedId;

        void Awake()
        {
            database ??= Resources.Load<ArtifactDatabase>("Data/ArtifactDatabase");
            if (root == null)
                root = gameObject;
            Hide();
            if (equipButton != null) equipButton.onClick.AddListener(OnEquip);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            RebuildGrid();
            RefreshDetail();
        }

        void RebuildGrid()
        {
            if (gridContainer == null || database == null)
                return;

            foreach (var s in slots)
                Object.Destroy(s);
            slots.Clear();

            foreach (var artifact in database.artifacts)
            {
                if (artifact == null)
                    continue;
                if (MetaProgressState.Instance != null && !MetaProgressState.Instance.OwnsArtifact(artifact.artifactId))
                    continue;

                var go = new GameObject(artifact.artifactId);
                go.transform.SetParent(gridContainer, false);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.3f, 0.25f, 0.5f, 0.9f);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(72, 72);
                var btn = go.AddComponent<Button>();
                var id = artifact.artifactId;
                btn.onClick.AddListener(() =>
                {
                    selectedId = id;
                    RefreshDetail();
                });
                slots.Add(go);
            }
        }

        void RefreshDetail()
        {
            var artifact = database?.GetById(selectedId) ?? (database?.artifacts.Count > 0 ? database.artifacts[0] : null);
            if (artifact == null)
                return;
            selectedId = artifact.artifactId;
            if (detailNameText != null) detailNameText.text = artifact.displayName;
            if (detailDescText != null)
            {
                var equipped = MetaProgressState.Instance?.EquippedArtifactId;
                var equippedNote = equipped == selectedId ? "\n(Đang trang bị)" : string.Empty;
                detailDescText.text = artifact.description + equippedNote;
            }
        }

        void OnEquip()
        {
            if (string.IsNullOrEmpty(selectedId))
                return;

            MetaProgressState.Instance?.SetEquippedArtifact(selectedId);
            Core.GameManager.Instance?.SaveGame();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
            RefreshDetail();
        }
    }
}
