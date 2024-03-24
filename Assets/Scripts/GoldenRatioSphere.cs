using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenRatioSphere : MonoBehaviour
{
    public GameObject point;
    [SerializeField] private int nbPoints;
    [SerializeField] private List<Vector3> pointsOnTheSphere;
    private List<GameObject> pointObjSpawned;

    // Start is called before the first frame update
    void Start()
    {
        pointObjSpawned = new();
        nbPoints = 1;
    }

    // Update is called once per frame
    void Update()
    {
        nbPoints += 1;
        GenerateGoldenSpiralPoints(nbPoints);

        DeletePointObjs();

        foreach (Vector3 p in pointsOnTheSphere)
        {
            pointObjSpawned.Add(Instantiate(point, p + transform.position, point.transform.rotation));
        }
    }

    private void DeletePointObjs()
    {
        if (pointObjSpawned.Count > 0)
        {
            foreach (GameObject inst in pointObjSpawned)
            {
                Destroy(inst);
            }
        }
    }

    private void GenerateGoldenSpiralPoints(int numberPoints)
    {
        pointsOnTheSphere = new();
        float phi, theta;
        float x, y, z;

        float pi = Mathf.PI;
        float offset = 0.5f;

        for (int i = 0; i < numberPoints; i++)
        {
            phi = Mathf.Acos(1 - 2 * (i + offset) / numberPoints);
            theta = pi * (1 + Mathf.Sqrt(5)) * (i + offset);

            x = Mathf.Cos(theta) * Mathf.Sin(phi);
            y = Mathf.Sin(theta) * Mathf.Sin(phi);
            z = Mathf.Cos(phi);

            pointsOnTheSphere.Add(new Vector3((float)x, (float)y, (float)z));
        }
    }

    // private void GenerateGoldenSpiralPoints(int samples = 1000)
    // {
    //     pointsOnTheSphere = new();
    //     float phi = Mathf.PI * (Mathf.Sqrt(5.0f) - 1.0f);
    //     for (int i = 0; i < 1000; i++)
    //     {
    //         float y = 1 - i / (float)(samples - 1) * 2; // y goes from 1 to -1
    //         y = Mathf.Clamp(y, -1f, 1f);
    //         float radius = Mathf.Sqrt(1 - y * y); // radius at y

    //         float theta = phi * i; // golden angle increment
    //         float x = Mathf.Cos(theta) * radius;
    //         float z = Mathf.Sin(theta) * radius;

    //         pointsOnTheSphere.Add(new Vector3(x, y, z));
    //     }
    // }
}
