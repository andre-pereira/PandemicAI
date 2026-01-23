using System;
using UnityEngine;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    public class DeckDrawer : MonoBehaviour
    {
        [SerializeField] public GameObject playerDeck;
        [SerializeField] public GameObject playerDeckDiscard;
        [SerializeField] public GameObject infectionDeck;
        [SerializeField] public GameObject infectionDiscard;

        public static GameObject PlayerDeck { get; private set; }
        public static GameObject PlayerDeckDiscard { get; private set; }
        public static GameObject InfectionDeck { get; private set; }
        public static GameObject InfectionDiscard { get; private set; }

        private static DeckDrawer _instance;

        public void Awake()
        {
            _instance = this;
            PlayerDeck = playerDeck;
            InfectionDeck = infectionDeck;
            PlayerDeckDiscard = playerDeckDiscard;
            InfectionDiscard = infectionDiscard;
        }

        public static GameObject SpawnPlayerCard(int cityId, Transform parent, Transform adjustTo = null)
            => _instance.InternalSpawn(cityId, parent, adjustTo);

        private GameObject InternalSpawn(int cityId, Transform parent, Transform adjustTo)
        {
            var cardGO = Instantiate(GameGUI.Assets.cityCardPrefab, parent);
            cardGO.GetComponent<CityCardDisplay>().CityCardData = CityDrawer.CityScripts[cityId].CityCard;

            if (adjustTo != null)
                cardGO.transform.SetPositionAndRotation(adjustTo.position, adjustTo.rotation);

            return cardGO;
        }

        public static void ExpandInfectionDeck()
        {
            int translationStep = 20;
            int translationValue = 20;

            int childCount = _instance.infectionDiscard.transform.childCount;

            if (childCount > 1)
            {
                for (int i = childCount - 1; i >= 0; i--)
                {
                    Transform child = _instance.infectionDiscard.transform.GetChild(i);
                    child.localPosition = new Vector3(translationValue, 0, 0);
                    translationValue -= translationStep;
                }
            }
        }

        public static void CollapseInfectionDeck()
        {
            foreach (Transform item in _instance.infectionDiscard.transform)
            {
                item.localPosition = new Vector3(0, 0, 0);
            }
        }
    }
}