using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _objects = new();
    private readonly HashSet<T> _inPoolSet = new();
    private readonly T _prefab;
    private readonly Transform _parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = GameObject.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _objects.Enqueue(obj);
            _inPoolSet.Add(obj);
        }
    }

    public T GetObject()
    {
        if (_objects.Count == 0)
        {
            var newObj = GameObject.Instantiate(_prefab, _parent);
            newObj.gameObject.SetActive(false);
            _objects.Enqueue(newObj);
            _inPoolSet.Add(newObj);
        }

        T obj = _objects.Dequeue();
        _inPoolSet.Remove(obj);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void ReturnObject(T obj)
    {
        if (obj == null) return;

        // Prevent double-returning which corrupts the pool queue
        if (_inPoolSet.Add(obj))
        {
            obj.gameObject.SetActive(false);
            _objects.Enqueue(obj);
        }
    }

    public void Clear()
    {
        foreach (T obj in _objects)
        {
            if (obj != null)
            {
                GameObject.Destroy(obj.gameObject);
            }
        }
        _objects.Clear();
        _inPoolSet.Clear();
    }
}