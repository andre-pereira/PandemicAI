using UnityEngine;

namespace OPEN.PandemicAI
{
    [CreateAssetMenu(fileName = "New City Card", menuName = "Cards/City Card")]
    public class CityCard : ScriptableObject
    {
        [SerializeField] private int cityID;

        [SerializeField] private int[] neighbors;

        [SerializeField] private string cityName;

        [SerializeField] private string countryName;

        [SerializeField] private VirusInfo virusInfo;

        [SerializeField] private Sprite mainArtwork;

        [SerializeField] private Sprite flagArtwork;

        public int CityID => cityID;

        public int[] Neighbors => neighbors;

        public string CityName => cityName;

        public string CountryName => countryName;

        public VirusInfo VirusInfo => virusInfo;

        public Sprite MainArtwork => mainArtwork;

        public Sprite FlagArtwork => flagArtwork;

        public bool IsValid()
        {
            return cityID >= 0
                && !string.IsNullOrWhiteSpace(cityName)
                && !string.IsNullOrWhiteSpace(countryName);
        }
    }
}
