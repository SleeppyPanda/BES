using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public struct CharacterAttackMove
    {
        public string attackId;
        public string displayName;
        public float damageMultiplier;
        public float range;
        public float angle;
        public float startup;
        public float recovery;
        public float effectRadius;
        public float effectDuration;
        public Color effectColor;
        public bool useCombo;
        public float[] comboMultipliers;
    }

    public struct CharacterAttackProfile
    {
        public CharacterAttackMove leftClick;
        public CharacterAttackMove rightClick;
    }

    public static class CharacterCombatProfile
    {
        public static CharacterAttackProfile Get(CharacterDefinition character)
        {
            var characterId = character != null ? character.characterId : "hero_01";
            var leftAttackId = !string.IsNullOrEmpty(character?.leftClickAttackId)
                ? character.leftClickAttackId
                : GetDefaultLeftClickAttackId(characterId);
            var rightAttackId = !string.IsNullOrEmpty(character?.rightClickAttackId)
                ? character.rightClickAttackId
                : GetDefaultRightClickAttackId(characterId);

            return new CharacterAttackProfile
            {
                leftClick = CreateMove(leftAttackId, true),
                rightClick = CreateMove(rightAttackId, false)
            };
        }

        public static CharacterAttackMove CreateMove(string attackId, bool leftClick)
        {
            return attackId switch
            {
                "attack_void_edge_left" => CreateCombo("attack_void_edge_left", "Void Edge", 2.4f, 75f, 0.1f, 0.25f, 0.85f, new Color(0.35f, 0.75f, 1f, 0.9f), 1f, 1.15f, 1.35f),
                "attack_void_burst_right" => CreateSingle("attack_void_burst_right", "Void Burst", 2.3f, 3.2f, 110f, 0.22f, 0.36f, 1.35f, new Color(0.15f, 0.45f, 1f, 0.9f)),

                "attack_flare_cuts_left" => CreateCombo("attack_flare_cuts_left", "Flare Cuts", 2.2f, 95f, 0.07f, 0.18f, 0.75f, new Color(1f, 0.35f, 0.15f, 0.9f), 1.05f, 1.2f, 1.45f),
                "attack_flare_lunge_right" => CreateSingle("attack_flare_lunge_right", "Flare Lunge", 2.1f, 2.8f, 130f, 0.18f, 0.35f, 1.45f, new Color(1f, 0.15f, 0.05f, 0.9f)),

                "attack_guard_sweep_left" => CreateCombo("attack_guard_sweep_left", "Guard Sweep", 1.9f, 125f, 0.12f, 0.3f, 1.05f, new Color(0.35f, 1f, 0.45f, 0.9f), 0.95f, 1.15f, 1.7f),
                "attack_earth_slam_right" => CreateSingle("attack_earth_slam_right", "Earth Slam", 2.6f, 2.5f, 165f, 0.28f, 0.45f, 1.9f, new Color(0.12f, 0.85f, 0.25f, 0.9f)),

                "attack_arc_shot_left" => CreateCombo("attack_arc_shot_left", "Arc Shots", 4.2f, 50f, 0.1f, 0.22f, 0.8f, new Color(0.8f, 0.45f, 1f, 0.9f), 0.9f, 1.1f, 1.35f),
                "attack_marked_burst_right" => CreateSingle("attack_marked_burst_right", "Marked Burst", 2.4f, 5.2f, 80f, 0.25f, 0.4f, 1.35f, new Color(0.95f, 0.55f, 1f, 0.9f)),

                "attack_star_edge_left" => CreateCombo("attack_star_edge_left", "Star Edge", 3.1f, 110f, 0.09f, 0.2f, 1f, new Color(1f, 0.82f, 0.18f, 0.9f), 1.2f, 1.45f, 1.85f),
                "attack_lunar_cleave_right" => CreateSingle("attack_lunar_cleave_right", "Lunar Cleave", 3f, 4.2f, 140f, 0.22f, 0.38f, 1.8f, new Color(1f, 0.95f, 0.35f, 0.9f)),

                "attack_spark_jab_left" => CreateCombo("attack_spark_jab_left", "Spark Jab", 2.6f, 80f, 0.08f, 0.2f, 0.7f, new Color(0.45f, 0.65f, 1f, 0.9f), 0.85f, 1f, 1.2f),
                "attack_rookie_blast_right" => CreateSingle("attack_rookie_blast_right", "Rookie Blast", 1.8f, 3.4f, 95f, 0.2f, 0.32f, 1.2f, new Color(0.45f, 0.95f, 1f, 0.9f)),

                _ => leftClick
                    ? CreateMove("attack_void_edge_left", true)
                    : CreateMove("attack_void_burst_right", false)
            };
        }

        static string GetDefaultLeftClickAttackId(string characterId)
        {
            return characterId switch
            {
                "hero_02" => "attack_flare_cuts_left",
                "hero_03" => "attack_guard_sweep_left",
                "hero_04" => "attack_arc_shot_left",
                "char_limited_01" => "attack_star_edge_left",
                "hero_05" => "attack_spark_jab_left",
                _ => "attack_void_edge_left"
            };
        }

        static string GetDefaultRightClickAttackId(string characterId)
        {
            return characterId switch
            {
                "hero_02" => "attack_flare_lunge_right",
                "hero_03" => "attack_earth_slam_right",
                "hero_04" => "attack_marked_burst_right",
                "char_limited_01" => "attack_lunar_cleave_right",
                "hero_05" => "attack_rookie_blast_right",
                _ => "attack_void_burst_right"
            };
        }

        static CharacterAttackMove CreateCombo(
            string attackId,
            string displayName,
            float range,
            float angle,
            float startup,
            float recovery,
            float effectRadius,
            Color effectColor,
            float combo1,
            float combo2,
            float combo3)
        {
            return new CharacterAttackMove
            {
                attackId = attackId,
                displayName = displayName,
                damageMultiplier = combo1,
                range = range,
                angle = angle,
                startup = startup,
                recovery = recovery,
                effectRadius = effectRadius,
                effectDuration = 0.22f,
                effectColor = effectColor,
                useCombo = true,
                comboMultipliers = new[] { combo1, combo2, combo3 }
            };
        }

        static CharacterAttackMove CreateSingle(
            string attackId,
            string displayName,
            float damageMultiplier,
            float range,
            float angle,
            float startup,
            float recovery,
            float effectRadius,
            Color effectColor)
        {
            return new CharacterAttackMove
            {
                attackId = attackId,
                displayName = displayName,
                damageMultiplier = damageMultiplier,
                range = range,
                angle = angle,
                startup = startup,
                recovery = recovery,
                effectRadius = effectRadius,
                effectDuration = 0.32f,
                effectColor = effectColor,
                useCombo = false,
                comboMultipliers = null
            };
        }
    }
}
