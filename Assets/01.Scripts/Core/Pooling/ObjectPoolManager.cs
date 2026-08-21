using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Pooling
{
    public sealed class ObjectPoolManager : MonoBehaviour
    {
        [SerializeField] private Transform _poolRoot;

        private readonly Dictionary<Component, Queue<Component>> _availableByPrefab = new();
        private readonly Dictionary<Component, Component> _prefabByInstance = new();
        private readonly HashSet<Component> _pooledInstances = new();

        public static ObjectPoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Only one ObjectPoolManager can be active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            if (_poolRoot == null)
            {
                _poolRoot = transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot spawn a null prefab.", this);
                return null;
            }

            if (!_availableByPrefab.TryGetValue(prefab, out Queue<Component> available))
            {
                available = new Queue<Component>();
                _availableByPrefab.Add(prefab, available);
            }

            T instance = TakeAvailable<T>(available);
            if (instance == null)
            {
                instance = Instantiate(prefab, position, rotation, _poolRoot);
                _prefabByInstance.Add(instance, prefab);
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(Component instance)
        {
            if (instance == null || _pooledInstances.Contains(instance))
            {
                return;
            }

            if (!_prefabByInstance.TryGetValue(instance, out Component prefab))
            {
                Debug.LogError("The returned instance was not created by this pool.", instance);
                instance.gameObject.SetActive(false);
                return;
            }

            NotifyDespawned(instance);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);

            if (!_availableByPrefab.TryGetValue(prefab, out Queue<Component> available))
            {
                available = new Queue<Component>();
                _availableByPrefab.Add(prefab, available);
            }

            _pooledInstances.Add(instance);
            available.Enqueue(instance);
        }

        private T TakeAvailable<T>(Queue<Component> available) where T : Component
        {
            while (available.Count > 0)
            {
                Component candidate = available.Dequeue();
                if (candidate == null)
                {
                    continue;
                }

                _pooledInstances.Remove(candidate);
                return candidate as T;
            }

            return null;
        }

        private static void NotifyDespawned(Component instance)
        {
            if (instance is IPoolable poolable)
            {
                poolable.OnDespawned();
            }
        }
    }
}
