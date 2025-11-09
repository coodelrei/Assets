using UnityEngine;

[DisallowMultipleComponent]
public class DoldurmaCubugu2D : MonoBehaviour
{
    [Header("Kök ve Dolu Parça")]
    public Transform barRoot;
    public Transform barFill;

    [Header("Ölçü (dünya)")]
    [Min(0.1f)] public float worldWidth = 1f;
    [Min(0.05f)] public float worldHeight = 0.15f;

    float _v = 1f;
    SpriteRenderer _bgSR, _fillSR;

    public void BuildIfNeeded(BarYoneticisi2D mgr, Color bg, Color fill, string layer, int order, float z)
    {
        if (barRoot == null) barRoot = transform;

        if (_bgSR == null)
        {
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(barRoot, false);
            _bgSR = bgGo.AddComponent<SpriteRenderer>();
        }
        if (barFill == null)
        {
            var fGo = new GameObject("Fill");
            fGo.transform.SetParent(barRoot, false);
            barFill = fGo.transform;
            _fillSR = fGo.AddComponent<SpriteRenderer>();
        }
        if (_fillSR == null) _fillSR = barFill.GetComponent<SpriteRenderer>();

        _bgSR.sprite = mgr.BeyazSprite;
        _bgSR.color = bg;
        _bgSR.sortingLayerName = layer;
        _bgSR.sortingOrder = order;

        _fillSR.sprite = mgr.BeyazSprite;
        _fillSR.color = fill;
        _fillSR.sortingLayerName = layer;
        _fillSR.sortingOrder = order + 1;

        var p = transform.position;
        transform.position = new Vector3(p.x, p.y, z);

        ApplySize();
        Set01(_v);
    }

    public void SetWorldWidth(float w)
    {
        worldWidth = Mathf.Max(0.1f, w);
        ApplySize();
        Set01(_v);
    }

    void ApplySize()
    {
        if (barRoot == null) barRoot = transform;
        barRoot.localScale = new Vector3(worldWidth, worldHeight, 1f);
        if (_bgSR != null) _bgSR.transform.localScale = Vector3.one;
        if (_fillSR != null) _fillSR.transform.localScale = Vector3.one;
    }

    public void Set01(float v)
    {
        _v = Mathf.Clamp01(v);
        if (barFill == null) return;

        var s = barFill.localScale; s.x = Mathf.Max(0.0001f, _v); s.y = 1f; s.z = 1f;
        barFill.localScale = s;
        barFill.localPosition = new Vector3(-(1f - _v) * 0.5f, 0f, 0f);
    }
}
