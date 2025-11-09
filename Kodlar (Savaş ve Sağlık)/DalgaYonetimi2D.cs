using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DalgaYonetimi2D : MonoBehaviour
{
    [System.Serializable] public class Grup { public GameObject prefab; [Min(1)] public int adet = 5; [Min(0.05f)] public float aralik = 0.4f; }
    [System.Serializable] public class Dalga { public List<Grup> gruplar = new(); public float gruplarArasiBekleme = 0.5f; }

    [Header("Genel")]
    public bool sahneAcilincaOtoBasla = true;
    [Min(1)] public int baslangicWaveIndex = 1;
    public List<Dalga> dalgalar = new();

    [Header("Spawn Noktaları")]
    public Transform tekSpawnNoktasi;
    public Transform spawnHatBas;
    public Transform spawnHatBit;

    [Header("UI")]
    public TextMeshProUGUI waveText;

    [Header("Panel Animasyonu")]
    public RectTransform panelToSlide;
    public float panelKaymaX = -800f;
    public float panelAnimSuresi = 0.5f;

    [Header("Kamera Animasyonu")]
    public float kameraHedefY = 0f;
    public float kameraAnimSuresi = 0.5f;

    [Header("Shop Panel (opsiyonel)")]
    public GameObject shopPanelObj;
    public bool waveBitinceAnimasyonYap = true;

    [Header("Test")]
    public bool testKisayoluAktif = true;
    public KeyCode testWaveBitirKey = KeyCode.F9;

    [SerializeField, Min(1)] int currentWave = 1;
    Coroutine _coSpawn, _coTakip;
    bool _spawnBitti;

    // ===== Uyuyan (wave başlayana kadar kapalı) binalar =====
    static DalgaYonetimi2D _inst;
    readonly List<GameObject> _dormant = new();
    void Awake() { _inst = this; }
    void OnDestroy() { if (_inst == this) _inst = null; }

    /// <summary>Shop’tan bırakılan binayı “uyuyan” olarak kaydet.
    /// Yeni dalga başlarken otomatik aktifleşir.</summary>
    public static void RegisterDormant(GameObject go)
    {
        if (_inst == null || go == null) return;
        if (!_inst._dormant.Contains(go)) _inst._dormant.Add(go);
    }

    void Start()
    {
        currentWave = Mathf.Max(1, baslangicWaveIndex);
        UIWaveYaz();
        if (sahneAcilincaOtoBasla) BaslatDalga(currentWave);
    }

    void Update()
    {
        if (testKisayoluAktif && Input.GetKeyDown(testWaveBitirKey)) ForceWaveComplete();
    }

    // ===== Dalga =====
    public void BaslatDalga(int waveIndex1Based)
    {
        if (_coSpawn != null) StopCoroutine(_coSpawn);
        if (_coTakip != null) StopCoroutine(_coTakip);

        // <<< Uyuyan binaları aktifleştir
        ActivateDormantBuildings();

        _spawnBitti = false;
        currentWave = Mathf.Clamp(waveIndex1Based, 1, Mathf.Max(1, dalgalar.Count));
        UIWaveYaz();

        var d = (dalgalar.Count >= currentWave) ? dalgalar[currentWave - 1] : null;
        _coSpawn = StartCoroutine(CoSpawn(d));
        _coTakip = StartCoroutine(CoTakipVeBitis());
    }

    void ActivateDormantBuildings()
    {
        for (int i = _dormant.Count - 1; i >= 0; i--)
        {
            var go = _dormant[i];
            if (!go) { _dormant.RemoveAt(i); continue; }

            var sag = go.GetComponent<SaglikBina2D>();
            if (sag) { sag.enabled = true; sag.ResetBina(); } // canı full + sağlık aç

            var kisla = go.GetComponent<BinaKisla2D>();
            if (kisla) { kisla.enabled = true; kisla.autoStart = true; } // spawn aç

            _dormant.RemoveAt(i);
        }
    }

    IEnumerator CoSpawn(Dalga d)
    {
        if (d == null) { _spawnBitti = true; yield break; }

        for (int gi = 0; gi < d.gruplar.Count; gi++)
        {
            var g = d.gruplar[gi];
            if (g == null || g.prefab == null || g.adet <= 0) continue;

            for (int i = 0; i < g.adet; i++)
            {
                SpawnDusman(g.prefab);
                yield return new WaitForSeconds(g.aralik);
            }
            if (d.gruplarArasiBekleme > 0f) yield return new WaitForSeconds(d.gruplarArasiBekleme);
        }
        _spawnBitti = true;
    }

    void SpawnDusman(GameObject prefab)
    {
        Vector3 pos = tekSpawnNoktasi ? tekSpawnNoktasi.position :
                       (spawnHatBas && spawnHatBit) ? Vector3.Lerp(spawnHatBas.position, spawnHatBit.position, Random.value)
                                                    : transform.position;

        var go = Instantiate(prefab, pos, Quaternion.identity);

        var s = go.GetComponent<SaglikBirim2D>();
        if (s != null)
        {
            var m = s.GetType().GetMethod("SetTaraf");
            if (m != null) m.Invoke(s, new object[] { Taraf2D.Dusman });
            else s.taraf = Taraf2D.Dusman;
        }
    }

    IEnumerator CoTakipVeBitis()
    {
        yield return null;
        while (true)
        {
#if UNITY_2023_1_OR_NEWER
            var hepsi = Object.FindObjectsByType<SaglikBirim2D>(FindObjectsSortMode.None);
#else
            var hepsi = Object.FindObjectsOfType<SaglikBirim2D>();
#endif
            int dusman = 0;
            for (int i = 0; i < hepsi.Length; i++)
            {
                var u = hepsi[i];
                if (u == null || u.taraf != Taraf2D.Dusman) continue;

                float dolu = 1f;
                var prop = u.GetType().GetProperty("Doluluk01");
                if (prop != null && prop.PropertyType == typeof(float))
                    dolu = (float)prop.GetValue(u, null);
                if (dolu > 0f) dusman++;
            }

            if (_spawnBitti && dusman == 0) break;
            yield return new WaitForSeconds(0.25f);
        }
        OnWaveComplete();
    }

    // ===== Wave Complete =====
    public void OnWaveComplete()
    {
        KillAllUnitsNow();
        ResetAllBuildingsNow();
        UIWaveYaz("Wave Complete!");
        PrepareShopPanel();
        if (waveBitinceAnimasyonYap) StartCoroutine(CoPanelVeKamera());
        currentWave++;
    }

    public void ForceWaveComplete()
    {
        if (_coSpawn != null) StopCoroutine(_coSpawn);
        if (_coTakip != null) StopCoroutine(_coTakip);
        _spawnBitti = true;
        OnWaveComplete();
    }

    // ===== UI & Anim =====
    void UIWaveYaz(string extra = null)
    {
        if (!waveText) return;
        waveText.text = extra == null ? $"Wave {currentWave}" : extra;
    }

    void PrepareShopPanel()
    {
        if (!shopPanelObj) return;
        shopPanelObj.SendMessage("PopulateForWave", currentWave, SendMessageOptions.DontRequireReceiver);
        shopPanelObj.SendMessage("Generate", SendMessageOptions.DontRequireReceiver);
        if (!shopPanelObj.activeSelf) shopPanelObj.SetActive(true);
    }

    IEnumerator CoPanelVeKamera()
    {
        Vector2 pBas = panelToSlide ? panelToSlide.anchoredPosition : Vector2.zero;
        Vector2 pBit = pBas + new Vector2(panelKaymaX, 0f);

        var cam = Camera.main;
        Vector3 cBas = cam ? cam.transform.position : Vector3.zero;
        Vector3 cBit = cBas; cBit.y = kameraHedefY;

        float t = 0f, sure = Mathf.Max(panelAnimSuresi, kameraAnimSuresi);
        while (t < sure)
        {
            t += Time.deltaTime;
            float uP = panelAnimSuresi > 0 ? Mathf.Clamp01(t / panelAnimSuresi) : 1f;
            float uC = kameraAnimSuresi > 0 ? Mathf.Clamp01(t / kameraAnimSuresi) : 1f;
            if (panelToSlide) panelToSlide.anchoredPosition = Vector2.Lerp(pBas, pBit, uP);
            if (cam) cam.transform.position = Vector3.Lerp(cBas, cBit, uC);
            yield return null;
        }
        if (panelToSlide) panelToSlide.anchoredPosition = pBit;
        if (cam) cam.transform.position = cBit;
    }

    // ===== Yardımcılar =====
    public void KillAllUnitsNow()
    {
#if UNITY_2023_1_OR_NEWER
        var birimler = Object.FindObjectsByType<SaglikBirim2D>(FindObjectsSortMode.None);
#else
        var birimler = Object.FindObjectsOfType<SaglikBirim2D>();
#endif
        for (int i = 0; i < birimler.Length; i++)
        {
            var u = birimler[i];
            if (u) Destroy(u.gameObject);
        }
    }

    public void ResetAllBuildingsNow()
    {
#if UNITY_2023_1_OR_NEWER
        var binalar = Object.FindObjectsByType<SaglikBina2D>(FindObjectsSortMode.None);
#else
        var binalar = Object.FindObjectsOfType<SaglikBina2D>();
#endif
        for (int i = 0; i < binalar.Length; i++)
        {
            var b = binalar[i];
            if (!b) continue;
            b.ResetBina(); // can full + içeride BinaKisla2D.Sifirla()
        }
    }
}
