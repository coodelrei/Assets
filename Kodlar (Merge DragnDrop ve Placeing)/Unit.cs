using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Unit : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPos;

    [Header("Çok Hücreli Şekil (ofsetler sol-alt köşeye göre)")]
    public Vector2Int[] ShapeOffsets = new[] { Vector2Int.zero };

    [Header("Merge Bilgisi")]
    public string mergeId = "t1";
    public int level = 1;
    public GameObject nextLevelPrefab;

    [Header("Shape Authoring (Checkbox Panel)")]
    public bool useGridAuthoring = false;
    [Min(1)] public int authorCols = 5;
    [Min(1)] public int authorRows = 5;
    [SerializeField, HideInInspector] private List<bool> authorGrid = new();

    [Header("Görsel Kaydırma (hücre bazlı)")]
    public Vector2 visualNudgeCells = Vector2.zero;

    [Header("Sprite / Collider Otomatik Kurulum")]
    public bool autoFitSprite = true;
    public SpriteRenderer spriteToScale;

    [Header("Polygon Collider Ayarı")]
    public float colliderPad = 0.0f; // tam hücreye otursun

    [Header("Smooth Snap")]
    [Range(0.05f, 1f)] public float snapDuration = 0.15f;
    public EaseType easeType = EaseType.SmoothStep;
    public enum EaseType { Linear, EaseIn, EaseOut, SmoothStep, EaseInOut, Bounce }

    const float UnitZ = -1f;

    // runtime
    private bool isDragging;
    private Vector3 dragOffset;
    private Camera cam;
    private Vector3 originalPos;
    private Vector2Int originalGrid;
    private Coroutine moveRoutine;
    private Collider2D col;
    private readonly List<Cell> highlighted = new();

    // Fx
    private UnitFx _fx;
    private Unit _pulseTargetUnit;
    private UnitFx _pulseTargetFx;
    private readonly List<UnitFx> _globalPulseFx = new();

    void Awake()
    {
        cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        var p = transform.position; p.z = UnitZ; transform.position = p;

        _fx = GetComponent<UnitFx>();

        RebuildVisualToGrid();
    }

    void Start()
    {
        if (GridManager.I != null) RebuildVisualToGrid();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        authorCols = Mathf.Max(1, authorCols);
        authorRows = Mathf.Max(1, authorRows);

        int need = authorCols * authorRows;
        if (authorGrid == null) authorGrid = new List<bool>(need);
        if (authorGrid.Count != need)
        {
            var old = new List<bool>(authorGrid);
            authorGrid.Clear();
            for (int i = 0; i < need; i++)
                authorGrid.Add(i < old.Count ? old[i] : false);
        }

        if (useGridAuthoring) BakeShapeOffsetsFromAuthorGrid();
        if (!Application.isPlaying) RebuildVisualToGrid();
    }
#endif

    public void BakeShapeOffsetsFromAuthorGrid()
    {
        int minX = int.MaxValue, minY = int.MaxValue; bool any = false;

        for (int y = 0; y < authorRows; y++)
            for (int x = 0; x < authorCols; x++)
            {
                int idx = y * authorCols + x;
                if (idx >= 0 && idx < authorGrid.Count && authorGrid[idx])
                {
                    any = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                }
            }

        var list = new List<Vector2Int>();
        if (!any) list.Add(Vector2Int.zero);
        else
        {
            for (int y = 0; y < authorRows; y++)
                for (int x = 0; x < authorCols; x++)
                {
                    int idx = y * authorCols + x;
                    if (idx >= 0 && idx < authorGrid.Count && authorGrid[idx])
                        list.Add(new Vector2Int(x - minX, y - minY));
                }
        }
        ShapeOffsets = list.ToArray();
        RebuildVisualToGrid();
    }

#if UNITY_EDITOR
    public List<bool> GetAuthorGrid() => authorGrid;
    public void SetAuthorCell(int x, int y, bool v)
    {
        int idx = y * authorCols + x;
        if (idx >= 0 && idx < authorGrid.Count) authorGrid[idx] = v;
    }
