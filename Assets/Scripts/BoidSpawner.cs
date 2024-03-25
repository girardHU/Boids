using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private int nbBoids = 25;
    [SerializeField] private GameObject boidPrefab;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < nbBoids; i += 1)
        {
            SpawnBoid();
        }
    }

    void OnEnable()
    {
        BoidController.boidDeathEvent += OnBoidDeath;
    }

    void OnDisable()
    {
        BoidController.boidDeathEvent -= OnBoidDeath;
    }

    public void OnBoidDeath()
    {
        Debug.Log("Event received in BoidSpawner");
        SpawnBoid();
    }

    private void SpawnBoid()
    {
        Instantiate(boidPrefab, Vector3.zero, boidPrefab.transform.rotation);
    }


}
