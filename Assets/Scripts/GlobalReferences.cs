using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance { get; set; }

    public GameObject bulletImpactEffectPrefab;
    public GameObject zombieImpactEffectPrefab; // <-- Agrega esta línea

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
          Destroy(gameObject);
        }
        else
        {
          Instance = this;
        }
    }
}
