using UnityEngine;
using System;

[DisallowMultipleComponent]
public class BinaKisla2D : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject soldierPrefab;
    public Taraf2D taraf = Taraf2D.Dost;
    [Min(0.3f)] public float spawnInterval = 3f;
    public Transform spawnPoint;
    public Vector2 spawnKick = new Vector2(0f, 0.3f);
    public bool autoStart = true;

    float _t;
    public float Ilerleme01 => Mathf.Clamp01(_t / Mathf.Max(0.01f, spawnInterval));

    public event Action<float> onIlerleme;

    void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
        _t = 0f;
    }

    void Update()
    {
        if (!autoStart || soldierPrefab == null) return;

        _t += Time.deltaTime;
        onIlerleme?.Invoke(Ilerleme01);

        if (_t >= spawnInterval)
        {
            _t = 0f;
            var go = Instantiate(soldierPrefab, spawnPoint.position, Quaternion.identity);

            // Tüm ilgili componentlere tek hamlede tarafý yay
            var s = go.GetComponent<SaglikBirim2D>();
            if (s != null) s.SetTaraf(taraf);

            go.transform.position += (Vector3)spawnKick;
        }
    }

    /// <summary>
    /// Sýfýrlama: üretim sayaçlarýný sýfýrla, varsa invoke/coroutine'leri durdur,
    /// ve üretimi durdur (component devre dýþý býrakýlýr ve autoStart false yapýlýr).
    /// Çaðrýldýktan sonra üretim durur; tekrar baþlatmak istersen enabled = true ve autoStart = true yap.
    /// </summary>
    public void Sifirla()
    {
        // Sýfýrla zamanlayýcýyý
        _t = 0f;

        // Ýlerleme bilgisini güncelle (0)
        onIlerleme?.Invoke(Ilerleme01);

        // Eðer invoke veya coroutine kullanýldýysa durdur
        CancelInvoke();
        StopAllCoroutines();

        // Üretimi durdur: component ve flag
        autoStart = false;
        enabled = false;
    }
}
