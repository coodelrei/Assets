using UnityEngine;

public class MergeData : MonoBehaviour
{
    [Header("Merge Bilgisi")]
    [Tooltip("Ayný anahtara sahip binalar birbirleriyle birleþir.")]
    public string mergeKey = "Building";

    [Min(0)]
    public int level = 0;

    [Tooltip("Bu bina birleþtiðinde doðacak bir üst seviye prefab (ör. Level2 prefab).")]
    public Unit nextLevelPrefab;

    [Tooltip("Bu bina merge’e uygun mu?")]
    public bool canMerge = true;
}
