#if UNITY_EDITOR
using BES.Gameplay;
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    public static class MenuHubInventoryRuntimeMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string DatabasePath = "Assets/Resources/Data/ItemDatabase.asset";

        [MenuItem("BES/UI/Wire MenuHub Inventory Runtime")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var systems = Find(root.transform, "Systems") ?? root.transform;
                var inventory =
                    systems.GetComponent<InventorySystem>() ??
                    systems.gameObject.AddComponent<InventorySystem>();
                var itemDatabase =
                    AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
                var inventorySerialized = new SerializedObject(inventory);
                inventorySerialized.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
                inventorySerialized.ApplyModifiedPropertiesWithoutUndo();

                var bag = root.GetComponentInChildren<BagPanelController>(true);
                if (bag != null)
                {
                    var serialized = new SerializedObject(bag);
                    serialized.FindProperty("inventory").objectReferenceValue = inventory;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var wish = root.GetComponentInChildren<MenuWishController>(true);
                if (wish != null)
                {
                    var serialized = new SerializedObject(wish);
                    serialized.FindProperty("inventory").objectReferenceValue = inventory;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Shared MenuHub InventorySystem wired to Wish rewards and Bag display.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
