using UnityEngine;

[DefaultExecutionOrder(-200)]
public class BarYoneticisi2D : MonoBehaviour
{
    public static BarYoneticisi2D Aktif { get; private set; }

    [Header("Sýralama / Z")]
    public string sortingLayerName = "Default";
    public int sortingOrderBase = 200;
    public float worldZ = 0f;

    [Header("Renkler")]
    public Color arkaPlanRenk = new Color(0, 0, 0, 0.55f);
    public Color dostBirimRenk = new Color(0.20f, 1f, 0.20f, 1f);
    public Color dusmanBirimRenk = new Color(1f, 0.25f, 0.25f, 1f);
    public Color binaCanRenk = new Color(0.2f, 0.8f, 1f, 1f);
    public Color binaSpawnRenk = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Boyutlar (Dünya birimi)")]
    public float birimBarGenislik = 1.1f;
    public float binaBarGenislik = 1.8f;
    public float barYukseklik = 0.15f;

    public Sprite BeyazSprite { get; private set; }

    void OnEnable()
    {
        Aktif = this;
        if (BeyazSprite == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            BeyazSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            BeyazSprite.name = "RuntimeWhite1x1";
        }
    }
    void OnDisable() { if (Aktif == this) Aktif = null; }

    public void SecimdenRenkVeGenislik(CanBarBaglayici2D.BarTipi tip, Taraf2D taraf,
                                       out Color bg, out Color fill, out float genislik)
    {
        bg = arkaPlanRenk;
        switch (tip)
        {
            default:
            case CanBarBaglayici2D.BarTipi.BirimSaglik:
                fill = (taraf == Taraf2D.Dost) ? dostBirimRenk : dusmanBirimRenk;
                genislik = birimBarGenislik;
                break;
            case CanBarBaglayici2D.BarTipi.BinaSaglik:
                fill = binaCanRenk;
                genislik = binaBarGenislik;
                break;
            case CanBarBaglayici2D.BarTipi.BinaSpawn:
                fill = binaSpawnRenk;
                genislik = binaBarGenislik;
                break;
        }
    }
}
