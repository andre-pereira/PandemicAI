using System.Collections.Generic;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class CubeDrawer : MonoBehaviour
    {
        [SerializeField] public List<GameObject> redCubes;
        [SerializeField] public List<GameObject> yellowCubes;
        [SerializeField] public List<GameObject> blueCubes;

        public static List<GameObject> RedCubes { get; private set; }
        public static List<GameObject> YellowCubes { get; private set; }
        public static List<GameObject> BlueCubes { get; private set; }

        public void Awake()
        {
            RedCubes = redCubes;
            YellowCubes = yellowCubes;
            BlueCubes = blueCubes;
        }

        public void OnEnable() => GUIEvents.DrawCubes += Draw;
        public void OnDisable() => GUIEvents.DrawCubes -= Draw;

        public void Draw()
        {
            for (int i = 0; i < RedCubes.Count; i++)
            {
                redCubes[i].SetActive(i < GameRoot.State.RedCubes);
                yellowCubes[i].SetActive(i < GameRoot.State.YellowCubes);
                blueCubes[i].SetActive(i < GameRoot.State.BlueCubes);
            }
        }

        public static GameObject GetCubeFromPool(VirusInfo virusInfo, int increment)
        {
            switch (virusInfo.virusName)
            {
                case VirusName.Red:
                    return RedCubes[GameRoot.State.RedCubes + increment];
                case VirusName.Yellow:
                    return YellowCubes[GameRoot.State.YellowCubes + increment];
                case VirusName.Blue:
                    return BlueCubes[GameRoot.State.BlueCubes + increment];
                default:
                    return null;
            }
        }
    }
}