using UnityEngine;

[DisallowMultipleComponent]
public class BirimSavas2D : MonoBehaviour
{
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Saldırı")]
    public float attackDamage = 2f;
    public float attackRate = 1.2f;
    public float atakMenzili = 0.6f;

    [Header("Algılama")]
    public float detectRadius = 2.5f;
    public float retargetInterval = 0.25f;

    float _atkTimer;
    float _reTimer;

    public Transform AktifHedef { get; private set; }
    Component _hedefSaglik;

    [Header("Gizmos (Scene Görsel)")]
    public bool gizmoHerZaman = true;
    public bool gizmoSeciliyken = true;
    public bool hedefCiz = true;

    public Color renkDetect = new Color(1f, 0.95f, 0.2f, 0.9f);
    public Color renkAtak = new Color(1f, 0.2f, 0.2f, 0.9f);
    public Color renkHedef = new Color(0.2f, 1f, 1f, 0.9f);

    void Awake()
    {
        var s = GetComponent<SaglikBirim2D>();
        if (s != null) taraf = s.taraf;
    }

    void Update()
    {
        _reTimer -= Time.deltaTime;
        _atkTimer -= Time.deltaTime;

        if (AktifHedef == null || _reTimer <= 0f)
        {
            _reTimer = retargetInterval;
            AraVeHedefBelirle();
        }

        if (AktifHedef == null) return;

        float distSurface = YuzeyeMesafe(AktifHedef, transform.position);
        if (distSurface <= atakMenzili && _atkTimer <= 0f)
        {
            _atkTimer = attackRate;
            Vur();
        }

        if (AktifHedef == null || (!_hedefSaglik))
        {
            AktifHedef = null;
            _hedefSaglik = null;
        }
    }

    void AraVeHedefBelirle()
    {
        Transform enYakin = null;
        Component saglik = null;
        float enKucuk = float.MaxValue;

        var askerler = Object.FindObjectsByType<SaglikBirim2D>(FindObjectsSortMode.None);
        for (int i = 0; i < askerler.Length; i++)
        {
            var a = askerler[i];
            if (a == null || a.taraf == taraf) continue;
            float d = YuzeyeMesafe(a.transform, transform.position);
            if (d > detectRadius) continue;
            if (d < enKucuk) { enKucuk = d; enYakin = a.transform; saglik = a; }
        }

        var binalar = Object.FindObjectsByType<SaglikBina2D>(FindObjectsSortMode.None);
        for (int i = 0; i < binalar.Length; i++)
        {
            var b = binalar[i];
            if (b == null || b.taraf == taraf) continue;
            float d = YuzeyeMesafe(b.transform, transform.position);
            if (d > detectRadius) continue;
            if (enYakin != null && !(d < enKucuk)) continue;

            enKucuk = d; enYakin = b.transform; saglik = b;
        }

        AktifHedef = enYakin;
        _hedefSaglik = saglik;
    }

    void Vur()
    {
        if (_hedefSaglik is SaglikBirim2D sb) sb.HasarAl(attackDamage);
        else if (_hedefSaglik is SaglikBina2D bb) bb.HasarAl(attackDamage);

        var mov = GetComponent<BirimHareket2D>();
        if (mov != null && AktifHedef != null)
        {
            Vector3 hedefNokta = AktifHedef.position;
            var col = AktifHedef.GetComponent<Collider2D>();
            if (col != null) hedefNokta = col.ClosestPoint(transform.position);
            mov.KocbasiAt(hedefNokta);
        }
    }

    public static float YuzeyeMesafe(Transform hedef, Vector3 from)
    {
        var col = hedef.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 cp = col.ClosestPoint(from);
            return Vector2.Distance(from, cp);
        }
        return Vector2.Distance(from, hedef.position);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (gizmoHerZaman) CizGizmos();
    }
    void OnDrawGizmosSelected()
    {
        if (gizmoSeciliyken) CizGizmos();
    }
    void CizGizmos()
    {
        var p = transform.position;
        Gizmos.color = renkDetect; Gizmos.DrawWireSphere(p, detectRadius);
        Gizmos.color = renkAtak; Gizmos.DrawWireSphere(p, atakMenzili);

        if (hedefCiz && AktifHedef != null)
        {
            Vector3 hedefNokta = AktifHedef.position;
            var col = AktifHedef.GetComponent<Collider2D>();
            if (col != null) hedefNokta = col.ClosestPoint(p);

            Gizmos.color = renkHedef;
            Gizmos.DrawLine(p, hedefNokta);
            Gizmos.DrawSphere(hedefNokta, 0.06f);
        }
    }
#endif
}
