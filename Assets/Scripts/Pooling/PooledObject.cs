using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject prefabKey;
    [HideInInspector] public PoolManager owner;
}
