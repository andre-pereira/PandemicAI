using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    public class CityCardDisplay : MonoBehaviour
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

        [SerializeField] private Image virusBottom;

        [Header("Visual Elements")]
        [SerializeField] private Image artwork;
        [SerializeField] private Image flag;
        [SerializeField] private Image border;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI cityName;
        //[SerializeField] private TextMeshProUGUI countryName;
        [SerializeField] private TextMeshProUGUI cityNameVertical;

        private void UpdateData()
        {
            cityName.text = cityCardData.CityName;
            cityNameVertical.text = cityCardData.CityName;
            //countryName.text = cityCardData.CountryName;
            artwork.sprite = cityCardData.MainArtwork;
            flag.sprite = cityCardData.FlagArtwork;
            virusBottom.color = cityCardData.VirusInfo.virusColor;

            
        }

        public void SetBorderVisibility(bool isVisible) => border.gameObject.SetActive(isVisible);
    }
}
