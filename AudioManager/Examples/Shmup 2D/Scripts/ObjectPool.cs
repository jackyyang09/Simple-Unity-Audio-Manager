using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    [SerializeField]
    int objectsToSpawn = 100;

    List<GameObject> pool;

    // Start is called before the first frame update
    void Start()
    {
        pool = new List<GameObject>();

        for (int i = 0; i < 100; i++)
        {
            pool.Add(Instantiate(prefab, transform));
            pool[i].SetActive(false);
        }
    }

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

    // Update is called once per frame
    //void Update()
    //{
    //    
    //}
}
