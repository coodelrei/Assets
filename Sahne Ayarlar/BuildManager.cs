using UnityEngine;

[DisallowMultipleComponent]
public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    GameObject _pendingPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void YerlesimModunaGec(GameObject prefab)
    {
        _pendingPrefab = prefab;
    }

    void Update()
    {
        if (_pendingPrefab == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;

            Instantiate(_pendingPrefab, world, Quaternion.identity);
            _pendingPrefab = null;
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Sað klik cancel
            _pendingPrefab = null;
        }
    }
}
