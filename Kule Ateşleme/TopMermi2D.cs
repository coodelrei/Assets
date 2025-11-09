using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class TopMermi2D : MonoBehaviour
{
    [Header("Kimlik")]
    public Taraf2D taraf = Taraf2D.Dost;

    [Header("Hareket")]
    public float hiz = 6f;
    public float omur = 3f;

    [Header("Hasar")]
    public float hasar = 3f;

    Rigidbody2D _rb;
    float _t;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // <— düzeltildi
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic; // <— düzeltildi
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void FixedUpdate()
    {
        if (_rb != null)
        {
            Vector2 ileri = transform.up * (hiz * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + ileri);
        }
        else
        {
            transform.position += transform.up * (hiz * Time.fixedDeltaTime);
        }

        _t += Time.fixedDeltaTime;
        if (_t >= omur) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Child collider'a çarparsa parent’taki sağlık bileşenini yakala
        var sb = other.GetComponentInParent<SaglikBirim2D>();
        if (sb != null)
        {
            if (sb.taraf != taraf)
            {
                sb.HasarAl(hasar);
                Destroy(gameObject);
            }
            return;
        }

        var bb = other.GetComponentInParent<SaglikBina2D>();
        if (bb != null)
        {
            if (bb.taraf != taraf)
            {
                bb.HasarAl(hasar);
                Destroy(gameObject);
            }
            return;
        }

        // İstersen çevreye çarpınca da yok et:
        // Destroy(gameObject);
    }
}
