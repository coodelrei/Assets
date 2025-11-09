using UnityEngine;

public class CanBarBaglayici2D : MonoBehaviour
{
    public enum BarTipi { BirimSaglik, BinaSaglik, BinaSpawn }

    [Header("Kimlik")]
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Ne tür bar?")]
    public BarTipi barTipi = BarTipi.BirimSaglik;

    [Header("Kaynak Bileşenler")]
    public SaglikBirim2D birimSaglik;   // BirimSaglik
    public SaglikBina2D binaSaglik;     // BinaSaglik
    public BinaKisla2D binaKisla;       // BinaSpawn

    [Header("Takip")]
    public Transform takip;
    public Vector3 localOffset = new Vector3(0f, 0.65f, 0f);
    public bool sabitDunyaBoyutu = true; // parent scale telafisi

    Transform _barRoot;
    DoldurmaCubugu2D _bar;

    const int ORDER_UNIT = 10;
    const int ORDER_BLD_HP = 20;
    const int ORDER_BLD_SP = 30;

    void Reset()
    {
        if (takip == null) takip = transform;
        if (birimSaglik == null)
            birimSaglik = GetComponent<SaglikBirim2D>() ?? GetComponentInParent<SaglikBirim2D>();
        if (binaSaglik == null)
            binaSaglik = GetComponent<SaglikBina2D>() ?? GetComponentInParent<SaglikBina2D>();
        if (binaKisla == null)
            binaKisla = GetComponent<BinaKisla2D>() ?? GetComponentInParent<BinaKisla2D>();

        if (birimSaglik != null) taraf = birimSaglik.taraf;
        else if (binaSaglik != null) taraf = binaSaglik.taraf;
    }

    void Awake()
    {
        if (takip == null) takip = transform;
        if (birimSaglik == null)
            birimSaglik = GetComponent<SaglikBirim2D>() ?? GetComponentInParent<SaglikBirim2D>();
        if (binaSaglik == null)
            binaSaglik = GetComponent<SaglikBina2D>() ?? GetComponentInParent<SaglikBina2D>();
        if (binaKisla == null)
            binaKisla = GetComponent<BinaKisla2D>() ?? GetComponentInParent<BinaKisla2D>();

        if (birimSaglik != null) taraf = birimSaglik.taraf;
        else if (binaSaglik != null) taraf = binaSaglik.taraf;

        KurulumVeyaYenile();
    }

    void OnEnable() { AboneOl(true); }
    void OnDisable() { AboneOl(false); }
    void OnDestroy() { if (_barRoot != null) Destroy(_barRoot.gameObject); }

    public void KurulumVeyaYenile()
    {
        var mgr = BarYoneticisi2D.Aktif;
        if (mgr == null)
        {
            Debug.LogWarning("[CanBarBaglayici2D] Sahnede BarYoneticisi2D yok.");
            return;
        }

        if (_barRoot != null) Destroy(_barRoot.gameObject);

        var go = new GameObject($"Bar_{barTipi}_{name}");
        _barRoot = go.transform;
        _barRoot.SetParent(takip != null ? takip : transform, false);
        _barRoot.localPosition = localOffset;

        _bar = go.AddComponent<DoldurmaCubugu2D>();

        mgr.SecimdenRenkVeGenislik(barTipi, taraf, out var bg, out var fill, out var istenenGenislik);
        float genislik = istenenGenislik;
        float yukseklik = mgr.barYukseklik;

        if (sabitDunyaBoyutu && _barRoot.parent != null)
        {
            var ps = _barRoot.parent.lossyScale;
            float sx = Mathf.Approximately(ps.x, 0f) ? 1f : ps.x;
            float sy = Mathf.Approximately(ps.y, 0f) ? 1f : ps.y;
            genislik = istenenGenislik / sx;
            yukseklik = mgr.barYukseklik / sy;
        }

        _bar.worldWidth = genislik;
        _bar.worldHeight = yukseklik;

        int order = (barTipi == BarTipi.BirimSaglik) ? ORDER_UNIT :
                    (barTipi == BarTipi.BinaSaglik) ? ORDER_BLD_HP : ORDER_BLD_SP;

        _bar.BuildIfNeeded(mgr, bg, fill, mgr.sortingLayerName, mgr.sortingOrderBase + order, mgr.worldZ);

        GuncelleAnlik();
        AboneOl(true);
    }

    void GuncelleAnlik()
    {
        if (_bar == null) return;
        switch (barTipi)
        {
            case BarTipi.BirimSaglik: if (birimSaglik != null) _bar.Set01(birimSaglik.Doluluk01); break;
            case BarTipi.BinaSaglik: if (binaSaglik != null) _bar.Set01(binaSaglik.Doluluk01); break;
            case BarTipi.BinaSpawn: if (binaKisla != null) _bar.Set01(binaKisla.Ilerleme01); break;
        }
    }

    void AboneOl(bool on)
    {
        if (on)
        {
            if (barTipi == BarTipi.BirimSaglik)
            {
                if (birimSaglik == null)
                    birimSaglik = GetComponent<SaglikBirim2D>() ?? GetComponentInParent<SaglikBirim2D>();

                if (birimSaglik != null)
                {
                    birimSaglik.onDegisti -= OnHP;
                    birimSaglik.onDegisti += OnHP;
                }
            }

            if (barTipi == BarTipi.BinaSaglik)
            {
                if (binaSaglik == null)
                    binaSaglik = GetComponent<SaglikBina2D>() ?? GetComponentInParent<SaglikBina2D>();

                if (binaSaglik != null)
                {
                    binaSaglik.onDegisti -= OnBinaHP;
                    binaSaglik.onDegisti += OnBinaHP;
                }
            }

            if (barTipi == BarTipi.BinaSpawn)
            {
                if (binaKisla == null)
                    binaKisla = GetComponent<BinaKisla2D>() ?? GetComponentInParent<BinaKisla2D>();

                if (binaKisla != null)
                {
                    binaKisla.onIlerleme -= OnSpawn;
                    binaKisla.onIlerleme += OnSpawn;
                }
            }
        }
        else
        {
            if (birimSaglik != null) birimSaglik.onDegisti -= OnHP;
            if (binaSaglik != null) binaSaglik.onDegisti -= OnBinaHP;
            if (binaKisla != null) binaKisla.onIlerleme -= OnSpawn;
        }
    }

    void OnHP(float now, float max) { _bar?.Set01(max <= 0 ? 0 : now / max); }
    void OnSpawn(float v01) { _bar?.Set01(Mathf.Clamp01(v01)); }

    // 🔧 Delegate uyumlu versiyon (ISaglik2D parametreli)
    void OnBinaHP(ISaglik2D saglik)
    {
        if (_bar != null && saglik != null)
            _bar.Set01(saglik.MaksCan <= 0 ? 0 : saglik.Can / saglik.MaksCan);
    }
}
