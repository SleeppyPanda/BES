#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class ResonanceContentMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.ResonanceContent.v1";

        static ResonanceContentMigration() => EditorApplication.delayCall += RunOnce;
        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Build Resonance Tab Entries")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var lost = Find(root.transform, "TabList_0_Sanctum of Lost Echoes");
                var ascension = Find(root.transform, "TabList_1_Sanctum of Ascension");
                var insight = Find(root.transform, "TabList_2_Sanctum of Insight");
                if (lost == null || ascension == null || insight == null) return;
                BuildLostEchoes(Find(lost, "ListContent"));
                BuildDomains(Find(ascension, "ListContent"), "ascension");
                BuildDomains(Find(insight, "ListContent"), "insight");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Resonance Lost Echoes, Ascension and Insight entries created and wired.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void BuildLostEchoes(Transform content)
        {
            if (content == null) return;
            Clear(content);
            for (var i = 0; i < 4; i++)
            {
                var entry = EntryRoot("LostEchoAchievement_" + (i + 1), content);
                var component = entry.AddComponent<LostEchoAchievementEntry>();
                var relics = new List<DiscoverableRelicSlot>();
                for (var slot = 0; slot < 4; slot++)
                {
                    var x0 = .03f + slot * .115f;
                    var relic = Rect("RelicSlot_" + (slot + 1), entry.transform, new Vector2(x0, .22f), new Vector2(x0 + .095f, .84f));
                    var image = relic.AddComponent<Image>();
                    var discoverable = relic.AddComponent<DiscoverableRelicSlot>();
                    Set(discoverable, "relicImage", image);
                    relics.Add(discoverable);
                }
                var title = Text("SetTitle", entry.transform, "RELIC SET " + (i + 1), new Vector2(.50f, .60f), new Vector2(.78f, .88f));
                var buff = Text("BuffDescription", entry.transform, "2 Pieces: Buff\n4 Pieces: Additional buff", new Vector2(.50f, .15f), new Vector2(.78f, .60f));
                var activate = Button("ActivateButton", entry.transform, "ACTIVATE", new Vector2(.80f, .38f), new Vector2(.93f, .68f));
                var cost = Text("EnergyCost", entry.transform, "30", new Vector2(.93f, .38f), new Vector2(.995f, .68f));
                SetString(component, "achievementId", "lost_echo_" + (i + 1)); Set(component, "titleText", title); Set(component, "buffDescriptionText", buff); Set(component, "activateButton", activate); Set(component, "energyCostText", cost); SetList(component, "relicSlots", relics);
            }
        }

        static void BuildDomains(Transform content, string prefix)
        {
            if (content == null) return;
            Clear(content);
            for (var i = 0; i < 4; i++)
            {
                var entry = EntryRoot(prefix + "Domain_" + (i + 1), content);
                var component = entry.AddComponent<SanctumDomainEntry>();
                var title = Text("DomainTitle", entry.transform, (prefix + " domain " + (i + 1)).ToUpperInvariant(), new Vector2(.03f, .68f), new Vector2(.44f, .94f));
                var progress = Text("Progress", entry.transform, "10/10", new Vector2(.44f, .68f), new Vector2(.58f, .94f));
                var rewards = new List<Image>();
                for (var slot = 0; slot < 4; slot++)
                {
                    var x0 = .04f + slot * .12f;
                    var reward = Rect("RewardSlot_" + (slot + 1), entry.transform, new Vector2(x0, .10f), new Vector2(x0 + .095f, .66f));
                    rewards.Add(reward.AddComponent<Image>());
                }
                var enter = Button("EnterButton", entry.transform, "ENTER", new Vector2(.70f, .23f), new Vector2(.86f, .60f));
                var cost = Text("EnergyCost", entry.transform, "10", new Vector2(.87f, .23f), new Vector2(.98f, .60f));
                SetString(component, "domainId", prefix + "_" + (i + 1)); Set(component, "titleText", title); Set(component, "progressText", progress); Set(component, "enterButton", enter); Set(component, "energyCostText", cost); SetList(component, "rewardSlots", rewards);
            }
        }

        static GameObject EntryRoot(string name, Transform parent)
        {
            var entry = Rect(name, parent, Vector2.zero, Vector2.one);
            entry.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 150);
            entry.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            return entry;
        }
        static void Clear(Transform root) { for(var i=root.childCount-1;i>=0;i--) Object.DestroyImmediate(root.GetChild(i).gameObject); }
        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max) { var go=new GameObject(name,typeof(RectTransform));go.layer=LayerMask.NameToLayer("UI");var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;return go; }
        static Button Button(string name,Transform parent,string value,Vector2 min,Vector2 max){var go=Rect(name,parent,min,max);var image=go.AddComponent<Image>();var button=go.AddComponent<Button>();button.targetGraphic=image;Text("Label",go.transform,value,new Vector2(.04f,.05f),new Vector2(.96f,.95f));return button;}
        static TMP_Text Text(string name,Transform parent,string value,Vector2 min,Vector2 max){var go=Rect(name,parent,min,max);var text=go.AddComponent<TextMeshProUGUI>();text.text=value;text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=7;text.fontSizeMax=20;return text;}
        static Transform Find(Transform root,string name){if(root==null)return null;foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;return null;}
        static void Set(Object target,string property,Object value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p!=null){p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}}
        static void SetString(Object target,string property,string value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p!=null){p.stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();}}
        static void SetFloat(Object target,string property,float value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p!=null){p.floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}}
        static void SetList<T>(Object target,string property,List<T> values)where T:Object{var so=new SerializedObject(target);var p=so.FindProperty(property);p.arraySize=values.Count;for(var i=0;i<values.Count;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
#endif
