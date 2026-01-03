using System.Collections.Generic;
using System.Text.Json;
using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public static class SerializationHelper
    {
        public static string SerializeCards(List<Card> cards)
        {
            if (cards == null)
            {
                return string.Empty;
            }
            return JsonSerializer.Serialize(cards);
        }

        public static List<Card> DeserializeCards(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<Card>();
            }
            return JsonSerializer.Deserialize<List<Card>>(json) ?? new List<Card>();
        }

        public static string SerializeAttachedCards(List<Card> attachedCards)
        {
            if (attachedCards == null)
            {
                return string.Empty;
            }
            return JsonSerializer.Serialize(attachedCards);
        }

        public static List<Card> DeserializeAttachedCards(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<Card>();
            }
            return JsonSerializer.Deserialize<List<Card>>(json) ?? new List<Card>();
        }

        public static string SerializeLanes(List<List<Card>> lanes)
        {
            if (lanes == null)
            {
                return string.Empty;
            }
            return JsonSerializer.Serialize(lanes);
        }

        public static List<List<Card>> DeserializeLanes(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<List<Card>>();
            }
            return JsonSerializer.Deserialize<List<List<Card>>>(json) ?? new List<List<Card>>();
        }

        public static string SerializePlayerCards(List<Card> playerCards)
        {
            return SerializeCards(playerCards);
        }

        public static List<Card> DeserializePlayerCards(string json)
        {
            return DeserializeCards(json);
        }
    }
}
