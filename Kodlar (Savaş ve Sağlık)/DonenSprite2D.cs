using UnityEngine;

/// <summary>
/// Sadece görseli döndürür (collider/movement etkilenmez).
/// Hedef boþsa, ilk SpriteRenderer child'ýný otomatik bulur.
/// </summary>
[DisallowMultipleComponent]
public class DonenSprite2D : MonoBehaviour
{
    [Header("Hedef")]
    [Tooltip("Genelde 'Sprite' child'ý. Boþsa otomatik bulunur.")]
    public Transform hedef;

    [Header("Dönüþ")]
    [Tooltip("Saniyedeki derece. (+) saat yönü, (-) tersi.")]
    public float dereceSn = 180f;
    [Tooltip("TimeScale'den etkilenmesin istiyorsan aç.")]
    public bool unscaledTime = false;
    [Tooltip("Baþlangýçta rastgele bir açý uygula.")]
    public bool rastgeleBaslangicAcisi = false;

    [Header("Yaþam Döngüsü")]
    [Tooltip("Objeyle birlikte otomatik baþlasýn.")]
    public bool otomatikBasla = true;

    bool aktif;
    Transform _auto;

    void Awake()
    {
        if (hedef == null)
        {
            // Adý 'Sprite' olan child'ý dene
            var t = transform.Find("Sprite");
            if (t != null) _auto = t;
            else
            {
                // Ýlk SpriteRenderer'ý bul
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null) _auto = sr.transform;
            }
        }
        if (rastgeleBaslangicAcisi)
        {
            var h = hedef ?? _auto ?? transform;
            var e = h.localEulerAngles;
            e.z = Random.Range(0f, 360f);
            h.localEulerAngles = e;
        }
    }

    void OnEnable()
    {
        aktif = otomatikBasla;
    }

    void Update()
    {
        if (!aktif) return;

        var h = hedef ?? _auto ?? transform;
        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Z ekseninde döndür
        h.Rotate(0f, 0f, dereceSn * dt, Space.Self);
    }

    // --- Dýþarýdan kontrol için ---
    public void Baslat() => aktif = true;
    public void Durdur() => aktif = false;
    public void SetHiz(float yeniDegSn) => dereceSn = yeniDegSn;
}
