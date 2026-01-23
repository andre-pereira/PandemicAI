using System.Linq;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class DeckEngine
    {
        private GameStateData s;
        private GameCatalog catalog;

        public void Init(GameStateData stateData, GameCatalog cat)
        {
            s = stateData;
            catalog = cat;
        }

        public void ResetDecks()
        {
            BuildPlayerDeck();
            BuildInfectionDeck();
        }

        public int DrawPlayerCard()
        {
            if (s.PlayerDeck.Count == 0) return -1;
            int id = s.PlayerDeck[^1];
            s.PlayerDeck.RemoveAt(s.PlayerDeck.Count - 1);
            return id;
        }

        public int DrawInfectionCard()
        {
            if (s.InfectionDeck.Count == 0) return -1;
            int id = s.InfectionDeck[^1];
            s.InfectionDeck.RemoveAt(s.InfectionDeck.Count - 1);
            return id;
        }

        public void DiscardPlayer(int cardId) => s.PlayerDiscard.Add(cardId);

        public void DiscardInfection(int cardId) => s.InfectionDiscard.Add(cardId);

        private void BuildPlayerDeck()
        {
            s.PlayerDeck.Clear();
            s.PlayerDeck.AddRange(Enumerable.Range(0, 24).ToList());
            s.PlayerDeck.Shuffle(s.playerRngState);
            s.playerRngState = Random.state;
        }

        private void BuildInfectionDeck()
        {
            s.InfectionDeck.Clear();
            s.InfectionDeck.AddRange(Enumerable.Range(0, 24).ToList());
            s.InfectionDeck.Shuffle(s.infectionRngState);
            s.infectionRngState = Random.state;
        }

        public void AddEpidemicCards()
        {
            int half = s.PlayerDeck.Count / 2;

            // Divide the list into three parts
            var part1 = s.PlayerDeck.Take(half).ToList();
            var part2 = s.PlayerDeck.Skip(half).Take(half).ToList();

            // Add the epidemic card value to each part
            part1.Add(catalog.EpidemicCardIndex);
            part2.Add(catalog.EpidemicCardIndex);

            // Shuffle each part
            part1.Shuffle(s.playerRngState);
            s.playerRngState = Random.state;
            part2.Shuffle(s.playerRngState);
            s.playerRngState = Random.state;

            // Join them back together
            s.PlayerDeck.Clear();
            s.PlayerDeck.AddRange(part1);
            s.PlayerDeck.AddRange(part2);
        }
    }
}
