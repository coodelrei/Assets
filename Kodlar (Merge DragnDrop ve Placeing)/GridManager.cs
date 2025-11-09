using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager I;

    [Header("Izgara Boyutu")]
    public int cols = 5;
    public int rows = 6;

    [Header("Hücre Boyutu ve Aralık")]
    public float cellSize = 1.2f;
    public float spacing = 0.05f;

    [Header("Otomatik Merkezleme")]
    public bool centerGrid = true;

    [Tooltip("Merkezleme kapalıysa sol-alt başlangıç noktası")]
    public Vector2 origin = Vector2.zero;

    [Header("Global Ofset (Tüm ızgarayı kaydır)")]
    [Tooltip("Pozitif Y yukarı taşır, negatif Y aşağı indirir. X ile sola/sağa kaydırabilirsin.")]
    public Vector2 gridOffset = Vector2.zero;

    [Header("Referanslar")]
    public Cell cellPrefab;
    public Transform cellsParent;

    private Cell[,] cells;
    private Unit[,] unitGrid;

    void Awake()
    {
        I = this;
        EnsureParentInScene();
        BuildGrid();
        FitCamera();
    }

    // === Izgara oluşturma ===
    public void BuildGrid()
    {
        EnsureParentInScene();

        // Eski hücreleri temizle
        for (int i = cellsParent.childCount - 1; i >= 0; i--)
        {
            var child = cellsParent.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }

        cells = new Cell[cols, rows];
        unitGrid = new Unit[cols, rows];

        float step = cellSize + spacing;

        // Parent'ı sıfırla ki yeniden yerleştirirken birikme olmasın
        cellsParent.localPosition = Vector3.zero;
        cellsParent.position = Vector3.zero;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = new Vector3(origin.x + x * step, origin.y + y * step, 0f);
                var c = Instantiate(cellPrefab, pos, Quaternion.identity, cellsParent);
                c.name = $"Cell_{x}_{y}";
                c.SetVisual(new Vector2Int(x, y), cellSize);
                cells[x, y] = c;
            }
        }

        if (centerGrid)
        {
            float width = (cols - 1) * step + cellSize;
            float height = (rows - 1) * step + cellSize;

            Vector3 gridCenter = new Vector3(
                origin.x + width * 0.5f - cellSize * 0.5f,
                origin.y + height * 0.5f - cellSize * 0.5f,
                0f);

            // Sıfıra göre merkezle
            cellsParent.position -= gridCenter;
        }

        // Son olarak global ofset uygula (pozitif Y yukarı, negatif Y aşağı)
        if (gridOffset != Vector2.zero)
            cellsParent.position += (Vector3)gridOffset;
    }

    // === Yardımcılar ===
    public Vector3 GridToWorld(Vector2Int g)
    {
        float step = cellSize + spacing;
        return cellsParent.position + new Vector3(
            origin.x + g.x * step,
            origin.y + g.y * step,
            0f);
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3 local = world - cellsParent.position;
        float step = cellSize + spacing;

        int x = Mathf.RoundToInt((local.x - origin.x) / step);
        int y = Mathf.RoundToInt((local.y - origin.y) / step);

        x = Mathf.Clamp(x, 0, cols - 1);
        y = Mathf.Clamp(y, 0, rows - 1);
        return new Vector2Int(x, y);
    }

    // clamp yok (drag sırasında dışarı çıkabilir)
    public Vector2Int WorldToGridRaw(Vector3 world)
    {
        Vector3 local = world - cellsParent.position;
        float step = cellSize + spacing;

        int x = Mathf.RoundToInt((local.x - origin.x) / step);
        int y = Mathf.RoundToInt((local.y - origin.y) / step);

        return new Vector2Int(x, y);
    }

    public bool IsInside(Vector2Int g)
    {
        return g.x >= 0 && g.y >= 0 && g.x < cols && g.y < rows;
    }

    public Cell GetCell(Vector2Int g)
    {
        if (!IsInside(g)) return null;
        return cells[g.x, g.y];
    }

    public Unit GetUnit(Vector2Int g)
    {
        if (!IsInside(g)) return null;
        return unitGrid[g.x, g.y];
    }

    public bool IsCellEmpty(Vector2Int g)
    {
        if (!IsInside(g)) return false;
        return unitGrid[g.x, g.y] == null;
    }

    // === Placement ===
    public bool CanPlace(Unit unit, Vector2Int baseGrid)
    {
        foreach (var off in unit.ShapeOffsets)
        {
            var g = baseGrid + off;
            if (!IsInside(g)) return false;
            if (unitGrid[g.x, g.y] != null) return false;
        }
        return true;
    }

    public void PlaceUnit(Unit unit, Vector2Int baseGrid)
    {
        foreach (var off in unit.ShapeOffsets)
        {
            var g = baseGrid + off;
            if (IsInside(g)) unitGrid[g.x, g.y] = unit;
        }

        unit.gridPos = baseGrid;
        unit.transform.position = GridToWorld(baseGrid);
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit == null) return;
        foreach (var off in unit.ShapeOffsets)
        {
            var g = unit.gridPos + off;
            if (IsInside(g) && unitGrid[g.x, g.y] == unit)
                unitGrid[g.x, g.y] = null;
        }
    }

    // === Merge (hücreye birebir hizalı) ===
    public bool TryExactMergePreview(Unit drop, Vector2Int baseGrid, Unit newTarget)
    {
        if (drop == null || newTarget == null) return false;

        foreach (var off in drop.ShapeOffsets)
        {
            var g = baseGrid + off;
            if (!IsInside(g)) return false;

            var u = unitGrid[g.x, g.y];
            if (u == null) return false;
            if (u != newTarget) return false;
        }

        if (!HasSameCellSet(drop, baseGrid, newTarget, newTarget.gridPos))
            return false;

        if (drop.mergeId != newTarget.mergeId) return false;
        if (drop.level != newTarget.level) return false;

        return true;
    }

    public bool TryExactMerge(Unit drop, Vector2Int baseGrid)
    {
        if (drop.ShapeOffsets == null || drop.ShapeOffsets.Length == 0) return false;

        Unit candidate = null;
        foreach (var off in drop.ShapeOffsets)
        {
            var g = baseGrid + off;
            if (!IsInside(g)) return false;

            var u = unitGrid[g.x, g.y];
            if (u == null) return false;
            if (u == drop) return false;
            if (candidate == null) candidate = u;
            else if (u != candidate) return false;
        }

        if (candidate == null) return false;

        if (!HasSameCellSet(drop, baseGrid, candidate, candidate.gridPos)) return false;
        if (drop.mergeId != candidate.mergeId) return false;
        if (drop.level != candidate.level) return false;

        MergeUnits(drop, candidate);
        return true;
    }

    bool HasSameCellSet(Unit a, Vector2Int aBase, Unit b, Vector2Int bBase)
    {
        var hash = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (var off in a.ShapeOffsets) hash.Add(aBase + off);

        foreach (var off in b.ShapeOffsets)
        {
            var g = bBase + off;
            if (!hash.Remove(g)) return false;
        }
        return hash.Count == 0;
    }

    public void MergeUnits(Unit drop, Unit target)
    {
        if (target == null || drop == null) return;
        if (target.nextLevelPrefab == null)
        {
            PlaceUnit(target, target.gridPos);
            return;
        }

        Vector2Int spawnBase = target.gridPos;

        RemoveUnit(target);
        RemoveUnit(drop);

        if (Application.isPlaying)
        {
            Destroy(target.gameObject);
            Destroy(drop.gameObject);
        }
        else
        {
            DestroyImmediate(target.gameObject);
            DestroyImmediate(drop.gameObject);
        }

        var go = Instantiate(target.nextLevelPrefab, GridToWorld(spawnBase), Quaternion.identity, transform.parent);
        var u = go.GetComponent<Unit>();
        if (u != null)
        {
            u.level = target.level + 1;
            PlaceUnit(u, spawnBase);
            u.RebuildVisualToGrid();
        }
    }

    // === Kamera ===
    void FitCamera()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float step = cellSize + spacing;
        float width = (cols - 1) * step + cellSize;
        float height = (rows - 1) * step + cellSize;

        cam.transform.position = new Vector3(0f, 0f, -10f);

        float halfHeight = height * 0.55f;
        float halfWidth = (width * cam.pixelHeight / cam.pixelWidth) * 0.55f;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth);
    }

    // === Cells Parent güvenliği ===
    void EnsureParentInScene()
    {
        if (cellsParent == null || !cellsParent.gameObject.scene.IsValid())
        {
            var existing = transform.Find("Cells");
            if (existing != null && existing.gameObject.scene.IsValid())
            {
                cellsParent = existing;
            }
            else
            {
                var p = new GameObject("Cells").transform;
                p.SetParent(transform);
                p.localPosition = Vector3.zero;
                cellsParent = p;
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate() { }
#endif
}
