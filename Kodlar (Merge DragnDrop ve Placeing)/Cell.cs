using UnityEngine;

public class Cell : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPos;

    [Header("Görsel")]
    public SpriteRenderer sr;
    public Color baseColor = new Color(1f, 1f, 1f, 0.20f);
    public Color altColor = new Color(1f, 1f, 1f, 0.10f);

    [Header("Highlight Renkleri")]
    public Color highlightGoodColor = new Color(0.20f, 1f, 0.20f, 0.60f); // yeþil
    public Color highlightBadColor = new Color(1f, 0.20f, 0.20f, 0.60f); // kýrmýzý

    public enum HighlightMode { None, Good, Bad }

    private Color _normalColor;
    private HighlightMode _mode = HighlightMode.None;

    void Reset()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    public void SetVisual(Vector2Int pos, float size)
    {
        gridPos = pos;
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        transform.localScale = Vector3.one * size;

        _normalColor = ((pos.x + pos.y) % 2 == 0) ? baseColor : altColor;
        sr.color = _normalColor;
        _mode = HighlightMode.None;
    }

    // Yeni: Mod tabanlý highlight
    public void SetHighlight(HighlightMode mode)
    {
        if (sr == null) return;
        if (_mode == mode) return;
        _mode = mode;

        switch (mode)
        {
            default:
            case HighlightMode.None:
                sr.color = _normalColor;
                break;
            case HighlightMode.Good:
                sr.color = highlightGoodColor;
                break;
            case HighlightMode.Bad:
                sr.color = highlightBadColor;
                break;
        }
    }

    // Eski API ile uyum için (Good varsayýlsýn)
    public void SetHighlighted(bool on)
    {
        SetHighlight(on ? HighlightMode.Good : HighlightMode.None);
    }
}
