using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SaglikBirim2D : MonoBehaviour
{
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Can")]
    public float maxHP = 10f;
    public float startHP = 10f;

    [Header("Ölünce Altın")]
    public int goldOnDeath = 1;
    public GameObject goldDropPrefab;
    public Vector3 dropOffset = new Vector3(0f, 0.2f, 0f);

    public float HP { get; private set; }
    public event Action<float, float> onDegisti;

    public float Doluluk01 => maxHP <= 0 ? 0 : Mathf.Clamp01(HP / maxHP);

    void Awake()
    {
        HP = Mathf.Clamp(startHP, 0, maxHP);
        PropagateTaraf();
        onDegisti?.Invoke(HP, maxHP);
    }

    public void SetTaraf(Taraf2D t)
    {
        taraf = t;
        PropagateTaraf();
    }

    void PropagateTaraf()
    {
        var sav = GetComponent<BirimSavas2D>();
        if (sav != null) sav.taraf = taraf;

        var mov = GetComponent<BirimHareket2D>();
        if (mov != null) { mov.taraf = taraf; mov.ApplyTaraf(); }
    }

    public void HasarAl(float dmg)
    {
        if (HP <= 0) return;
        HP = Mathf.Max(0, HP - Mathf.Max(0, dmg));
        onDegisti?.Invoke(HP, maxHP);

        if (HP <= 0)
        {
            if (taraf == Taraf2D.Dusman)
            {
                if (goldDropPrefab != null)
                {
                    Vector3 dropPos = transform.position + dropOffset;
                    var go = Instantiate(goldDropPrefab, dropPos, Quaternion.identity);

                    var gold = go.GetComponent<GoldPickup>();
                    if (gold != null) gold.altinMiktari = goldOnDeath;
                }
                else
                {
                    GoldManager.Instance?.AddGold(goldOnDeath);
                }
            }

            Destroy(gameObject);
        }
    }

    public void Iyilestir(float val)
    {
        HP = Mathf.Min(maxHP, HP + Mathf.Max(0, val));
        onDegisti?.Invoke(HP, maxHP);
    }
}
