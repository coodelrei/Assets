using UnityEngine;

[DisallowMultipleComponent]
public class HareketliTop2D : MonoBehaviour
{
    [Header("Kimlik")]
    public Taraf2D taraf = Taraf2D.Dost; // Dost ise düþmaný arar

    [Header("Child Referanslarý")]
    public Transform topGovde;   // namluyu döndüreceðimiz child (örn: "Cannon")
    public Transform namluUcu;   // mermi çýkýþ noktasý (boþsa topGovde.up + ofset)

    [Header("Tespit & Dönüþ")]
    public float detectRadius = 4.5f;
    public float donusHiziDegSn = 360f;
    public float hedefYenileAraligi = 0.25f;

    [Header("Ateþ")]
    public TopMermi2D mermiPrefab;
    public float atisGecikmesi = 0.6f; // ROF
    public float mermiHizi = 6f;
    public float mermiHasar = 3f;
    public float namluUzunluk = 0.35f; // topGovde local +Y yönüne

    public Transform AktifHedef { get; private set; }

    float _reTimer, _atkTimer;

    void Awake()
    {
        if (topGovde == null)
            topGovde = transform.Find("Cannon") ?? transform;
    }

    void Update()
    {
        _reTimer -= Time.deltaTime;
        _atkTimer -= Time.deltaTime;

        if (_reTimer <= 0f)
        {
            _reTimer = Mathf.Max(0.05f, hedefYenileAraligi);
            AktifHedef = EnYakinDusman();
        }

        if (topGovde == null || AktifHedef == null) return;

        // Hedefe dön
        Vector3 hedef = HedefNoktaYuzey(AktifHedef);
        Vector2 dir = (hedef - topGovde.position);
        float hedefAci = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // up = ileri
        float mevcutZ = topGovde.eulerAngles.z;
        float yeniZ = Mathf.MoveTowardsAngle(mevcutZ, hedefAci, donusHiziDegSn * Time.deltaTime);
        var e = topGovde.eulerAngles; e.z = yeniZ; topGovde.eulerAngles = e;

        // Ateþ
        float yuzeyMesafe = BirimSavas2D.YuzeyeMesafe(AktifHedef, topGovde.position);
        if (_atkTimer <= 0f && yuzeyMesafe <= detectRadius * 1.05f)
        {
            Atis();
            _atkTimer = Mathf.Max(0.05f, atisGecikmesi);
        }
    }

    Transform EnYakinDusman()
    {
        Transform enYakin = null;
        float enKucuk = float.MaxValue;

        // Birimler
#if UNITY_2023_1_OR_NEWER
        var askerler = Object.FindObjectsByType<SaglikBirim2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var askerler = Object.FindObjectsOfType<SaglikBirim2D>();
#endif
        for (int i = 0; i < askerler.Length; i++)
        {
            var a = askerler[i];
            if (a == null || a.taraf == taraf) continue;
            float d = BirimSavas2D.YuzeyeMesafe(a.transform, topGovde.position);
            if (d <= detectRadius && d < enKucuk) { enKucuk = d; enYakin = a.transform; }
        }

        // Binalar
#if UNITY_2023_1_OR_NEWER
        var binalar = Object.FindObjectsByType<SaglikBina2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var binalar = Object.FindObjectsOfType<SaglikBina2D>();
#endif
        for (int i = 0; i < binalar.Length; i++)
        {
            var b = binalar[i];
            if (b == null || b.taraf == taraf) continue;
            float d = BirimSavas2D.YuzeyeMesafe(b.transform, topGovde.position);
            if (d <= detectRadius && d < enKucuk) { enKucuk = d; enYakin = b.transform; }
        }

        return enYakin;
    }

    void Atis()
    {
        if (mermiPrefab == null) return;

        Vector3 pos = namluUcu != null
            ? namluUcu.position
            : topGovde.position + (topGovde.up * namluUzunluk);
        Quaternion rot = topGovde.rotation;

        var m = Instantiate(mermiPrefab, pos, rot);
        m.taraf = taraf;
        m.hiz = mermiHizi;
        m.hasar = mermiHasar;
    }

    Vector3 HedefNoktaYuzey(Transform t)
    {
        var col = t.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 cp = col.ClosestPoint(topGovde.position);
            return new Vector3(cp.x, cp.y, 0f);
        }
        return t.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.9f, 0.85f);
        Gizmos.DrawWireSphere((topGovde != null ? topGovde.position : transform.position), detectRadius);
    }
#endif
}
