using UnityEngine;

/// <summary>
/// Hedef yokken hat boyunca yürür (Dost: +Y / Düşman: -Y), hafif sağ-sol salınım.
/// Hedef varsa ve menzil DIŞIndaysa hedefe doğru yürür.
/// Hedef menzildeyse durur (yerinde kalır) — backoff yok.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HareketliTop2D))]
public class HareketliTopHareket2D : MonoBehaviour
{
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Boştayken (hat boyunca)")]
    public float hiz = 1.6f;
    public float salinimGen = 0.12f;
    public float salinimHiz = 2.0f;

    [Header("Takip (hedef menzil dışındaysa)")]
    public float takipHizi = 2.0f;

    HareketliTop2D _top;
    Vector2 _baseDir;

    void Awake()
    {
        _top = GetComponent<HareketliTop2D>();
        // Tarafı top’a da uygula (senkron kalsın)
        if (_top != null) _top.taraf = taraf;

        _baseDir = (taraf == Taraf2D.Dusman) ? Vector2.down : Vector2.up;
    }

    public void SetTaraf(Taraf2D t)
    {
        taraf = t;
        if (_top != null) _top.taraf = t;
        _baseDir = (t == Taraf2D.Dusman) ? Vector2.down : Vector2.up;
    }

    void Update()
    {
        var hedef = (_top != null) ? _top.AktifHedef : null;

        if (hedef != null && _top != null)
        {
            // Hedefin en yakın yüzey noktasını baz al
            Vector3 hedefNokta = hedef.position;
            var col = hedef.GetComponent<Collider2D>();
            if (col != null)
            {
                Vector2 cp = col.ClosestPoint(transform.position);
                hedefNokta = cp;
            }

            Vector3 delta = hedefNokta - transform.position; delta.z = 0f;
            float d = delta.magnitude;

            // KURAL:
            // - d <= detectRadius  → DUR (ateş ederken yerinde kal)
            // - d  > detectRadius  → HEDEFE YAKLAŞ
            float menzil = Mathf.Max(0.01f, _top.detectRadius);

            if (d > menzil)
            {
                // Hedef menzil dışındaysa hedefe doğru yürü
                transform.position += delta.normalized * (takipHizi * Time.deltaTime);
            }
            // else: menzilde → hiç hareket etme (yerinde kal)

            return;
        }

        // Hedef yoksa: hat boyunca ilerle + hafif salınım
        float sway = Mathf.Sin(Time.time * salinimHiz) * salinimGen;
        Vector3 dir = new Vector3(_baseDir.x, _baseDir.y, 0f);
        Vector3 right = Vector3.right * sway;
        transform.position += (dir * hiz * Time.deltaTime) + right * Time.deltaTime;
    }
}
