using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class FurhatGameMap
    {
        private static FurhatGameMap _instance;
        public static FurhatGameMap Instance => _instance ??= new FurhatGameMap();

        public Dictionary<string, RectTransform> ItemPlacement { get; private set; }

        public FurhatGameMap()
        {
            ItemPlacement = new Dictionary<string, RectTransform>();
        }

        // Add all the city positions in the beginning of the game
        public void SetCityPositions()
        {
            foreach (var city in CityDrawer.CityScripts)
            {
                int cityIndex = city.CityCard.CityID;
                RectTransform cityRectTransform = city.GetComponent<RectTransform>();
                SetItemPlacement(cityIndex.ToString(), cityRectTransform);
            }
        }

        // Method to add or update an item
        public void SetItemPlacement(string itemName, RectTransform rectTransform)
        {
            ItemPlacement[itemName] = rectTransform;
        }

        // Method to get an item's placement
        public RectTransform GetItemPlacement(string itemName)
        {
            if (ItemPlacement.TryGetValue(itemName, out RectTransform rectTransform))
            {
                return rectTransform;
            }
            return null;
        }

        // Helper method to get cube identifier
        public static string GetCubeIdentifier(int cityId, VirusName virusName)
        {
            return $"cube{cityId}{virusName}";
        }
    }
}
