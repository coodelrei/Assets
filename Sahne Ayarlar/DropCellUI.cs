using UnityEngine;

[DisallowMultipleComponent]
public class DropCell : MonoBehaviour
{
    [Header("Yerleþtirme")]
    public Vector3 spawnOffset = Vector3.zero;   // Hücre merkezine göre ofset (istersen)

    /// <summary>
    /// Panelden sürükleyip bu hücre üstüne býraktýðýnda çaðrýlýr.
    /// Burada GOLD kontrolü yapýlýr, yeterliyse harcanýr ve prefab hücrede spawn edilir.
    /// </summary>
    public bool TryPlace(GameObject prefab, int price)
    {
        if (prefab == null) return false;

        // Altýn kontrolü
        if (GoldManager.Instance != null)
        {
            if (!GoldManager.Instance.SpendGold(price))
            {
                Debug.Log("Yetersiz altýn.");
                return false;
            }
        }

        // Yerleþtir
        Vector3 pos = transform.position + spawnOffset;
        pos.z = 0f;
        Instantiate(prefab, pos, Quaternion.identity);

        return true;
    }
}
