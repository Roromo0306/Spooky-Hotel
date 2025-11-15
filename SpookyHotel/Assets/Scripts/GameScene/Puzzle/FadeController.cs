using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public Image blackImage;
    public float duration = 1f;

    private void Awake() { if (blackImage != null) blackImage.gameObject.SetActive(false); }

    public IEnumerator FadeOutCoroutine()
    {
        if (blackImage == null) yield break;
        blackImage.gameObject.SetActive(true);
        Color c = blackImage.color;
        c.a = 0f;
        blackImage.color = c;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            blackImage.color = c;
            yield return null;
        }
    }

    public IEnumerator FadeInCoroutine()
    {
        if (blackImage == null) yield break;
        Color c = blackImage.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / duration);
            blackImage.color = c;
            yield return null;
        }
        blackImage.gameObject.SetActive(false);
    }
}
