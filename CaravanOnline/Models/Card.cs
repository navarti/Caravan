// Card.cs
using System;
using System.Collections.Generic;

namespace CaravanOnline.Models
{
    public class Card
    {
        public string Face { get; set; }
        public string Suit { get; set; }
        public int Number { get; set; }
        public string Direction { get; set; } = "up";
        public string? Effect { get; set; } = null;
        public List<Card> AttachedCards { get; set; } = new List<Card>();

        public Card(string face, string suit)
        {
            Face = face;
            Suit = suit;
            Number = GetNumberFromFace(face);
        }

        private int GetNumberFromFace(string face)
        {
            return face switch
            {
                "A" => 1,
                "2" => 2,
                "3" => 3,
                "4" => 4,
                "5" => 5,
                "6" => 6,
                "7" => 7,
                "8" => 8,
                "9" => 9,
                "10" => 10,
                "J" => 11,
                "Q" => 12,
                "K" => 13,
                _ => throw new ArgumentException("Invalid card face", nameof(face)),
            };
        }

        public override string ToString()
        {
            return $"{Face} of {Suit}";
        }
    }
}
