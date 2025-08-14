using System.Runtime.InteropServices;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private int nbBoids = 25;
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private Material[] materials;
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
        BoidController.BoidDeathEvent += OnBoidDeath;
    }

    void OnDisable()
    {
        BoidController.BoidDeathEvent -= OnBoidDeath;
    }

    public void OnBoidDeath()
    {
        SpawnBoid();
    }

    private void SpawnBoid()
    {
        GameObject inst = Instantiate(boidPrefab, Vector3.zero, boidPrefab.transform.rotation);
        inst.GetComponent<Renderer>().material = materials[Random.Range(0, materials.Length)];
    }


}
