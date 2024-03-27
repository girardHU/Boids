using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private int nbBoids = 25;
    [SerializeField] private GameObject boidPrefab;
    private Vector3 dest;
    // Start is called before the first frame update
    void Start()
    {

        dest = BoidController.SelectRandomDestination();
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
        SpawnBoid();
    }

    private void SpawnBoid()
    {
        GameObject inst = Instantiate(boidPrefab, Vector3.zero, boidPrefab.transform.rotation);
        inst.GetComponent<BoidController>().destination = dest;
    }


}
