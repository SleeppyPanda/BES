using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "CharacterPortraitManifest", menuName = "BES/Character Portrait Manifest")]
    public class CharacterPortraitManifest : ScriptableObject
    {
        public Sprite hero01;
        public Sprite hero02;
        public Sprite hero03;
        public Sprite hero04;
        public Sprite limitedHero;
        public Sprite defaultPortrait;

        public Sprite GetPortrait(string characterId)
        {
            var sprite = characterId switch
            {
                "hero_01" => hero01,
                "hero_02" => hero02,
                "hero_03" => hero03,
                "hero_04" => hero04,
                "char_limited_01" => limitedHero,
                _ => null
            };

            return sprite != null ? sprite : defaultPortrait;
        }
    }
}
