using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Displays infection card information in the UI.
    /// </summary>
    public class InfectionCardDisplay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private CityCard cityCardData;

        public CityCard CityCardData
        {
            get => cityCardData;
            set
            {
                cityCardData = value;
                UpdateData();
            }
        }


        [Header("Visual Elements")]
        [SerializeField] private Image background;
        [SerializeField] private Image virus;
        //[SerializeField] private Image artwork;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI cityName;

        //[Header("Other Elements")]
        //[SerializeField] private GameObject border;


        private void UpdateData()
        {
            cityName.text = cityCardData.CityName;
            virus.color = cityCardData.VirusInfo.virusColor;
            background.color = cityCardData.VirusInfo.virusColor;
            cityName.color = cityCardData.VirusInfo.virusColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            DeckDrawer.ExpandInfectionDeck();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            DeckDrawer.CollapseInfectionDeck();
        }
    }
}
