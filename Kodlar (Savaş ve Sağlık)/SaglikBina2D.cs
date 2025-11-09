using System;
using UnityEngine;

public class SaglikBina2D : MonoBehaviour, ISaglik2D
{
    public float maxSaglik = 100f;
    public float saglik = 100f;
    public Taraf2D taraf = Taraf2D.Dusman;
    private bool yokEdildi = false;

    public float Can => saglik;
    public float MaksCan => maxSaglik;
    public bool Hayatta => !yokEdildi;
    public float Doluluk01 => Mathf.Clamp01(saglik / maxSaglik);

    public event Action<ISaglik2D> onDegisti;

    public void HasarAl(float miktar)
    {
        saglik -= miktar;

        if (saglik <= 0)
        {
            yokEdildi = true;
            Destroy(gameObject);
        }

        onDegisti?.Invoke(this);
    }

    public void ResetBina()
    {
        saglik = maxSaglik;
        yokEdildi = false;

        CancelInvoke();
        StopAllCoroutines();

        // ✅ Asker üretimini durdur + sıfırla
        var kisla = GetComponent<BinaKisla2D>();
        if (kisla != null)
        {
            kisla.Sifirla();
        }

        onDegisti?.Invoke(this);
    }
}
