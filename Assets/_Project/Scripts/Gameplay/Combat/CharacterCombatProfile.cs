using UnityEngine;

namespace BES.Gameplay
{
    public struct CharacterAttackProfile
    {
        public string name;
        public float range;
        public float angle;
        public float startup;
        public float recovery;
        public float[] comboMultipliers;
        public float heavyMultiplier;
        public float heavyRange;
        public float heavyStartup;
        public float heavyRecovery;
        public Color effectColor;
    }

    public static class CharacterCombatProfile
    {
        public static CharacterAttackProfile Get(string characterId)
        {
            return characterId switch
            {
                "hero_02" => Create("Flare Blades", 2.2f, 95f, 0.07f, 0.18f, 1.05f, 1.2f, 1.45f, 2.1f, 2.8f, 0.18f, 0.35f, new Color(1f, 0.35f, 0.15f, 0.9f)),
                "hero_03" => Create("Shield Crusher", 1.9f, 125f, 0.12f, 0.3f, 0.95f, 1.15f, 1.7f, 2.6f, 2.5f, 0.28f, 0.45f, new Color(0.35f, 1f, 0.45f, 0.9f)),
                "hero_04" => Create("Arc Shots", 4.2f, 50f, 0.1f, 0.22f, 0.9f, 1.1f, 1.35f, 2.4f, 5.2f, 0.25f, 0.4f, new Color(0.8f, 0.45f, 1f, 0.9f)),
                "char_limited_01" => Create("Starfall Edge", 3.1f, 110f, 0.09f, 0.2f, 1.2f, 1.45f, 1.85f, 3f, 4.2f, 0.22f, 0.38f, new Color(1f, 0.82f, 0.18f, 0.9f)),
                "hero_05" => Create("Rookie Sparks", 2.6f, 80f, 0.08f, 0.2f, 0.85f, 1.0f, 1.2f, 1.8f, 3.4f, 0.2f, 0.32f, new Color(0.45f, 0.65f, 1f, 0.9f)),
                _ => Create("Void Sword", 2.4f, 75f, 0.1f, 0.25f, 1f, 1.15f, 1.35f, 2.3f, 3.2f, 0.22f, 0.36f, new Color(0.35f, 0.75f, 1f, 0.9f))
            };
        }

        static CharacterAttackProfile Create(
            string name,
            float range,
            float angle,
            float startup,
            float recovery,
            float combo1,
            float combo2,
            float combo3,
            float heavyMultiplier,
            float heavyRange,
            float heavyStartup,
            float heavyRecovery,
            Color effectColor)
        {
            return new CharacterAttackProfile
            {
                name = name,
                range = range,
                angle = angle,
                startup = startup,
                recovery = recovery,
                comboMultipliers = new[] { combo1, combo2, combo3 },
                heavyMultiplier = heavyMultiplier,
                heavyRange = heavyRange,
                heavyStartup = heavyStartup,
                heavyRecovery = heavyRecovery,
                effectColor = effectColor
            };
        }
    }
}
