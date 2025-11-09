using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SimpleShopGrid : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public Sprite icon;
        public GameObject prefab;
        public int price = 10;
    }

    [Header("UI")]
    public RectTransform contentRoot;     // Panel içindeki alan
    public SimpleShopCell cellPrefab;     // Hücre prefab (UI)

    [Header("Grid Ayarlarý")]
    public int columns = 2;
    public Vector2 cellSize = new Vector2(200, 200);
    public Vector2 spacing = new Vector2(24, 24);

    [Header("Davranýþ")]
    public bool populateOnEnable = true;

    [Header("Ýçerik (Elle doldur)")]
    public List<Entry> items = new List<Entry>();

    void OnEnable()
    {
        if (populateOnEnable) Generate();
    }

    public void Generate()
    {
        if (!contentRoot || !cellPrefab)
        {
            Debug.LogError("[SimpleShopGrid] contentRoot veya cellPrefab eksik.");
            return;
        }

        EnsureGrid();

        // Eski çocuklarý sil
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        // Hücreleri oluþtur
        int created = 0;
        foreach (var e in items)
        {
            if (e == null || e.prefab == null) continue;
            var cell = Instantiate(cellPrefab, contentRoot);
            cell.Setup(e.icon, e.price, e.prefab);
            created++;
        }

        Debug.Log($"[SimpleShopGrid] {created} hücre oluþturuldu.");
    }

    void EnsureGrid()
    {
        // Vertical varsa sök
        var v = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (v) DestroyImmediate(v);

        // Grid
        var grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (!grid) grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);

        // Fitter
        var fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Anchor/pivot (üstten aç)
        contentRoot.anchorMin = new Vector2(0, 1);
        contentRoot.anchorMax = new Vector2(1, 1);
        contentRoot.pivot = new Vector2(0.5f, 1);
        contentRoot.anchoredPosition = Vector2.zero;
    }
}
