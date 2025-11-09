using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SimpleShopCell : MonoBehaviour,
    IPointerDownHandler, IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    public Image iconImage;
    public TextMeshProUGUI priceText;

    [Header("Snap Ayarları")]
    public LayerMask snapMask = ~0;      // Hücre collider layer'ı (örn: Cells)
    public float snapRadius = 0.8f;      // Hücre arama yarıçapı
    public string requiredTag = "";      // İstersen "Cell" yaz; boşsa tag şartı yok
    public float worldZ = 0f;

    [Header("Fallback (snap yoksa)")]
    public Vector2 gridCellSize = new Vector2(1.2f, 1.2f);

    GameObject _prefab; int _price;

    // Drag edilen gerçek bina
    GameObject _dragObj;
    Collider2D[] _dragCols;
    Transform _currentSnapCell;
    Vector3 _lastValidPos;

    // ScrollRect çakışması
    ScrollRect _scrollRect;
    bool _dragging;

    public void Setup(Sprite icon, int price, GameObject prefab)
    {
        _prefab = prefab; _price = price;
        EnsureLayout();

        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = true;
            var leI = iconImage.GetComponent<LayoutElement>() ?? iconImage.gameObject.AddComponent<LayoutElement>();
            leI.preferredHeight = 100f;
        }
        if (priceText)
        {
            priceText.text = price.ToString();
            priceText.color = Color.black;
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = 24; priceText.fontSizeMax = 64;
            priceText.raycastTarget = true;
            var leT = priceText.GetComponent<LayoutElement>() ?? priceText.gameObject.AddComponent<LayoutElement>();
            leT.preferredHeight = 40f; leT.flexibleWidth = 1f;
        }
    }

    void EnsureLayout()
    {
        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        if (bg.color.a <= 0.0001f) bg.color = new Color(1, 1, 1, 0.001f);
        bg.raycastTarget = true;

        var v = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(12, 12, 12, 12);
        v.spacing = 6f;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        if (le.preferredWidth <= 0) le.preferredWidth = 200;
        if (le.preferredHeight <= 0) le.preferredHeight = 200;
    }

    // --- ScrollRect’i ez ---
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_scrollRect == null) _scrollRect = GetComponentInParent<ScrollRect>();
        eventData.pointerPress = gameObject;
        eventData.pointerDrag = gameObject;
        eventData.useDragThreshold = false;
    }
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (_scrollRect == null) _scrollRect = GetComponentInParent<ScrollRect>();
        if (_scrollRect) _scrollRect.OnInitializePotentialDrag(eventData);
        eventData.pointerDrag = gameObject;
        eventData.useDragThreshold = false;
    }

    // =============== DRAG ===============
    public void OnBeginDrag(PointerEventData e)
    {
        if (_prefab == null) return;

        if (_scrollRect == null) _scrollRect = GetComponentInParent<ScrollRect>();
        if (_scrollRect) _scrollRect.enabled = false;
        _dragging = true;

        var cam = Camera.main; if (!cam) return;

        _dragObj = Instantiate(_prefab);
        _dragCols = _dragObj.GetComponentsInChildren<Collider2D>(true);
        if (_dragCols != null) for (int i = 0; i < _dragCols.Length; i++) _dragCols[i].enabled = false;

        var kisla = _dragObj.GetComponent<BinaKisla2D>();
        if (kisla) { kisla.autoStart = false; kisla.enabled = false; }
        var sag = _dragObj.GetComponent<SaglikBina2D>();
        if (sag) { sag.enabled = false; }

        Vector3 wp = cam.ScreenToWorldPoint(e.position);
        if (TryGetSnap(wp, out var pos, out var cell))
        {
            _dragObj.transform.position = pos;
            _currentSnapCell = cell;
            _lastValidPos = pos;
        }
        else
        {
            _dragObj.transform.position = FallbackSnap(wp);
            _lastValidPos = _dragObj.transform.position;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging || _dragObj == null) return;
        var cam = Camera.main; if (!cam) return;

        Vector3 wp = cam.ScreenToWorldPoint(e.position);
        if (TryGetSnap(wp, out var pos, out var cell))
        {
            _dragObj.transform.position = pos;
            _currentSnapCell = cell;
            _lastValidPos = pos;
        }
        else
        {
            _dragObj.transform.position = _lastValidPos;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging) return;
        _dragging = false;

        if (_scrollRect) _scrollRect.enabled = true;
        if (_dragObj == null) return;

        var cam = Camera.main; if (!cam) { Destroy(_dragObj); return; }

        // 1) SON SNAP NOKTASINI KESİNLE
        _dragObj.transform.position = _lastValidPos;

        // 2) ALTIN YOKSA iptal (drag’i bozmaz)
        if (!AltinYeterliMi(_price))
        {
            Destroy(_dragObj);
            return;
        }

        // 3) COLLIDER’LARI AÇ (yer kaplasın)
        if (_dragCols != null) for (int i = 0; i < _dragCols.Length; i++) _dragCols[i].enabled = true;

        // 4) SADECE TryPlaceExisting → alıcı varsa “objeyi” kabul etsin (konumu değiştirmesin)
        bool acceptedByReceiver = TryPlaceExistingOnly(e, cam, _dragObj);

        // 5) UYKU + KAYIT + ALTIN
        MakeDormant(_dragObj);
        DalgaYonetimi2D.RegisterDormant(_dragObj);
        HarcaAltin(_price);

        _dragObj = null;
    }

    // =============== Snap yardımcıları ===============
    bool TryGetSnap(Vector3 worldPoint, out Vector3 snappedPos, out Transform cell)
    {
        worldPoint.z = worldZ;
        snappedPos = Vector3.zero; cell = null;

        var hits = Physics2D.OverlapCircleAll((Vector2)worldPoint, snapRadius, snapMask);
        float best = float.MaxValue;
        Transform bestCell = null;
        Vector3 bestPos = Vector3.zero;

        for (int i = 0; i < hits.Length; i++)
        {
            var tr = hits[i].transform;
            if (!string.IsNullOrEmpty(requiredTag) && !tr.CompareTag(requiredTag))
                continue;

            Vector3 cp = tr.position; cp.z = worldZ;
            float d = (cp - worldPoint).sqrMagnitude;
            if (d < best) { best = d; bestCell = tr; bestPos = cp; }
        }

        if (bestCell != null)
        {
            snappedPos = bestPos;
            cell = bestCell;
            return true;
        }

        snappedPos = FallbackSnap(worldPoint);
        return false;
    }

    Vector3 FallbackSnap(Vector3 wp)
    {
        wp.z = worldZ;
        float sx = Mathf.Max(0.01f, gridCellSize.x);
        float sy = Mathf.Max(0.01f, gridCellSize.y);
        wp.x = Mathf.Round(wp.x / sx) * sx;
        wp.y = Mathf.Round(wp.y / sy) * sy;
        return wp;
    }

    // =============== Yerleştirme & Uyutma ===============
    void MakeDormant(GameObject go)
    {
        var sag = go.GetComponent<SaglikBina2D>();
        if (sag) sag.enabled = false;

        var kisla = go.GetComponent<BinaKisla2D>();
        if (kisla) { kisla.autoStart = false; kisla.enabled = false; }
    }

    // >>> SADECE TryPlaceExisting çağrılır (pozisyonu biz tutuyoruz)
    bool TryPlaceExistingOnly(PointerEventData e, Camera cam, GameObject currentObj)
    {
        Vector3 wp3 = cam.ScreenToWorldPoint(e.position);
        Vector2 wp = new Vector2(wp3.x, wp3.y);

        var hits = Physics2D.OverlapPointAll(wp);
        if (hits == null || hits.Length == 0) return false;

        for (int i = 0; i < hits.Length; i++)
        {
            var go = hits[i].gameObject;
            var comps = go.GetComponents<MonoBehaviour>();
            for (int c = 0; c < comps.Length; c++)
            {
                var comp = comps[c]; if (comp == null) continue;
                var t = comp.GetType();

                var me = t.GetMethod("TryPlaceExisting", new System.Type[] { typeof(GameObject), typeof(int) });
                if (me != null)
                {
                    var ok = me.Invoke(comp, new object[] { currentObj, _price });
                    if (ok is bool b && b) return true;
                }
            }
        }
        return false;
    }

    // =============== Altın yardımcıları ===============
    bool AltinYeterliMi(int miktar)
    {
        var gm = GoldManager.Instance;
        if (gm == null) return true;
        var m = gm.GetType().GetMethod("AltinYeterliMi");
        if (m != null && m.ReturnType == typeof(bool))
            return (bool)m.Invoke(gm, new object[] { miktar });
        return true;
    }

    void HarcaAltin(int miktar)
    {
        var gm = GoldManager.Instance; if (gm == null) return;
        var spend = gm.GetType().GetMethod("SpendGold");
        if (spend != null) { spend.Invoke(gm, new object[] { miktar }); return; }
        var add = gm.GetType().GetMethod("AddGold");
        if (add != null) add.Invoke(gm, new object[] { -miktar });
    }
}
