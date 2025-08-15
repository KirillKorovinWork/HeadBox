using UnityEngine;
using System.Collections.Generic;

public class DecalPool
{
    Queue<Transform> pool = new Queue<Transform>();
    Transform parent;

    public DecalPool(GameObject prefab, int size, Transform parent)
    {
        this.parent = parent;
        if (prefab == null || size <= 0) return;
        for (int i = 0; i < size; i++)
        {
            var go = Object.Instantiate(prefab, parent);
            // Ensure decals are purely visual (no colliders)
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int ci = 0; ci < cols.Length; ci++) Object.Destroy(cols[ci]);
            go.SetActive(false);
            pool.Enqueue(go.transform);
        }
    }

    public void Spawn(Vector3 pos, Quaternion rot, float size)
    {
        if (pool.Count == 0) return;
        var t = pool.Dequeue();
        t.gameObject.SetActive(true);
        t.position = pos;
        t.rotation = rot;
        t.localScale = Vector3.one * size;
        pool.Enqueue(t);
    }
}