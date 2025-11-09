using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopPanelQuickFix : MonoBehaviour
{
    public RectTransform contentRoot;
    public int columns = 2;
    public Vector2 cellSize = new Vector2(200, 200);
    public Vector2 spacing = new Vector2(24, 24);

    void Awake()
    {
        if (!contentRoot) return;

        var v = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (v) DestroyImmediate(v);

        var grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (!grid) grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);

        var fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        contentRoot.anchorMin = new Vector2(0, 1);
        contentRoot.anchorMax = new Vector2(1, 1);
        contentRoot.pivot = new Vector2(0.5f, 1);
        contentRoot.anchoredPosition = Vector2.zero;
    }
}
