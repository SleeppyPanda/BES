using BES.Core;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public class PartyCharacterVisualSwitcher : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;

        GameObject activeVisual;
        string activeCharacterId;
        MeshRenderer rootRenderer;
        MaterialPropertyBlock propertyBlock;

        void Awake()
        {
            if (visualRoot == null)
            {
                var root = new GameObject("CharacterVisualRoot");
                root.transform.SetParent(transform, false);
                visualRoot = root.transform;
            }

            rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;
            propertyBlock = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            GameEvents.OnPartyChanged += ApplyActiveCharacter;
            ApplyActiveCharacter();
        }

        void Start()
        {
            ApplyActiveCharacter();
        }

        void Update()
        {
            if (activeVisual == null && PartyRoster.Instance?.ActiveCharacter != null)
            {
                ApplyActiveCharacter();
            }
        }

        void OnDisable() => GameEvents.OnPartyChanged -= ApplyActiveCharacter;

        public void ApplyActiveCharacter()
        {
            var character = PartyRoster.Instance?.ActiveCharacter;
            if (character == null || character.characterId == activeCharacterId)
                return;

            activeCharacterId = character.characterId;

            if (activeVisual != null)
            {
                Destroy(activeVisual);
                activeVisual = null;
            }

            activeVisual = character.gameplayPrefab != null
                ? Instantiate(character.gameplayPrefab, visualRoot)
                : CreateFallbackVisual(character);
            activeVisual.transform.localPosition = Vector3.zero;
            activeVisual.transform.localRotation = Quaternion.identity;
            activeVisual.transform.localScale = character.gameplayPrefab != null ? Vector3.one : character.testVisualScale;
        }

        GameObject CreateFallbackVisual(CharacterDefinition character)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = $"{character.characterId}_TestVisual";
            visual.transform.SetParent(visualRoot, false);

            foreach (var col in visual.GetComponents<Collider>())
                Destroy(col);

            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null)
                return visual;

            renderer.material = rootRenderer != null ? rootRenderer.sharedMaterial : renderer.sharedMaterial;
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", character.testVisualColor);
            propertyBlock.SetColor("_BaseColor", character.testVisualColor);
            renderer.SetPropertyBlock(propertyBlock);
            return visual;
        }
    }
}
