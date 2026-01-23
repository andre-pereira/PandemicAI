namespace OPEN.PandemicAI
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    [CreateAssetMenu(fileName = "GameAssetLibrary", menuName = "PandemicAI/GameAssetLibrary")]
    public class GameAssetLibrary : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField] public GameObject cityCardPrefab;
        [SerializeField] public GameObject infectionCardPrefab;
        [SerializeField] public GameObject cubePrefab;
        [SerializeField] public GameObject epidemicCardPrefab;
        [SerializeField] public GameObject cureVialPrefab;
        [SerializeField] public GameObject infectionCardBackPrefab;
        [SerializeField] public GameObject outbreakMarkerPrefab;
        [SerializeField] public GameObject infectionRateMarkerPrefab;
        [SerializeField] public GameObject pawnPrefab;
        [SerializeField] public GameObject playerAreaPrefab;
        [SerializeField] public GameObject roleCardPrefab;

        [Header("Materials")]
        [SerializeField] public Material lineMaterial;
    }
}