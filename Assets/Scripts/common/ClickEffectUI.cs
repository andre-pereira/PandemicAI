using UnityEngine;
using UnityEngine.UI;

public class ClickEffectUI : MonoBehaviour
{
    public static ClickEffectUI Instance { get; private set; }

    public Image clickEffectPrefab;
    public Canvas canvas;

    public float animationDuration = 0.5f;
    public float maxScale = 2f;
    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 clickPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out clickPos
            );

            Image effect = Instantiate(clickEffectPrefab, canvas.transform);
            effect.gameObject.SetActive(true);
            effect.rectTransform.anchoredPosition = clickPos;
            StartCoroutine(PlayClickEffect(effect));
        }
    }

    public void ShowClickEffectAt(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            canvas.worldCamera,
            out localPoint
        );

        Image effect = Instantiate(clickEffectPrefab, canvas.transform);
        effect.gameObject.SetActive(true);
        effect.rectTransform.anchoredPosition = localPoint;
        StartCoroutine(PlayClickEffect(effect));
    }

    private System.Collections.IEnumerator PlayClickEffect(Image effect)
    {
        float time = 0f;
        Color startColor = effect.color;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * maxScale;

        while (time < animationDuration)
        {
            float t = time / animationDuration;
            effect.color = new Color(startColor.r, startColor.g, startColor.b, 1 - t);
            effect.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(effect.gameObject);
    }
}