#endif

    void Update()
    {
        if (cam == null || GridManager.I == null) return;

        Vector3 world = GetPointerWorld();
        bool down = PointerDown();
        bool held = PointerHeld();
        bool up = PointerUp();

        if (!isDragging && down && IsPointerOverThis(world))
            StartDrag(world);

        if (isDragging && held)
        {
            transform.position = world + dragOffset;
            UpdateHighlightUnder(transform.position);
        }

        if (isDragging && up)
            EndDrag();
    }

    void StartDrag(Vector3 world)
    {
        isDragging = true;
        originalPos = transform.position;
        originalGrid = gridPos;
        GridManager.I.RemoveUnit(this);
        dragOffset = transform.position - world;
        UpdateHighlightUnder(transform.position);

        // sahnedeki tüm adayları pulse
        StartGlobalPulseForCandidates();
    }

    void EndDrag()
    {
        isDragging = false;

        var baseGrid = GridManager.I.WorldToGridRaw(transform.position - NudgeWorld());

        if (GridManager.I.CanPlace(this, baseGrid))
        {
            GridManager.I.PlaceUnit(this, baseGrid);
            PlaySnap(GridManager.I.GridToWorld(baseGrid) + NudgeWorld());
            if (_fx != null) _fx.SetState(UnitFx.FxState.MergeSuccess);
            ClearHighlight();
            return;
        }

        if (GridManager.I.TryExactMerge(this, baseGrid))
        {
            ClearHighlight();
            return;
        }

        GridManager.I.PlaceUnit(this, originalGrid);
        PlaySnap(GridManager.I.GridToWorld(originalGrid));
        if (_fx != null) _fx.SetState(UnitFx.FxState.MergeFail);
        ClearHighlight();
    }

    // === Highlight & hedef/pulse ===
    void UpdateHighlightUnder(Vector3 worldPos)
    {
        ClearHighlightCells();

        var baseGrid = GridManager.I.WorldToGridRaw(worldPos - NudgeWorld());
        bool canPlace = GridManager.I.CanPlace(this, baseGrid);

        foreach (var off in ShapeOffsets)
        {
            var g = baseGrid + off;
            if (!GridManager.I.IsInside(g)) continue;

            var cell = GridManager.I.GetCell(g);
            if (cell != null)
            {
                cell.SetHighlight(canPlace ? Cell.HighlightMode.Good : Cell.HighlightMode.Bad);
                highlighted.Add(cell);
            }
        }

        if (_fx != null)
            _fx.SetState(canPlace ? UnitFx.FxState.Good : UnitFx.FxState.Bad);

        // tam üstüne gelinen hedef için ek pulse
        Unit newTarget = GridManager.I.GetUnit(baseGrid);
        bool validMergeTarget = false;
        if (newTarget != null && newTarget != this)
        {
            validMergeTarget = (newTarget.mergeId == mergeId && newTarget.level == level &&
                                GridManager.I.TryExactMergePreview(this, baseGrid, newTarget));
        }

        if (validMergeTarget)
        {
            if (_pulseTargetUnit != newTarget)
            {
                if (_pulseTargetFx != null) _pulseTargetFx.SetState(UnitFx.FxState.None);
                _pulseTargetUnit = newTarget;
                _pulseTargetFx = _pulseTargetUnit.GetComponent<UnitFx>();
                if (_pulseTargetFx != null)
                    _pulseTargetFx.SetState(UnitFx.FxState.MergeTarget);
            }
        }
        else
        {
            if (_pulseTargetFx != null) _pulseTargetFx.SetState(UnitFx.FxState.None);
            _pulseTargetUnit = null;
            _pulseTargetFx = null;
        }
    }

    void ClearHighlight()
    {
        ClearHighlightCells();
        if (_fx != null) _fx.SetState(UnitFx.FxState.None);
        if (_pulseTargetFx != null) _pulseTargetFx.SetState(UnitFx.FxState.None);
        _pulseTargetUnit = null;
        _pulseTargetFx = null;
        StopGlobalPulseForCandidates();
    }

    void ClearHighlightCells()
    {
        for (int i = 0; i < highlighted.Count; i++)
            if (highlighted[i] != null) highlighted[i].SetHighlight(Cell.HighlightMode.None);
        highlighted.Clear();
    }

    // === Pulse: sahnedeki tüm aynı tip level
    void StartGlobalPulseForCandidates()
    {
        _globalPulseFx.Clear();
#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var all = Object.FindObjectsOfType<Unit>();
#endif
        for (int i = 0; i < all.Length; i++)
        {
            var u = all[i];
            if (u == null || u == this) continue;
            if (u.mergeId != mergeId || u.level != level) continue;

            var fx = u.GetComponent<UnitFx>();
            if (fx != null)
            {
                fx.SetState(UnitFx.FxState.MergeTarget);
                _globalPulseFx.Add(fx);
            }
        }
    }
    void StopGlobalPulseForCandidates()
    {
        for (int i = 0; i < _globalPulseFx.Count; i++)
            if (_globalPulseFx[i] != null) _globalPulseFx[i].SetState(UnitFx.FxState.None);
        _globalPulseFx.Clear();
    }

    // === Snap anim
    void PlaySnap(Vector3 target)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(SmoothMove(target, snapDuration));
    }
    IEnumerator SmoothMove(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = Ease(t);
            transform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }
        transform.position = target;
    }
    float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        switch (easeType)
        {
            case EaseType.Linear: return t;
            case EaseType.EaseIn: return t * t;
            case EaseType.EaseOut: return t * (2 - t);
            case EaseType.SmoothStep: return Mathf.SmoothStep(0, 1, t);
            case EaseType.EaseInOut: return t * t * (3 - 2 * t);
            case EaseType.Bounce: return BounceEaseOut(t);
            default: return t;
        }
    }
    float BounceEaseOut(float t)
    {
        if (t < (1 / 2.75f)) return 7.5625f * t * t;
        else if (t < (2 / 2.75f)) { t -= (1.5f / 2.75f); return 7.5625f * t * t + 0.75f; }
        else if (t < (2.5f / 2.75f)) { t -= (2.25f / 2.75f); return 7.5625f * t * t + 0.9375f; }
        else { t -= (2.625f / 2.75f); return 7.5625f * t * t + 0.984375f; }
    }

    // === Input yardımcıları
    Vector3 GetPointerWorld()
    {
        Vector3 s;
#if UNITY_EDITOR || UNITY_STANDALONE
        s = Input.mousePosition;
#else
        s = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
#endif
        var w = (cam ?? Camera.main).ScreenToWorldPoint(s);
        w.z = 0f;
        return w;
    }
    bool PointerDown()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonDown(0);
