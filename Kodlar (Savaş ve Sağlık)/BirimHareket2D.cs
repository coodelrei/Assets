using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BirimSavas2D))]
public class BirimHareket2D : MonoBehaviour
{
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Boþtayken Yol Yönü")]
    public float hýz = 1.5f;
    public float salýnýmGen = 0.15f;
    public float salýnýmHýz = 2.2f;

    [Header("Takip")]
    public float takipHýzý = 2.2f;

    [Header("Koçbaþý (Vuruþ animasyonu)")]
    public bool kocbasiAktif = true;
    [Tooltip("Geri kaçýþ mesafesi (dünya birimi)")]
    public float kocbasiGeri = 0.12f;
    [Tooltip("Ýleri hamle mesafesi (dünya birimi)")]
    public float kocbasiIleri = 0.22f;
    [Tooltip("Toplam süre (sn)")]
    public float kocbasiSure = 0.18f;
    [Tooltip("Animasyon sonunda baþlangýç konumuna geri dönsün (drift olmasýn)")]
    public bool kocbasiSondaGeriDon = true;

    BirimSavas2D _savas;
    Vector2 _baseDir;
    bool _overrideMove;
    Coroutine _kocbasiCR;

    void Awake()
    {
        _savas = GetComponent<BirimSavas2D>();
        if (_savas != null) taraf = _savas.taraf;
        ApplyTaraf();
    }

    public void ApplyTaraf()
    {
        _baseDir = (taraf == Taraf2D.Dusman) ? Vector2.down : Vector2.up;
        if (_savas != null) _savas.taraf = taraf;
    }

    void Update()
    {
        if (_savas == null) return;
        if (_overrideMove) return; // koçbaþý oynarken normal hareketi durdur

        var hedefTr = _savas.AktifHedef;
        if (hedefTr != null)
        {
            // Hedefin en yakýn yüzey noktasý
            Vector3 hedefNokta = hedefTr.position;
            var hedefCol = hedefTr.GetComponent<Collider2D>();
            if (hedefCol != null)
                hedefNokta = hedefCol.ClosestPoint(transform.position);

            float stop = Mathf.Max(0.03f, _savas.atakMenzili * 0.9f);
            Vector3 delta = (hedefNokta - transform.position); delta.z = 0f;
            if (delta.magnitude > stop)
                transform.position += delta.normalized * (takipHýzý * Time.deltaTime);

            return; // hedef varken boþtaki akýþ yok
        }

        // Boþtayken aþaðý/yukarý + hafif sað/sol salýným
        float sway = Mathf.Sin(Time.time * salýnýmHýz) * salýnýmGen;
        Vector3 dir = new Vector3(_baseDir.x, _baseDir.y, 0f);
        Vector3 right = Vector3.right * sway;
        transform.position += (dir * hýz * Time.deltaTime) + right * Time.deltaTime;
    }

    // === Koçbaþý API ===
    public void KocbasiAt(Vector3 hedefNokta)
    {
        if (!kocbasiAktif) return;
        if (_kocbasiCR != null) StopCoroutine(_kocbasiCR);
        _kocbasiCR = StartCoroutine(KocbasiRoutine(hedefNokta));
    }

    IEnumerator KocbasiRoutine(Vector3 hedefNokta)
    {
        _overrideMove = true;

        Vector3 start = transform.position;
        Vector3 dir = (hedefNokta - start);
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = _baseDir;
        else dir.Normalize();

        float half = Mathf.Max(0.01f, kocbasiSure * 0.45f);
        float second = Mathf.Max(0.01f, kocbasiSure - half);

        // 1) Geri kaçýþ (easeOut)
        Vector3 geriPos = start - dir * kocbasiGeri;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            float e = EaseOutQuad(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(start, geriPos, e);
            yield return null;
        }

        // 2) Ýleri hamle (easeIn)
        Vector3 ileriPos = kocbasiSondaGeriDon ? start : start + dir * kocbasiIleri;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / second;
            float e = EaseInQuad(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(geriPos, ileriPos, e);
            yield return null;
        }

        // Net drift olmasýn
        if (kocbasiSondaGeriDon) transform.position = start;

        _overrideMove = false;
        _kocbasiCR = null;
    }

    static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
    static float EaseInQuad(float x) => x * x;

    // Eski API stublarý
    public void SetDurdur(bool _) { }
    public void TemizleTakip() { }
    public void SetTakipNokta(Vector3 _) { }
    public void SetTakipHedef(Transform _) { }
}
