using UnityEngine;

namespace BES.UI
{
    public static class CharacterDatabaseLoader
    {
        const string DefaultPath = "Data/CharacterDatabase";
        static CharacterDatabase cached;

        public static CharacterDatabase Load()
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<CharacterDatabase>(DefaultPath);
#if UNITY_EDITOR
            if (cached == null)
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabase>(
                    "Assets/_Project/Resources/Data/CharacterDatabase.asset");
#endif
            if (cached == null)
                cached = CharacterDatabase.CreateRuntimeDefault();

            return cached;
        }
    }
}
