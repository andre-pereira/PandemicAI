using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class GameGUI : MonoBehaviour
    {
        [SerializeField] private GameAssetLibrary gameAssetLibrary;

        [Header("Canvases")]
        [SerializeField] private GameObject linesCanvasGO;
        [SerializeField] private GameObject cityCanvasGO;
        [SerializeField] private GameObject animationCanvasGO;

        public static GameAssetLibrary Assets { get; private set; }
        public static Transform LinesCanvasTransform { get; private set; }
        public static Canvas CityCanvas { get; private set; }
        public static Transform AnimationCanvasTransform { get; private set; }

        private void Awake()
        {
            Assets = gameAssetLibrary;
            LinesCanvasTransform = linesCanvasGO.transform;
            CityCanvas = cityCanvasGO.GetComponent<Canvas>();
            AnimationCanvasTransform = animationCanvasGO.transform;
        }

        private void OnEnable()
        {
            GUIEvents.DrawGUI += Draw;
            GUIEvents.DrawBoard += DrawBoard;
        }

        public void Draw()
        {
            DrawBoard();
            GUIEvents.RaiseDrawPlayerAreas();
        }

        public void DrawBoard()
        {
            GUIEvents.RaiseDrawPlayerDeckTextCount(GameRoot.State.PlayerDeck.Count);
            GUIEvents.RaiseDrawInfectionDeckTextCount(GameRoot.State.InfectionDeck.Count);
            GUIEvents.RaiseDrawMarkers();
            GUIEvents.RaiseDrawCubes();
            GUIEvents.RaiseDrawCities();
        }
    }
}
