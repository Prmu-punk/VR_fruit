using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Spawner : MonoBehaviour
{
    private Collider spawnArea;

    public GameObject[] fruitPrefabs;
    public GameObject bombPrefab;
    [Range(0f, 1f)]
    public float bombChance = 0.05f;

    public float minSpawnDelay = 0.25f;
    public float maxSpawnDelay = 1f;

    public float minAngle = -15f;
    public float maxAngle = 15f;

    public float minForce = 18f;
    public float maxForce = 22f;

    public float maxLifetime = 5f;
    public float minSpawnedObjectScale = 0.22f;
    public float maxSpawnedObjectScale = 0.3f;
    public float minSideForce = -0.55f;
    public float maxSideForce = 0.55f;
    public float minDepthForce = 0.18f;
    public float maxDepthForce = 0.55f;

    private void Awake()
    {
        spawnArea = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        StartCoroutine(Spawn());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Spawn()
    {
        yield return new WaitForSeconds(2f);

        while (enabled)
        {
            GameObject prefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

            if (Random.value < bombChance) {
                prefab = bombPrefab;
            }

            Vector3 position = new Vector3
            {
                x = Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
                y = Random.Range(spawnArea.bounds.min.y, spawnArea.bounds.max.y),
                z = Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
            };

            Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(minAngle, maxAngle));

            GameObject fruit = Instantiate(prefab, position, rotation);
            fruit.transform.localScale *= Random.Range(minSpawnedObjectScale, maxSpawnedObjectScale);

            float force = Random.Range(minForce, maxForce);
            Vector3 launchDirection = new Vector3(Random.Range(minSideForce, maxSideForce), force, Random.Range(minDepthForce, maxDepthForce));
            fruit.GetComponent<Rigidbody>().AddForce(launchDirection, ForceMode.Impulse);

            Fruit fruitScript = fruit.GetComponent<Fruit>();
            if (fruitScript != null) {
                fruitScript.BeginLifetime(maxLifetime);
            } else {
                Destroy(fruit, maxLifetime);
            }

            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
        }
    }

}
