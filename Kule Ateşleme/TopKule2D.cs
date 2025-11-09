using UnityEngine;

[DisallowMultipleComponent]
public class TopKule2D : MonoBehaviour
{
    public Taraf2D taraf = Taraf2D.Dost;

    public Transform platform;
    public Transform topGövde;
    public Transform namluUcu;

    public float detectRadius = 4.5f;
    public float donusHiziDegSn = 360f;
    public float hedefYenileAraligi = 0.25f;

    public TopMermi2D mermiPrefab;
    public float atisGecikmesi = 0.5f;
    public float mermiHizi = 6f;
    public float mermiHasar = 3f;

    public float namluUzunluk = 0.35f;

    Transform _hedef;
    float _reTimer, _atkTimer;

    void Awake()
    {
        if (platform == null) platform = transform.Find("Platform");
        if (topGövde == null) topGövde = transform.Find("Cannon") ?? transform;
    }

    void Update()
    {
        _reTimer -= Time.deltaTime;
        _atkTimer -= Time.deltaTime;

        if (_reTimer <= 0f)
        {
            _reTimer = Mathf.Max(0.05f, hedefYenileAraligi);
            _hedef = EnYakinDusman();
        }

        if (topGövde == null) return;

        if (_hedef != null)
        {
            Vector3 hedef = HedefNoktaYuzey(_hedef);
            Vector2 dir = (hedef - topGövde.position);
            float hedefAci = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            float mevcutZ = topGövde.eulerAngles.z;
            float yeniZ = Mathf.MoveTowardsAngle(mevcutZ, hedefAci, donusHiziDegSn * Time.deltaTime);
            var e = topGövde.eulerAngles; e.z = yeniZ; topGövde.eulerAngles = e;

            float yuzeyMesafe = BirimSavas2D.YuzeyeMesafe(_hedef, topGövde.position);
            if (_atkTimer <= 0f && yuzeyMesafe <= detectRadius * 1.05f)
            {
                Atis();
                _atkTimer = Mathf.Max(0.05f, atisGecikmesi);
            }
        }
    }

    Transform EnYakinDusman()
    {
        Transform enYakin = null;
        float enKucuk = float.MaxValue;

        // — Birimler —
#if UNITY_2023_1_OR_NEWER
        var askerler = Object.FindObjectsByType<SaglikBirim2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var askerler = Object.FindObjectsOfType<SaglikBirim2D>();
#endif
        for (int i = 0; i < askerler.Length; i++)
        {
            var a = askerler[i];
            if (a == null || a.taraf == taraf) continue;
            float d = BirimSavas2D.YuzeyeMesafe(a.transform, topGövde.position);
            if (d <= detectRadius && d < enKucuk) { enKucuk = d; enYakin = a.transform; }
        }

        // — Binalar —
#if UNITY_2023_1_OR_NEWER
        var binalar = Object.FindObjectsByType<SaglikBina2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var binalar = Object.FindObjectsOfType<SaglikBina2D>();
#endif
        for (int i = 0; i < binalar.Length; i++)
        {
            var b = binalar[i];
            if (b == null || b.taraf == taraf) continue;
            float d = BirimSavas2D.YuzeyeMesafe(b.transform, topGövde.position);
            if (d <= detectRadius && d < enKucuk) { enKucuk = d; enYakin = b.transform; }
        }

        return enYakin;
    }

    void Atis()
    {
        if (mermiPrefab == null) return;

        Vector3 pos;
        Quaternion rot = topGövde.rotation;
        if (namluUcu != null) pos = namluUcu.position;
        else pos = topGövde.position + (topGövde.up * namluUzunluk);

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
            Vector2 cp = col.ClosestPoint(topGövde.position);
            return new Vector3(cp.x, cp.y, 0f);
        }
        return t.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.9f, 0.85f);
        Gizmos.DrawWireSphere((topGövde != null ? topGövde.position : transform.position), detectRadius);
    }
#endif
}
