using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [Header("Gold")]
    public int altin = 500;

    [Header("UI (opsiyonel)")]
    public TextMeshProUGUI goldText;           // Altýn sayýsýný yazdýracaðýn TMP
    public RectTransform goldTargetUI;         // Altýn barýndaki ikon/alan (hedef)
    public Camera uiCamera;                    // Canvas 'Screen Space - Camera' ise burayý ata (yoksa boþ býrak)

    [Header("World Target")]
    [Tooltip("Altýn pickup'larýnýn uçacaðý dünya konumu (otomatik güncellenir, gerekirse manuel de verebilirsin).")]
    public Vector3 targetWorldPos = Vector3.zero;

    [Tooltip("Hedef world Z deðeri (2D projede genelde 0).")]
    public float targetWorldZ = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UIRefresh();
        RefreshTargetWorldPos(true);
    }

    void LateUpdate()
    {
        // UI hedefi varsa sürekli güncelle (panel hareketleri, kamera kaymasý vs. için)
        RefreshTargetWorldPos(false);
    }

    /// <summary>
    /// UI hedefinden targetWorldPos üretir. UI hedefi yoksa dokunmaz.
    /// </summary>
    void RefreshTargetWorldPos(bool force)
    {
        if (goldTargetUI == null) return;

        // UI objesinin ekran pozisyonu
        Vector3 screenPos;
        if (uiCamera != null)
            screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, goldTargetUI.position);
        else
            screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, goldTargetUI.position);

        // Dünya pozisyonu (ortografik kamerada z’yi sabitle)
        var cam = Camera.main != null ? Camera.main : uiCamera;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z - targetWorldZ)));
        world.z = targetWorldZ;

        // Ýlk kurulumda ya da sürekli güncelle
        if (force || (targetWorldPos - world).sqrMagnitude > 0.0001f)
            targetWorldPos = world;
    }

    // =========================
    //  ALTIN ÝÞLEMLERÝ (API)
    // =========================

    /// <summary>Eski koddaki beklentiyle uyumlu: altýn arttýr.</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        altin += amount;
        UIRefresh();
    }

    /// <summary>Geriye dönük uyumluluk (AddGold ile ayný iþi yapar).</summary>
    public void AltinEkle(int miktar) => AddGold(miktar);

    /// <summary>Altýný harca, yetmezse false döner.</summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (altin < amount) return false;
        altin -= amount;
        UIRefresh();
        return true;
    }

    /// <summary>Geriye dönük uyumluluk (SpendGold ile ayný iþi yapar).</summary>
    public void AltinHarca(int miktar) { SpendGold(miktar); }

    /// <summary>Yeterli mi kontrolü (shop vb. için).</summary>
    public bool AltinYeterliMi(int miktar) => altin >= miktar;

    // =========================
    //  UI
    // =========================

    void UIRefresh()
    {
        if (goldText != null) goldText.text = altin.ToString();
    }
}
