using System;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class CityDrawer : MonoBehaviour
    {
        [SerializeField] public GameObject[] citiesGO;
        [SerializeField] public City[] citiesScripts;

        public static GameObject[] CitiesGO { get; private set; }
        public static City[] CityScripts { get; private set;}

        public void Awake()
        {
            CitiesGO = citiesGO;
            CityScripts = citiesScripts;
        }

        public void OnEnable()
        {
            GUIEvents.DrawCities += DrawCities;
            GUIEvents.DrawCity += DrawCity;
        }


        public void OnDisable() => GUIEvents.DrawCities -= DrawCities;
        public void Start() => CreateNeighborLines();

        public void DrawCities() {
            foreach (City city in CityScripts) 
                city.Draw();
        }

        private void DrawCity(int city) { CityScripts[city].Draw(); }

        private void CreateNeighborLines()
        {
            for(int i = 0; i < CitiesGO.Length; i++)
            {
                City cityScript = citiesScripts[i];

                foreach (int neighbor in cityScript.CityCard.Neighbors)
                {
                    if (neighbor > cityScript.CityCard.CityID && neighbor < CitiesGO.Length)
                    {
                        GameObject line = new GameObject($"Line - {CitiesGO[i].name}_{CitiesGO[neighbor].name}");
                        line.transform.SetParent(GameGUI.LinesCanvasTransform.transform, false);
                        line.transform.position = cityScript.transform.position;
                        LineRenderer lr = line.AddComponent<LineRenderer>();
                        lr.sortingLayerName = "Lines";
                        lr.material = GameGUI.Assets.lineMaterial;
                        lr.startColor = cityScript.CityCard.VirusInfo.virusColor;
                        lr.endColor = citiesScripts[neighbor].CityCard.VirusInfo.virusColor;
                        //lr.startColor = Color.white;
                        //lr.endColor = Color.white
                        lr.startWidth = 0.075f;
                        lr.endWidth = 0.075f;
                        lr.SetPosition(0, cityScript.transform.position);
                        lr.SetPosition(1, CitiesGO[neighbor].transform.position);
                    }
                }
            }
        }

        public static bool AddCubes(int cityID, VirusName virusName, int numberOfCubes)
        {
            return CityScripts[cityID].IncrementNumberOfCubes(virusName, numberOfCubes);
        }
    }
}