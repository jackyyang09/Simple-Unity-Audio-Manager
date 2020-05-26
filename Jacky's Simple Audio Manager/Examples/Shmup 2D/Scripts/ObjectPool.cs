using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a large pool of objects so we don't have to constantly instantiate and destroy them at runtime
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The reference object to pool")]
    GameObject prefab = null;

    [SerializeField]
    [Tooltip("Spawn this many objects on start")]
    int objectsToSpawn = 100;

    List<GameObject> pool;

    // Start is called before the first frame update
    void Start()
    {
        pool = new List<GameObject>();

        for (int i = 0; i < objectsToSpawn; i++)
        {
            pool.Add(Instantiate(prefab, transform));
            pool[i].SetActive(false);
        }
    }

    /// <summary>
    /// Returns the first available object
    /// </summary>
    /// <returns></returns>
    public GameObject GetObject()
    {
        foreach (GameObject g in pool)
        {
            if (!g.activeSelf)
            {
                return g;
            }
        }
        return null;
    }
}
