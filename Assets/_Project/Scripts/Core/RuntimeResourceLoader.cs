using BES.Gameplay;
using BES.Narrative;
using UnityEngine;

namespace BES.Core
{
    public class RuntimeResourceLoader : MonoBehaviour
    {
        void Awake()
        {
            var questDatabase = Resources.Load<QuestDatabase>("Data/QuestDatabase");
            var itemDatabase = Resources.Load<ItemDatabase>("Data/ItemDatabase");

            if (GameManager.Instance == null)
                return;

            GameManager.Instance.Inventory.SetDatabase(itemDatabase != null
                ? itemDatabase
                : ItemDatabase.CreateDefaultRuntime());

            if (questDatabase != null)
                GameManager.Instance.Quests.SetDatabase(questDatabase);
        }
    }
}
