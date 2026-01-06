using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

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
            currCooldown = respawnCooldownSec;
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

    [SerializeField] private bool spawnObstacles = true;
    [SerializeField] private bool spawnAnimals = true;
    [SerializeField] private bool allowRespawn = true;
    private bool firstInstantiate = true;

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

        if (spawnObstacles)
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
        if (firstInstantiate)
        {
            firstInstantiate = false;
            foreach (AnimalCounter animalCounter in animalCounters)
            {
                for (int i = 0; i < animalCounter.targetAmount; i++)
                    TryInstantiateAnimal(animalCounter);
            }
        }

        CheckRespawnCounters();
    }

    private void TryInstantiateAnimal(AnimalCounter counter)
    {
        if (TryGetRandomPointOnNavMesh(spawnPlane.transform.position, planeXEdge, planeZEdge, out Vector3 spawnPos))
        {
            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject animal = Instantiate(counter.spawnData.animalPrefab, spawnPos, randomRotation, counter.container.transform);
            if (counter.spawnData.animalName.Length != 0)
                animal.name = counter.spawnData.animalName + " " + counter.currAnimalIndex++;
            animal.GetComponent<NavMeshAgent>().Warp(spawnPos);
            counter.animalObjectList.Add(animal);
        }
    }

    private bool TryGetRandomPointOnNavMesh(Vector3 center, float rangeX, float rangeZ, out Vector3 result, int areaMask = NavMesh.AllAreas)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-rangeX, rangeX),
                0f,
                Random.Range(-rangeZ, rangeZ)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 0.5f, areaMask))
            {
                result = hit.position;
                float checkRadius = 0.5f;
                if (Physics.CheckSphere(result, checkRadius, LayerMask.GetMask("Obstacles")))
                    continue;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    private void CheckRespawnCounters()
    {
        foreach (AnimalCounter counter in animalCounters)
        {
            for (int i = 0; i < counter.animalObjectList.Count; i++)
            {
                if (counter.animalObjectList[i] == null)
                    counter.animalObjectList.RemoveAt(i);
            }

            if (counter.animalObjectList.Count < counter.targetAmount && allowRespawn)
            {
                counter.currCooldown -= Time.deltaTime;
                if (counter.currCooldown <= 0)
                {
                    TryInstantiateAnimal(counter);
                    counter.currCooldown = counter.respawnCooldownSec;
                }
            }
        }
    }
}
