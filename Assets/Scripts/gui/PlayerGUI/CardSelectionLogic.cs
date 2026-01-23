// CardSelectionLogic.cs
using System;
using System.Collections.Generic;
using UnityEditor;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    /// <summary>Holds the temporary set of selected city-cards and knows the
    /// colour/quantity rules for discard, cure, fly, share, etc.</summary>
    public sealed class CardSelectionLogic
    {
        readonly List<int> _sel = new();
        public List<int> Selected => _sel;
        public void Clear() => _sel.Clear();

        /// <returns>true if the set changed</returns>
        public bool Toggle(int cityId, CardGUIStates mode, Player owner)
        {
            bool changed = false;

            if (mode == CardGUIStates.CardsDiscarding)
            {
                if (_sel.Count == 1 && _sel[0] == cityId) return false;
                _sel.Clear();
                _sel.Add(cityId);
                return true;
            }

            if (_sel.Contains(cityId) && mode != CardGUIStates.CardsExpandedShareAction && mode != CardGUIStates.CardsExpandedCharterActionToSelect)
            {
                _sel.Remove(cityId);
                changed = true;
            }
            else
            {
                if (mode == CardGUIStates.CardsExpandedFlyActionToSelect || (mode == CardGUIStates.CardsExpandedFlyActionSelected))
                {
                    _sel.Clear();
                    _sel.Add(cityId);
                    changed = true;
                }
                else if (mode == CardGUIStates.CardsExpandedCureActionToSelect)
                {
                    if (CanAddForCure(cityId, owner)) { _sel.Add(cityId); changed = true; }
                }
            }
            return changed;
        }

        public bool CurePossible(Player owner)
        {
            if (_sel.Count < 4) return false;

            int red = 0, yellow = 0, blue = 0;
            foreach (int id in _sel)
            {
                switch (CityDrawer.CityScripts[id].CityCard.VirusInfo.virusName)
                {
                    case VirusName.Red: ++red; break;
                    case VirusName.Yellow: ++yellow; break;
                    case VirusName.Blue: ++blue; break;
                }
            }
            return red >= 4 || yellow >= 4 || blue >= 4;
        }

        bool CanAddForCure(int cardId, Player owner)
        {
            var cardToAddVirus = CityDrawer.CityScripts[cardId].CityCard.VirusInfo.virusName;

            switch (cardToAddVirus)
            {
                case VirusName.Red:
                    if(owner.RedCardsInHand.Count >= 4 && _sel.Count <4) return true;
                    break;
                case VirusName.Yellow:
                    if (owner.YellowCardsInHand.Count >= 4 && _sel.Count < 4) return true;
                    break;
                case VirusName.Blue:
                    if (owner.BlueCardsInHand.Count >= 4 && _sel.Count < 4) return true;
                    break;
            }
            return false;
        }

        public void AddSingleCardToSelection(int cityId)
        {
            _sel.Clear();
            _sel.Add(cityId);
        }
    }
}
