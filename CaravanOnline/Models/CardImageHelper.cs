using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public static class CardImageHelper
    {
        public static string GetCardImagePath(Card card)
        {
            var suitName = card.Suit.ToLower();

            (string faceName, string suffix) = card.Face switch
            {
                "K" => ("king", "2"),
                "Q" => ("queen", "2"),
                "J" => ("jack", "2"),
                "A" => ("ace", ""),
                _   => (card.Face, "")
            };

            var fileName = $"{faceName}_of_{suitName}{suffix}.png";
            return $"/assets/cards/{fileName}";
        }
    }
}
