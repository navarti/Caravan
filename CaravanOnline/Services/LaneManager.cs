using System;
using System.Collections.Generic;
using System.Linq;
using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public class LaneManager
    {
        public List<List<Card>> Lanes { get; set; }
        public string LastFailReason { get; private set; } = string.Empty;

        public LaneManager()
        {
            Lanes = new List<List<Card>>
            {
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>()
            };
        }

        public bool AddCardToLane(int lane, Card card)
        {
            LastFailReason = string.Empty;
            if (lane < 1 || lane > 6)
            {
                LastFailReason = "Invalid lane number.";
                return false;
            }
            var laneIndex = lane - 1;
            var currentLane = Lanes[laneIndex];
            if (currentLane.Count == 0)
            {
                currentLane.Add(card);
                return true;
            }
            if (currentLane.Count == 1)
            {
                var firstCard = currentLane[0];
                currentLane.Add(card);
                if (card.Number > firstCard.Number) card.Direction = "up";
                else if (card.Number < firstCard.Number) card.Direction = "down";
                else card.Direction = "up";
                return true;
            }
            var lastCard = currentLane.Last();
            if (card.Suit == lastCard.Suit)
            {
                if (card.Number > lastCard.Number) card.Direction = "up";
                else if (card.Number < lastCard.Number) card.Direction = "down";
                else card.Direction = "up";
            }
            else
            {
                if (lastCard.Direction == "up")
                {
                    if (card.Number <= lastCard.Number)
                    {
                        LastFailReason = $"Invalid move: {card.Face} must be higher than {lastCard.Face} in 'up' lane.";
                        return false;
                    }
                    card.Direction = "up";
                }
                else if (lastCard.Direction == "down")
                {
                    if (card.Number >= lastCard.Number)
                    {
                        LastFailReason = $"Invalid move: {card.Face} must be lower than {lastCard.Face} in 'down' lane.";
                        return false;
                    }
                    card.Direction = "down";
                }
                else
                {
                    card.Direction = "up";
                }
            }
            currentLane.Add(card);
            return true;
        }

        public void DiscardLane(int lane)
        {
            if (lane >= 1 && lane <= 6)
            {
                Lanes[lane - 1].Clear();
            }
        }

        public int CalculateLaneScore(int lane)
        {
            if (lane < 1 || lane > 6) return 0;
            int score = 0;
            foreach (var card in Lanes[lane - 1])
            {
                if (card.Face == "RemovedByJack") continue;
                int kingCount = card.AttachedCards.Count(a => a.Face == "K");
                int cardValue = card.Number * (int)Math.Pow(2, kingCount);
                score += cardValue;
            }
            return score;
        }

        public string EvaluateGame()
        {
            if (!ShouldEvaluateGame()) return "The game is still ongoing.";

            int p1Points = 0;
            int p2Points = 0;
            var pairs = new List<(int, int)> { (1, 4), (2, 5), (3, 6) };
            foreach (var (l1, l2) in pairs)
            {
                int s1 = CalculateLaneScore(l1);
                int s2 = CalculateLaneScore(l2);
                if (s1 > s2) p1Points++;
                else if (s2 > s1) p2Points++;
            }

            if (p1Points > p2Points) return "Player 1 wins!";
            if (p2Points > p1Points) return "Player 2 wins!";
            return "It's a tie!";
        }

        private bool ShouldEvaluateGame()
        {
            bool groupA = IsLaneScoreInRange(1, 21, 26) || IsLaneScoreInRange(4, 21, 26);
            bool groupB = IsLaneScoreInRange(2, 21, 26) || IsLaneScoreInRange(5, 21, 26);
            bool groupC = IsLaneScoreInRange(3, 21, 26) || IsLaneScoreInRange(6, 21, 26);

            return groupA && groupB && groupC;
        }

        private bool IsLaneScoreInRange(int lane, int min, int max)
        {
            int score = CalculateLaneScore(lane);
            return score >= min && score <= max;
        }
    }
}
