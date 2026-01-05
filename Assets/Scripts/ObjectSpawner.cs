using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout;

public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    private struct ObstacleSpawnData
    {
        public GameObject obstaclePrefab;
        public int amount;
        public string containerName;
        [Range(0.1f, 100f)] public float minScale;
        [Range(0.1f, 100f)] public float maxScale;
    }

    [System.Serializable]
    private struct AnimalSpawnData
    {
        public GameObject animalPrefab;
        public int amount;
        public string animalName;
        public string containerName;
        public int respawnTimeSec;
    }

    private class AnimalCounter
    {
        public List<GameObject> animalObjectList;
        public int targetAmount;
        public int respawnCooldownSec;
        public float currCooldown;
        public int currAnimalIndex;

        public GameObject container;
        public AnimalSpawnData spawnData;
        public AnimalCounter(int targetAmount, int respawnCooldownSec, GameObject container, AnimalSpawnData spawnData)
        {
            this.container = container;
            animalObjectList = new List<GameObject>();
            this.targetAmount = targetAmount;
            this.respawnCooldownSec = respawnCooldownSec;
            currCooldown = 0;
            currAnimalIndex = 0;
            this.spawnData = spawnData;
        }
    }

    [Header("Referencja do obiektu p³aszczyzny")]
    [SerializeField] private GameObject spawnPlane;
    private float planeXEdge = 0f;
    private float planeZEdge = 0f;

    [Header("Lista prefabów przeszkód generowanych losowo")]
    [SerializeField] private List<ObstacleSpawnData> obstacleSpawnDatas = new List<ObstacleSpawnData>();

    [Header("Lista prefabów zwierz¹t")]
    [SerializeField] private List<AnimalSpawnData> animalSpawnDatas = new List<AnimalSpawnData>();

    [SerializeField] private bool spawnObstables = true;
    [SerializeField] private bool spawnAnimals = true;

    private List<AnimalCounter> animalCounters = new List<AnimalCounter>();
    void Awake()
    {
        if (spawnPlane == null)
        {
            Debug.LogError("ObjectSpawner: nie przypisano referencji do powierzchni!", this);
            return;
        }

        Vector3 planeCenter = spawnPlane.transform.position;
        planeXEdge = 5f * spawnPlane.transform.localScale.x;
        planeZEdge = 5f * spawnPlane.transform.localScale.z;

        if (spawnObstables)
        {
            foreach (ObstacleSpawnData objectData in obstacleSpawnDatas)
            {
                if (objectData.obstaclePrefab == null)
                {
                    Debug.LogWarning("ObjectSpawner: nie uda³o siê utworzyæ obiektu przeszkody - nie podano prefabu!", this);
                    continue;
                }
                string name;
                if (objectData.containerName == null)
                    name = "Container";
                else
                    name = objectData.containerName;

                GameObject container = new GameObject(name);
                container.transform.position = Vector3.zero;

                for (int i = 0; i < objectData.amount; i++)
                {
                    GameObject obstacle = Instantiate(objectData.obstaclePrefab);
                    obstacle.transform.SetParent(container.transform);
                    obstacle.transform.localPosition = Vector3.zero;
                    float randomScale = Random.Range(objectData.minScale, objectData.maxScale);
                    float randomPosX = Random.Range(planeCenter.x - planeXEdge, planeCenter.x + planeXEdge);
                    float randomPosZ = Random.Range(planeCenter.z - planeZEdge, planeCenter.z + planeZEdge);
                    obstacle.transform.localScale = new Vector3(randomScale, 1, randomScale);
                    obstacle.transform.position = new Vector3(randomPosX, 0, randomPosZ);
                }
            }
        }

        if (spawnAnimals)
        {
            foreach (AnimalSpawnData animalData in animalSpawnDatas)
            {
                if (animalData.animalPrefab == null)
                {
                    Debug.LogWarning("ObjectSpawner: nie uda³o siê utworzyæ obiektu zwierzêcia - nie podano prefabu!", this);
                    continue;
                }
                string name;
                if (animalData.containerName == null)
                    name = "Container";
                else
                    name = animalData.containerName;

                GameObject container = new GameObject(name);
                container.transform.position = Vector3.zero;

                NavMeshAgent prefabNavAgent = animalData.animalPrefab.GetComponent<NavMeshAgent>();
                if (prefabNavAgent == null)
                {
                    Debug.LogWarning("ObjectSpawner: nie uda³o siê utworzyæ obiektu zwierzêcia - prefab nie zawiera komponentu NavMeshAgent!", this);
                    continue;
                }
                AnimalCounter newCounter = new AnimalCounter(animalData.amount, animalData.respawnTimeSec, container, animalData);
                animalCounters.Add(newCounter);
            }
        }
    }


    void LateUpdate()
    {
        foreach (AnimalCounter animalCounter in animalCounters)
        {
            if (animalCounter.targetAmount > animalCounter.animalObjectList.Count)
            {
                TryInstantiateAnimal(animalCounter);
            }
        }
    }

    private void TryInstantiateAnimal(AnimalCounter counter)
    {
        if (TryGetRandomPointOnNavMesh(spawnPlane.transform.position, planeXEdge, planeZEdge, out Vector3 spawnPos))
        {
            GameObject animal = Instantiate(counter.spawnData.animalPrefab, spawnPos, Quaternion.identity, counter.container.transform);
            if (counter.spawnData.animalName.Length != 0)
                animal.name = counter.spawnData.animalName + " " + counter.currAnimalIndex++;
            animal.GetComponent<NavMeshAgent>().Warp(spawnPos);
            counter.animalObjectList.Add(animal);
        }
        else
        {
            Debug.LogWarning("Nie znaleziono punktu na NavMeshu!");
        }
    }

    private bool TryGetRandomPointOnNavMesh(Vector3 center, float rangeX, float rangeZ, out Vector3 result, int areaMask = NavMesh.AllAreas)
    {
        for (int i = 0; i < 1; i++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-rangeX, rangeX),
                0f,
                Random.Range(-rangeZ, rangeZ)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, areaMask))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
}
