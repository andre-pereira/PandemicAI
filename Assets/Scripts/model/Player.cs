using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OPEN.PandemicAI
{

    public class Player
    {
        public PlayerPanel Panel { get; private set; }

        //private readonly List<int> _playerCardsInHand = new();
        private readonly List<int> _cityCardsInHand = new();
        private readonly List<int> _redCardsInHand = new();
        private readonly List<int> _yellowCardsInHand = new();
        private readonly List<int> _blueCardsInHand = new();
        private readonly List<int> _eventCardsInHand = new();

        public IReadOnlyList<int> Hand => _cityCardsInHand;
        //public IReadOnlyList<int> CityCardsInHand => _cityCardsInHand;
        public IReadOnlyList<int> RedCardsInHand => _redCardsInHand;
        public IReadOnlyList<int> YellowCardsInHand => _yellowCardsInHand;
        public IReadOnlyList<int> BlueCardsInHand => _blueCardsInHand;

        public enum Roles
        {
            [Description("Containment Specialist")]
            ContainmentSpecialist,

            [Description("Quarantine Specialist")]
            QuarantineSpecialist
        };

        public string Name { get; private set; }
        public Roles Role { get; private set; }
        public int ActionsRemaining { get; private set; }
        private int _currentCity;
        public bool IsAIControlled;
        public Plan Plan;

        public City CurrentCity => CityDrawer.CityScripts[_currentCity];

        public Player(PlayerPanel panel, Roles playerRole, string playerName)
        {
            Panel = panel;
            Role = playerRole;
            Name = playerName;
            IsAIControlled = false;
            ActionsRemaining = 4; // Default actions per turn
            UpdateCurrentCity(GameRoot.Catalog.InitialCityId, false);
        }

        public void DecreaseActionsRemaining(int decrement)
        {
            ActionsRemaining -= decrement;
            if (ActionsRemaining == 0)
            {
                GameEvents.RequestStateChange(GameState.DrawPlayerCards);
            }
            GameRoot.State.UpdatePlayerActions();
            Panel.ClearSelectedAction(false, ActionsRemaining);
        }

        public void AddCardToHand(int card)
        {
            if (!_cityCardsInHand.Contains(card))
            {
                    _cityCardsInHand.Add(card);
                    _cityCardsInHand.Sort();
                    City city = CityDrawer.CityScripts[card];

                    switch (city.CityCard.VirusInfo.virusName)
                    {
                        case Enums.VirusName.Red:
                            _redCardsInHand.Add(card);
                            break;
                        case Enums.VirusName.Yellow:
                            _yellowCardsInHand.Add(card);
                            break;
                        case Enums.VirusName.Blue:
                            _blueCardsInHand.Add(card);
                            break;
                    }
            }
            if (_cityCardsInHand.Count > 6)
            {
                GameEvents.RequestStateChange(GameState.Discarding);
            }
            if (_currentCity == card)
            {
                // update player actions
                GameRoot.State.UpdatePlayerActions();
            }
        }

        public void UpdateCurrentCity(int cityID, bool updateRoles)
        {
            // Remove player from the current city.
            CurrentCity.RemovePawn(this);
            _currentCity = cityID;
            // Add player to the new city.
            CurrentCity.AddPawn(this);

            // If role-specific update is required for Containment Specialist, add the corresponding event.
            if (updateRoles && Role == Roles.ContainmentSpecialist)
            {
                int redCount = CurrentCity.GetNumberOfCubes(Enums.VirusName.Red);
                int yellowCount = CurrentCity.GetNumberOfCubes(Enums.VirusName.Yellow);
                int blueCount = CurrentCity.GetNumberOfCubes(Enums.VirusName.Blue);

                if (redCount >= 2 || yellowCount >= 2 || blueCount >= 2)
                {
                    Timeline.Instance.AddEvent(new PContainSpecialistRemoveWhenEntering(CurrentCity, redCount, yellowCount, blueCount));
                }
            }
        }

        public void RemoveCardInHand(int cityID, bool addToDiscardPile = false)
        {
            if (_cityCardsInHand.Contains(cityID))
            {
                _cityCardsInHand.Remove(cityID);

                if (cityID < 24)
                {
                    _cityCardsInHand.Remove(cityID);
                    switch (CityDrawer.CityScripts[cityID].CityCard.VirusInfo.virusName)
                    {
                        case Enums.VirusName.Red:
                            _redCardsInHand.Remove(cityID);
                            break;
                        case Enums.VirusName.Yellow:
                            _yellowCardsInHand.Remove(cityID);
                            break;
                        case Enums.VirusName.Blue:
                            _blueCardsInHand.Remove(cityID);
                            break;
                    }
                }
                else
                {
                    _eventCardsInHand.Remove(cityID);
                }

                if (addToDiscardPile)
                {
                    GameRoot.State.PlayerDiscard.Add(cityID);
                }
            }
        }

        public void ResetTurn()
        {
            ActionsRemaining = 4;
            GameRoot.State.UpdatePlayerActions();
            GameEvents.RequestStateChange(GameState.PlayerActions);
        }
        
        public int GetCurrentCityId() => _currentCity;

        public RoleCard GetRoleCard() => GameRoot.Catalog.RoleCards[(int)Role];

        internal List<int> GetCardsInHand(Enums.VirusName color)
        {
            switch (color)
            {
                case Enums.VirusName.Red:
                    return _redCardsInHand;
                case Enums.VirusName.Yellow:
                    return _yellowCardsInHand;
                case Enums.VirusName.Blue:
                    return _blueCardsInHand;
                default:
                    throw new ArgumentException("Invalid virus color specified.");
            }
        }

        internal (bool canCure, Enums.VirusName virus) HasCardSetForUncuredDisease()
        {
            foreach (Enums.VirusName color in Enum.GetValues(typeof(Enums.VirusName)))
            {
                if (color == Enums.VirusName.None) continue;

                int numCards = GetCardsInHand(color).Count;
                if (numCards >= 4 && !GameRoot.State.IsCuredColor(color))
                {
                    return (true, color);
                }
            }
            return (false, Enums.VirusName.None);
        }
    }
}
