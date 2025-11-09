using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GoldPickup : MonoBehaviour
{
    [Header("Altýn")]
    public int altinMiktari = 1;   // <-- SaglikBirim2D bunu bekliyor
    public int amount = 1;         // Geriye dönük uyum için; altinMiktari ile senkron tutulur

    [Header("Zamanlar")]
    public float delayMin = 1.0f;
    public float delayMax = 2.0f;
    public float travelTime = 0.6f;

    [Header("Görsel")]
    public AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeEase = AnimationCurve.Linear(0, 1, 1, 0);
    public AnimationCurve scaleEase = AnimationCurve.EaseInOut(0, 1, 1, 0.6f);
    public SpriteRenderer spriteRenderer;

    [Header("Hedef")]
    [Tooltip("Belirtilirse coin buraya uçar. Boþsa GoldManager.targetWorldPos kullanýlýr.")]
    public Transform targetOverride;

    void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Senkron
        amount = altinMiktari;
    }

    void OnValidate()
    {
        // Ýki alaný senkron tut
        if (amount != altinMiktari)
        {
            // Inspector’da hangisi deðiþtiyse diðeriyle eþitle
            // Basit strateji: altinMiktari ana alan; amount ona uyar.
            amount = altinMiktari;
        }
    }

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Çalýþma anýnda da senkron kal
        amount = altinMiktari;
    }

    void OnEnable()
    {
        StartCoroutine(CoFlyToTarget());
    }

    IEnumerator CoFlyToTarget()
    {
        float wait = Random.Range(delayMin, delayMax);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;

        if (targetOverride != null)
        {
            targetPos = targetOverride.position;
        }
        else if (GoldManager.Instance != null)
        {
            targetPos = GoldManager.Instance.targetWorldPos;
        }

        targetPos.z = startPos.z;

        float t = 0f;
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, travelTime);
            float m = moveEase.Evaluate(t);
            float f = fadeEase.Evaluate(t);
            float s = scaleEase.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, m);
            transform.localScale = startScale * s;

            if (spriteRenderer != null)
            {
                var c = startColor; c.a = Mathf.Clamp01(f);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        // Altýn ekle
        if (GoldManager.Instance != null && altinMiktari > 0)
            GoldManager.Instance.AddGold(altinMiktari);

        Destroy(gameObject);
    }
}
