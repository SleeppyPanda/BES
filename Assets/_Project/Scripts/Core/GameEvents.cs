using System;

namespace BES.Core
{
    public static class GameEvents
    {
        public static event Action<float, float> OnPlayerHealthChanged;
        public static event Action<float, float> OnPlayerStaminaChanged;
        public static event Action<float, float> OnPlayerManaChanged;
        public static event Action<string> OnQuestUpdated;
        public static event Action<string, int> OnRelationshipChanged;
        public static event Action<string> OnDialogueStarted;
        public static event Action OnDialogueEnded;
        public static event Action<string> OnSceneLoadStarted;
        public static event Action<string> OnSceneLoadCompleted;
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;
        public static event Action OnPartyChanged;
        public static event Action<string> OnCollectiblePickedUp;
        public static event Action<string> OnNpcInRange;
        public static event Action OnNpcOutOfRange;
        public static event Action<string> OnNpcTalked;
        public static event Action<string> OnRegionEntered;
        public static event Action<string> OnEnemyDefeated;

        public static void RaiseNpcInRange(string npcDisplayName) =>
            OnNpcInRange?.Invoke(npcDisplayName);

        public static void RaiseNpcOutOfRange() =>
            OnNpcOutOfRange?.Invoke();

        public static void RaisePlayerHealthChanged(float current, float max) =>
            OnPlayerHealthChanged?.Invoke(current, max);

        public static void RaisePlayerStaminaChanged(float current, float max) =>
            OnPlayerStaminaChanged?.Invoke(current, max);

        public static void RaisePlayerManaChanged(float current, float max) =>
            OnPlayerManaChanged?.Invoke(current, max);

        public static void RaiseQuestUpdated(string questId) =>
            OnQuestUpdated?.Invoke(questId);

        public static void RaiseRelationshipChanged(string npcId, int affinity) =>
            OnRelationshipChanged?.Invoke(npcId, affinity);

        public static void RaiseDialogueStarted(string speakerId) =>
            OnDialogueStarted?.Invoke(speakerId);

        public static void RaiseDialogueEnded() =>
            OnDialogueEnded?.Invoke();

        public static void RaiseSceneLoadStarted(string sceneName) =>
            OnSceneLoadStarted?.Invoke(sceneName);

        public static void RaiseSceneLoadCompleted(string sceneName) =>
            OnSceneLoadCompleted?.Invoke(sceneName);

        public static void RaiseGameSaved() => OnGameSaved?.Invoke();
        public static void RaiseGameLoaded() => OnGameLoaded?.Invoke();
        public static void RaisePartyChanged() => OnPartyChanged?.Invoke();

        public static void RaiseCollectiblePickedUp(string itemId) =>
            OnCollectiblePickedUp?.Invoke(itemId);

        public static void RaiseNpcTalked(string npcId) =>
            OnNpcTalked?.Invoke(npcId);

        public static void RaiseRegionEntered(string regionId) =>
            OnRegionEntered?.Invoke(regionId);

        public static void RaiseEnemyDefeated(string enemyId) =>
            OnEnemyDefeated?.Invoke(enemyId);
    }
}
