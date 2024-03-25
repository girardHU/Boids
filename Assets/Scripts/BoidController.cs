using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class BoidController : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10.0f;
    [SerializeField] private float steeringForce = 8.0f;
    [SerializeField] private int nbRays = 1200;
    [SerializeField] private float rayMaxDistance = 4.0f;
    [SerializeField] private float dodgeAngle = 40.0f;
    [SerializeField] private float destinationValidationRadius = 5.0f;
    [SerializeField] private float destinationChoosingRange = 100.0f;
    [SerializeField] private float collisionSphereRadius = 3.0f;
    [SerializeField] private float influenceSphereRadius = 10.0f;
    [SerializeField] private bool debug = false;

    private Vector3 destination;
    private Vector3 position;
    private Vector3 velocity;
    [SerializeField] private Vector3 desiredDirection;
    private List<Vector3> rays;
    private List<Vector3> sortedRays;

    public delegate void KillBoid();
    public static event KillBoid boidDeathEvent;

    void Start()
    {
        destination = SelectRandomDestination();
        position = transform.position;
        rays = GenGoldenSpiralPoints(nbRays); // Generate the rays
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, destination) < destinationValidationRadius)
        {
            destination = SelectRandomDestination();
        }
        HandleMovement();
        CastPhysicsRays(debug);
    }

    private void HandleMovement()
    {
        // Find the Boids in both spheres around the current Boid and filter out itself
        Collider[] collisionsRiskyBoids = Physics.OverlapSphere(transform.position, collisionSphereRadius, LayerMask.GetMask("Boids"));
        Collider[] influencingBoids = Physics.OverlapSphere(transform.position, influenceSphereRadius, LayerMask.GetMask("Boids"));
        collisionsRiskyBoids = collisionsRiskyBoids.Where(boidCol => boidCol.gameObject != gameObject).ToArray();
        influencingBoids = influencingBoids.Where(boidCol => boidCol.gameObject != gameObject).ToArray();

        float xSum = 0;
        float ySum = 0;
        float zSum = 0;
        Vector3 awayFromMidPoint = Vector3.zero;
        Vector3 towardsMidPoint = Vector3.zero;
        Vector3 averageBoidsDirection = Vector3.zero;
        float closestBoidDistance = 1;

        if (collisionsRiskyBoids.Length > 0)
        {
            // Separation
            // Not fully tested yet hehe
            collisionsRiskyBoids.OrderBy(boidCol => transform.position - boidCol.transform.position);
            closestBoidDistance = collisionsRiskyBoids[0].transform.position.magnitude;
            foreach (Collider boidCol in collisionsRiskyBoids)
            {
                xSum += boidCol.transform.position.x;
                ySum += boidCol.transform.position.y;
                zSum += boidCol.transform.position.z;
            }
            Vector3 riskyMiddlePoint = new(xSum / collisionsRiskyBoids.Length, ySum / collisionsRiskyBoids.Length, zSum / collisionsRiskyBoids.Length);
            // awayFromMidPoint = riskyMiddlePoint - transform.position;
            awayFromMidPoint = riskyMiddlePoint;
            // End

            // Alignement
            // Vector3 averageBoidsDirection = influencingBoids.Average(boidCol => boidCol.transform.forward);
            averageBoidsDirection = influencingBoids.Select(col => col.transform.up)
            .Aggregate(Vector3.zero, (acc, dir) => acc + dir) / influencingBoids.Length; // Sum and average
            averageBoidsDirection -= transform.position;
            // End

            // Cohesion
            foreach (Collider boidCol in influencingBoids)
            {
                xSum += boidCol.transform.position.x;
                ySum += boidCol.transform.position.y;
                zSum += boidCol.transform.position.z;
            }
            Vector3 cohesionMmiddlePoint = new(xSum / influencingBoids.Length, ySum / influencingBoids.Length, zSum / influencingBoids.Length);
            // towardsMidPoint = cohesionMmiddlePoint - transform.position;
            towardsMidPoint = cohesionMmiddlePoint;
            // End
        }



        Vector3 randomSteerForce = Vector3.zero; // TODO: Implement randomSteerForce

        desiredDirection = destination - transform.position;
        Vector3 desiredVelocity = (desiredDirection + averageBoidsDirection + towardsMidPoint + awayFromMidPoint + randomSteerForce).normalized * maxSpeed;
        // Debug.DrawRay(transform.position, desiredVelocity, Color.red);
        // Debug.DrawRay(transform.position, averageBoidsDirection * 5f, Color.blue);
        Debug.DrawRay(transform.position, towardsMidPoint * 5f, Color.yellow);
        Debug.DrawRay(transform.position, awayFromMidPoint * 5f, Color.green);
        Vector3 desiredSteeringForce = (desiredVelocity - velocity) * steeringForce;
        Vector3 acceleration = Vector3.ClampMagnitude(desiredSteeringForce, steeringForce) / 1;

        velocity = Vector3.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);
        position += velocity * Time.deltaTime;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
        transform.Rotate(90, 0, 0, Space.Self);
    }

    private Vector3 SelectRandomDestination()
    {
        return new Vector3(Random.Range(-destinationChoosingRange, destinationChoosingRange),
                           Random.Range(-destinationChoosingRange, destinationChoosingRange),
                           Random.Range(-destinationChoosingRange, destinationChoosingRange));
    }

    private void CastPhysicsRays(bool debug = false)
    {
        // TODO : When too close to a wall, the boid keeps changing direction and ends up in the said wall... FIX IT
        sortedRays = rays.Where(ray => Vector3.Angle(transform.up, ray) < 90.0f).ToList(); // Keep only the rays going in a half sphere in front of the boid
        sortedRays = sortedRays.OrderBy(ray => Vector3.Angle(transform.up, ray)).ToList(); // Sort the rays by lowest angle compared to the forward of the boid
        if (debug)
            DrawRays();

        foreach (Vector3 ray in sortedRays)
        {
            if (Vector3.Angle(transform.up, ray) < dodgeAngle)
            {
                if (Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacles")))
                {
                    // Select the closest Vector to forward which does not collide with anything
                    // TODO : increase steering force according to how close to the obstacle the boid is
                    destination = sortedRays.First(ray => !Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacles")));
                    return;
                }
            }
        }
    }

    private void DrawRays()
    {
        foreach (Vector3 ray in sortedRays)
        {
            Color rayColor = new(1, 1, 1, 0.15f);
            if (Vector3.Angle(transform.up, ray) < dodgeAngle)
            {
                if (Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacles"))) // color forward rays in Red if they hit an obstacle
                {
                    rayColor = Color.red;
                }
            }
            Debug.DrawRay(transform.position, ray * rayMaxDistance, rayColor);
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            boidDeathEvent?.Invoke();
            Destroy(gameObject);
        }
    }

    // void OnDrawGizmos()
    // {
    //     Gizmos.color = new Color(1f, .99f, .8f, .2f);
    //     Gizmos.DrawSphere(transform.position, influenceSphereRadius);
    // }

    private List<Vector3> GenGoldenSpiralPoints(int numberPoints)
    {
        List<Vector3> rays = new();
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

            rays.Add(new Vector3((float)x, (float)y, (float)z));
        }
        return rays;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Boid"))
        {
            // Debug.Log("Carambolage de Boids");
        }
    }

    void FixedUpdate()
    {
        // separation: steer to avoid crowding local flockmates

        // alignment: steer towards the average heading of local flockmates

        // cohesion: steer to move towards the average position (center of mass) of local flockmates

    }
}
