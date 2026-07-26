using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI
{
    [Serializable]
    public class LetterDefinition
    {
        public string letterId;
        public string senderName;
        public string title;
        [TextArea(3, 8)] public string body;
        public bool isRead;
        public string rewardLabel;
    }

    [CreateAssetMenu(fileName = "LetterDatabase", menuName = "BES/UI/Letter Database")]
    public class LetterDatabase : ScriptableObject
    {
        [SerializeField] List<LetterDefinition> letters = new();

        public IReadOnlyList<LetterDefinition> Letters => letters;

        public void ResetToDefaultEntries()
        {
            letters.Clear();
            letters.Add(new LetterDefinition
            {
                letterId = "welcome_back",
                senderName = "BES Team",
                title = "Welcome back, Traveler",
                body = "Cảm ơn bạn đã quay lại BES. Hòm thư này giờ là UI thật: có danh sách thư, trạng thái đã đọc và vùng nội dung.",
                rewardLabel = "100 Coins"
            });
            letters.Add(new LetterDefinition
            {
                letterId = "daily_supply",
                senderName = "Guild Clerk",
                title = "Daily Supply",
                body = "Một phần quà nhỏ để bạn test inventory, gacha và quest flow trong bản MVP.",
                rewardLabel = "3 Healing Potions"
            });
            letters.Add(new LetterDefinition
            {
                letterId = "nyc_note",
                senderName = "Người yêu cũ",
                title = "Lời nhắn chưa gửi",
                body = "Anh có nhớ em không? Nếu chưa chắc, cứ mở lại dialogue ngoài map để chọn Có hoặc Không.",
                rewardLabel = "Một vết thương lòng"
            });
        }

        public bool MarkRead(string letterId)
        {
            var letter = Find(letterId);
            if (letter == null)
                return false;

            letter.isRead = true;
            return true;
        }

        public LetterDefinition Find(string letterId) =>
            letters.Find(letter => letter != null && letter.letterId == letterId);
    }
}
