using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PanelAlignBottom : MonoBehaviour
{
    public RectTransform target;     // boþ býrakýlýrsa kendi RectTransform'u
    [Header("Yerleþim")]
    public float height = 360f;      // panel yüksekliði
    public float bottomMargin = 40f; // alttan boþluk

    [Header("Seçenekler")]
    public bool forceScaleOne = true;        // scale'i 1,1,1 yap
    public bool applyOnAwake = true;         // Awake'te uygula
    public bool liveInEditMode = true;       // Edit modda her frame uygula

    void Reset() { target = GetComponent<RectTransform>(); }
    void Awake() { if (applyOnAwake) Apply(); }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying && liveInEditMode) Apply();
    }
#endif

    public void Apply()
    {
        var rt = target ? target : GetComponent<RectTransform>();
        if (!rt) return;

        if (forceScaleOne) rt.localScale = Vector3.one;

        // Ekran boyunca yatay esnet, altta hizala
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);

        // Yükseklik + alttan ofset
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, bottomMargin);
    }
}
