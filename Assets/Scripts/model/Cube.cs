using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class Cube : MonoBehaviour
    {
        [SerializeField] private VirusInfo virusInfo;

        public VirusInfo VirusInfo
        {
            get => virusInfo;
            set
            {
                virusInfo = value;
                ApplyVirusColor(); // Apply the new virus color if changed dynamically.
            }
        }

        private Image imageComponent;

        private void Awake() => imageComponent = GetComponent<Image>();
        private void Start()
        {
            ApplyVirusColor();
            CubeClickRelay relay = gameObject.AddComponent<CubeClickRelay>();
            relay.SetMeta(virusInfo.virusName);
        }
        public void Initialize(VirusInfo virusInfo, City city)
        {
            VirusInfo = virusInfo;
            ApplyVirusColor();
            CubeClickRelay relay = gameObject.AddComponent<CubeClickRelay>();
            relay.SetMeta(virusInfo.virusName);
            relay.SetCity(city);
        }


        private void ApplyVirusColor()
        {
            if (imageComponent != null && virusInfo != null)
            {
                imageComponent.color = virusInfo.virusColor;
            }
            else if (virusInfo == null)
            {
                Debug.LogError($"[{nameof(Cube)}] VirusInfo is not assigned to {gameObject.name}", gameObject);
            }
        }

        private class CubeClickRelay : MonoBehaviour, IPointerClickHandler
        {
            private VirusName color;
            private City city;
            public void SetMeta(VirusName v) => color = v;
            public void SetCity(City c) => city = c;



            public void OnPointerClick(PointerEventData _)
            {
                if (city == null) return; //The cube has already been clicked and removed.
                GUIEvents.RaiseCubeClicked(city, color);
            }
        }
    }
}