#else
        return (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0);
#endif
    }
    bool PointerHeld()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
#else
        return (Input.touchCount > 0 &&
                (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary))
               || Input.GetMouseButton(0);
#endif
    }
    bool PointerUp()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonUp(0);
#else
        return (Input.touchCount > 0 &&
                (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled))
               || Input.GetMouseButtonUp(0);
#endif
    }
    bool IsPointerOverThis(Vector3 worldPoint)
    {
        var hits = Physics2D.OverlapPointAll(worldPoint);
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == transform || t.IsChildOf(transform))
                return true;
        }
        return false;
    }

    // ========= SPRITE + POLYGON =========
    [ContextMenu("Görseli Grid’e Göre Yeniden Kur")]
    public void RebuildVisualToGrid()
    {
        if (GridManager.I == null || ShapeOffsets == null || ShapeOffsets.Length == 0) return;

        RemoveCompositeIfAny();

        if (autoFitSprite) FitSpriteToShape();
        BuildPolygonFromShapeOutline();
        RefreshColliderCache();

        var fx = _fx ?? GetComponent<UnitFx>();
        if (fx != null) fx.MarkVisualChanged();
    }

    void FitSpriteToShape()
    {
        if (spriteToScale == null)
            spriteToScale = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteToScale == null || spriteToScale.sprite == null) return;

        GetShapeWH(out int wCells, out int hCells);

        float step = GridManager.I.cellSize + GridManager.I.spacing;

        float desiredW = (wCells - 1) * step + GridManager.I.cellSize;
        float desiredH = (hCells - 1) * step + GridManager.I.cellSize;

        Vector3 centerOff = new Vector3((wCells - 1) * step * 0.5f, (hCells - 1) * step * 0.5f, 0f);
        spriteToScale.transform.localPosition = centerOff;

        Vector2 sprLocal = GetSpriteLocalSize(spriteToScale);
        if (sprLocal.x <= 1e-4f || sprLocal.y <= 1e-4f) return;

        Vector3 s = spriteToScale.transform.localScale;
        s.x = desiredW / sprLocal.x;
        s.y = desiredH / sprLocal.y;
        spriteToScale.transform.localScale = s;
    }

    void BuildPolygonFromShapeOutline()
    {
        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null) poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;

        float step = GridManager.I.cellSize + GridManager.I.spacing;
        float half = (step * 0.5f);

        GetShapeWH(out int wCells, out int hCells);
        Vector2 centerOff = new Vector2((wCells - 1) * step * 0.5f, (hCells - 1) * step * 0.5f);

        var occ = new HashSet<Vector2Int>(ShapeOffsets);
        var segs = new HashSet<(Vector2 a, Vector2 b)>(new EdgeComparer());

        foreach (var o in occ)
        {
            float cx = o.x * step;
            float cy = o.y * step;

            float left = cx - half;
            float right = cx + half;
            float bottom = cy - half;
            float top = cy + half;

            if (!occ.Contains(new Vector2Int(o.x - 1, o.y)))
                segs.Add((new Vector2(left, bottom), new Vector2(left, top)));
            if (!occ.Contains(new Vector2Int(o.x + 1, o.y)))
                segs.Add((new Vector2(right, bottom), new Vector2(right, top)));
            if (!occ.Contains(new Vector2Int(o.x, o.y - 1)))
                segs.Add((new Vector2(left, bottom), new Vector2(right, bottom)));
            if (!occ.Contains(new Vector2Int(o.x, o.y + 1)))
                segs.Add((new Vector2(left, top), new Vector2(right, top)));
        }

        var loops = BuildLoops(segs);

        poly.pathCount = loops.Count;
        for (int i = 0; i < loops.Count; i++)
        {
            var loop = loops[i];
            for (int k = 0; k < loop.Count; k++)
                loop[k] -= centerOff;
            poly.SetPath(i, loop.ToArray());
        }
        poly.offset = centerOff;
    }

    struct EdgeComparer : IEqualityComparer<(Vector2, Vector2)>
    {
        public bool Equals((Vector2, Vector2) x, (Vector2, Vector2) y)
        {
            return (Approximately(x.Item1, y.Item1) && Approximately(x.Item2, y.Item2)) ||
                   (Approximately(x.Item1, y.Item2) && Approximately(x.Item2, y.Item1));
        }
        public int GetHashCode((Vector2, Vector2) obj)
        {
            var v = obj.Item1 + obj.Item2;
            return v.GetHashCode();
        }
    }
    class VecComparer : IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 x, Vector2 y) => Approximately(x, y);
        public int GetHashCode(Vector2 obj) => obj.GetHashCode();
    }
    static bool Approximately(Vector2 a, Vector2 b)
        => Mathf.Abs(a.x - b.x) < 1e-4f && Mathf.Abs(a.y - b.y) < 1e-4f;

    static List<List<Vector2>> BuildLoops(HashSet<(Vector2 a, Vector2 b)> segs)
    {
        var adj = new Dictionary<Vector2, List<Vector2>>(new VecComparer());

        foreach (var e in segs)
        {
            if (!adj.TryGetValue(e.a, out var la)) adj[e.a] = la = new List<Vector2>();
            if (!adj.TryGetValue(e.b, out var lb)) adj[e.b] = lb = new List<Vector2>();
            la.Add(e.b); lb.Add(e.a);
        }

        var loops = new List<List<Vector2>>();

        while (adj.Count > 0)
        {
            var en = adj.GetEnumerator(); en.MoveNext();
            Vector2 start = en.Current.Key;

            var loop = new List<Vector2>();
            Vector2 curr = start;
            Vector2 prev = start + Vector2.left;

            int guard = 0;
            while (true)
            {
                loop.Add(curr);
                var neigh = adj[curr];
                if (neigh.Count == 0) break;

                Vector2 next = neigh[0];
                if (Approximately(next, prev) && neigh.Count > 1) next = neigh[1];

                RemoveEdge(adj, curr, next);

                prev = curr; curr = next;
                if (Approximately(curr, start)) break;
                if (++guard > 4096) break;
            }

            if (loop.Count >= 3) loops.Add(loop);
        }
        return loops;
    }

    static void RemoveEdge(Dictionary<Vector2, List<Vector2>> adj, Vector2 a, Vector2 b)
    {
        if (adj.TryGetValue(a, out var la))
        {
            la.RemoveAll(v => Approximately(v, b));
            if (la.Count == 0) adj.Remove(a);
        }
        if (adj.TryGetValue(b, out var lb))
        {
            lb.RemoveAll(v => Approximately(v, a));
            if (lb.Count == 0) adj.Remove(b);
        }
    }

    void RemoveCompositeIfAny()
    {
        var comp = GetComponent<CompositeCollider2D>();
        if (comp != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(comp);
            else Destroy(comp);
#else
            Destroy(comp);
#endif
        }
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType == RigidbodyType2D.Static)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(rb);
            else Destroy(rb);
#else
            Destroy(rb);
#endif
        }
        var holder = transform.Find("__ColCells");
        if (holder != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(holder.gameObject);
            else Destroy(holder.gameObject);
#else
            Destroy(holder.gameObject);
#endif
        }
    }

    void RefreshColliderCache() => col = GetComponent<Collider2D>();

    Vector3 NudgeWorld()
    {
        if (GridManager.I == null) return Vector3.zero;
        float step = GridManager.I.cellSize + GridManager.I.spacing;
        return new Vector3(visualNudgeCells.x * step, visualNudgeCells.y * step, 0f);
    }

    void GetShapeWH(out int w, out int h)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        if (ShapeOffsets == null || ShapeOffsets.Length == 0) { w = h = 1; return; }
        foreach (var o in ShapeOffsets)
        {
            if (o.x < minX) minX = o.x;
            if (o.y < minY) minY = o.y;
            if (o.x > maxX) maxX = o.x;
            if (o.y > maxY) maxY = o.y;
        }
        w = (maxX - minX + 1);
        h = (maxY - minY + 1);
    }

    static Vector2 GetSpriteLocalSize(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return Vector2.one;
        var sp = sr.sprite;
        return sp.rect.size / sp.pixelsPerUnit;
    }
}
