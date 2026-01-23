using OPEN.PandemicAI;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace OPEN.PandemicAI
{
    public class PlayerHandView : MonoBehaviour
    {
        [SerializeField] public Transform container;
        [SerializeField] GameObject cardPrefab;
        [SerializeField] HorizontalLayoutGroup layout;

        [Serializable]
        struct LayoutPreset
        {
            public Vector3 scale;
            public Vector3 localPos;
            public int paddingLeft;
            public float spacing;
            public bool expandWidth;
            public bool controlWidth;
        }

        [Header("Presets 0,1,2")]
        [SerializeField] LayoutPreset[] presets = new LayoutPreset[3];

        public readonly Dictionary<int, GameObject> pool = new();

        public event Action<int> CardClicked;

        public void ApplyPreset(int index)
        {
            var p = presets[index];
            layout.childForceExpandWidth = p.expandWidth;
            layout.childControlWidth = p.controlWidth;
            layout.transform.localScale = p.scale;
            layout.transform.localPosition = p.localPos;
            layout.padding.left = p.paddingLeft;
            layout.spacing = p.spacing;
        }

        public GameObject GetLastActiveCard()
        {
            return pool.LastOrDefault(kv => kv.Value.activeSelf).Value;
        }

        public void SetHand(IEnumerable<int> ids, IReadOnlyList<int> selected)
        {
            foreach (var id in ids)
            {
                if (!pool.TryGetValue(id, out var go))
                {
                    go = pool[id] = Instantiate(cardPrefab, container);
                    go.GetComponent<CityCardDisplay>().CityCardData = CityDrawer.CityScripts[id].CityCard;
                }

                go.SetActive(true);
                go.GetComponent<CityCardDisplay>().SetBorderVisibility(selected.Contains(id));

                var btn = go.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => CardClicked?.Invoke(id));

            }

            foreach (var kv in pool.Where(kv => !ids.Contains(kv.Key)))
                kv.Value.SetActive(false);

        }
        public void CommunicateHandPositionToFurhat()
        {
            // Get the parent PlayerPanel component
            PlayerPanel parentPanel = GetComponentInParent<PlayerPanel>();
            foreach (var kv in pool.Where(kv => kv.Value.activeSelf))
            {
                var go = kv.Value;
                //Debug.Log($"Setting item placement for card {kv.Key} at position {go.GetComponent<RectTransform>().anchoredPosition}");

                if (parentPanel.IsAIControlled)
                {
                    FurhatGameMap.Instance.SetItemPlacement("card" + kv.Key.ToString(), go.GetComponent<RectTransform>());
                }

            }
        }
    }
}
