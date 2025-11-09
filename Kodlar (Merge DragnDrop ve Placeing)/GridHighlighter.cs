using UnityEngine;

public class GridHighlighter : MonoBehaviour
{
    [Tooltip("Mobilde sadece dokunma varken highlight yapsýn (önerilir).")]
    public bool requireTouchOnMobile = true;

    private Camera _cam;
    private Cell _current;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        _cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
#else
        _cam = Camera.main ?? FindObjectOfType<Camera>();
#endif
    }

    void Update()
    {
        if (_cam == null || GridManager.I == null) return;

        Vector3 world = GetPointerWorld(out bool hasPointer);

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        if (requireTouchOnMobile && !hasPointer)
        {
            ClearHighlight();
            return;
        }
#endif

        var grid = GridManager.I.WorldToGrid(world);
        if (!GridManager.I.IsInside(grid))
        {
            ClearHighlight();
            return;
        }

        var cell = GridManager.I.GetCell(grid);
        if (_current != cell)
        {
            ClearHighlight();
            _current = cell;
            if (_current != null)
                _current.SetHighlight(Cell.HighlightMode.Good); // hover => yeþil
        }
    }

    void ClearHighlight()
    {
        if (_current != null)
        {
            _current.SetHighlight(Cell.HighlightMode.None);
            _current = null;
        }
    }

    Vector3 GetPointerWorld(out bool hasPointer)
    {
        hasPointer = false;
        Vector3 s;

#if UNITY_EDITOR || UNITY_STANDALONE
        s = Input.mousePosition;
        hasPointer = true;
#else
        if (Input.touchCount > 0)
        {
            s = Input.GetTouch(0).position;
            hasPointer = true;
        }
        else
        {
            s = Input.mousePosition;
            hasPointer = Input.mousePresent;
        }
#endif
        var w = (_cam ?? Camera.main).ScreenToWorldPoint(s);
        w.z = 0f;
        return w;
    }
}
