using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public static class Enums
    {
        public enum VirusName { Red, Yellow, Blue, None };

        public enum GameOverReasons
        {
            PlayersWon,
            TooManyOutbreaks,
            NoMoreCubesOfAColor,
            NoMorePlayerCards,
            None
        };

        public enum ActionType
        {
            Move,
            Treat,
            Fly,
            Charter,
            Share,
            FindCure,
            EndTurn,
            None
        };

        public enum CardGUIStates
        {
            None,
            CardsExpanded,
            CardsExpandedFlyActionToSelect,
            CardsExpandedFlyActionSelected,
            CardsExpandedShareAction,
            CardsExpandedCharterActionToSelect,
            CardsExpandedCureActionToSelect,
            CardsExpandedCureActionSelected,
            CardsDiscarding
        };

        public enum ContextButtonStates
        {
            Reject = 0,
            Accept = 1,
            Discard = 2,
            None
        };
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null)
                return value.ToString();

            var descriptionAttributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return descriptionAttributes.Length > 0 ? descriptionAttributes[0].Description : value.ToString();
        }

        public static T GetEnumValueFromDescription<T>(string description) where T : Enum
        {
            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description must be a non-empty string.", nameof(description));

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                var fieldInfo = value.GetType().GetField(value.ToString());
                if (fieldInfo == null)
                    continue;

                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0 && attributes[0].Description == description)
                {
                    return value;
                }
            }

            throw new ArgumentException($"Enum value not found for description: {description}", nameof(description));
        }
    }
}
