using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class City : MonoBehaviour
    {
        public CityCard CityCard;

        public Dictionary<VirusName, int> cubes;

        public GameObject CubesGameObject;
        public GameObject PawnsGameObject;
        public GameObject GlowGameObject;
        public List<Player> PlayersInCity = new List<Player>();
        public Pawn[] PawnsInCity;
        public Image Image;
        public TextMeshProUGUI cityNameText;

        private RectTransform _rectTransform;
        private RectTransform _canvasRectTransform;

        private void Awake()
        {
            //Initialize();
            _rectTransform = GetComponent<RectTransform>();
            _canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            cityNameText.text = CityCard.CityName;
            Image.color = CityCard.VirusInfo.virusColor;

        }

        public void Initialize()
        {
            cubes = new Dictionary<VirusName, int>{
            { VirusName.Red, 0 },
            { VirusName.Yellow, 0 },
            { VirusName.Blue, 0 }
        };
            PawnsInCity = new Pawn[2];
            PlayersInCity.Clear();
        }

        public int GetNumberOfCubes(VirusName virusName) =>
            cubes.ContainsKey(virusName) ? cubes[virusName] : 0;


        public int GetMaxNumberCubes()
        {
            int max = 0;
            foreach (var kvp in cubes)
            {
                if (kvp.Value > max) max = kvp.Value;
            }
            return max;
        }

        public VirusName GetMaxVirusName()
        {
            VirusName maxVirus = VirusName.Red;
            int max = 0;
            foreach (var kvp in cubes)
            {
                if (kvp.Value > max)
                {
                    max = kvp.Value;
                    maxVirus = kvp.Key;
                }
            }
            return maxVirus;
        }

        public void ResetCubesOfColor(VirusName virusName)
        {
            if (cubes.ContainsKey(virusName))
                cubes[virusName] = 0;
        }

        public bool IncrementNumberOfCubes(VirusName virusName, int increment)
        {
            bool valToReturn = (cubes[virusName] + increment) > 3;
            cubes[virusName] = Mathf.Min(cubes[virusName] + increment, 3);
            return valToReturn;
        }

        public void AddPawn(Player player) => PlayersInCity.Add(player);
        public void RemovePawn(Player player) => PlayersInCity.Remove(player);

        public void Draw()
        {
            CubesGameObject.DestroyChildrenImmediate();
            PawnsGameObject.DestroyChildrenImmediate();

            DrawCubes();

            var catalog = GameRoot.Catalog;

            if (PlayersInCity.Count > 0)
            {
                for (int i = 0; i < PlayersInCity.Count; i++)
                {
                    int playerPosition = (int)PlayersInCity[i].Role;
                    GameObject pawn = Instantiate(GameGUI.Assets.pawnPrefab, PawnsGameObject.transform);
                    pawn.transform.Translate(catalog.pawnOffsets[i][0], catalog.pawnOffsets[i][1], 0);
                    PawnsInCity[playerPosition] = pawn.GetComponent<Pawn>();
                    PawnsInCity[playerPosition].SetRoleAndPlayer(PlayersInCity[i]);
                }
            }
        }

        private void DrawCubes()
        {
            var catalog = GameRoot.Catalog;
            InstantiateCubes(cubes[VirusName.Red], catalog.cubeOffsetRed, catalog.VirusInfos[0]);
            InstantiateCubes(cubes[VirusName.Yellow], catalog.cubeOffsetYellow, catalog.VirusInfos[1]);
            InstantiateCubes(cubes[VirusName.Blue], catalog.cubeOffsetBlue, catalog.VirusInfos[2]);
        }

        private void InstantiateCubes(int numberOfCubes, Vector2[] offsets, VirusInfo info)
        {
            for (int i = numberOfCubes - 1; i >= 0; i--)
            {
                GameObject cube = Instantiate(GameGUI.Assets.cubePrefab, CubesGameObject.transform);
                cube.transform.Translate(offsets[i][0], offsets[i][1], 0);

                Cube cubeScript = cube.GetComponent<Cube>();
                if (cubeScript != null)
                    cubeScript.Initialize(info, this);
                else
                    Debug.LogWarning("Cube instantiated in City is missing Cube script", cube);

                // Add the cube to FurhatGameMap
                string cubeId = FurhatGameMap.GetCubeIdentifier(CityCard.CityID, info.virusName);
                FurhatGameMap.Instance.SetItemPlacement(cubeId, cube.GetComponent<RectTransform>());
            }
        }

        public bool HasCubes() => cubes.Values.Any(cubes => cubes > 0);

        public VirusName? FirstVirusFoundInCity()
        {
            foreach (var kvp in cubes)
            {
                if (kvp.Value > 0) return kvp.Key;
            }
            return null;
        }
    }
}
