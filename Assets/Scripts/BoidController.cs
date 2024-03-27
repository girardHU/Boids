using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using System;

public class BoidController : MonoBehaviour
{
    // Tweeking Variables
    [SerializeField] private float maxSpeed = 10.0f;
    // [SerializeField] private float steeringForce = 15.0f;
    private float steeringForce;
    [SerializeField] private int nbRays = 1200;
    [SerializeField] private float rayMaxDistance = 4.0f;
    [SerializeField] private float dodgeAngle = 40.0f;
    [SerializeField] private float destinationValidationRadius = 5.0f;
    [SerializeField] private float destinationChoosingRange = 100.0f;
    [SerializeField] private float collisionSphereRadius = 3.0f;
    [SerializeField] private float influenceSphereRadius = 10.0f;
    // [SerializeField] private float criticalCollDist = 4.0f;
    private float criticalCollDist;

    [SerializeField] private bool displayInfluenceRadius = false;
    [SerializeField] private bool displayRayCasts = false;
    [SerializeField] private bool displayDirections = false;
    [SerializeField] private bool displayBoidCollisionMsg = false;

    // Compute Variables
    public Vector3 destination;
    private Vector3 position;
    private Vector3 velocity;

    private Vector3 desiredDirection;
    private Vector3 awayFromMidPoint;
    private Vector3 towardsMidPoint;
    private Vector3 averageBoidsDirection;
    private Vector3 awayFromObstacle;

    private List<Vector3> rays;
    private List<Vector3> sortedRays;
    private RaycastHit hit;

    // Delegates
    public delegate void KillBoid();
    public static event KillBoid boidDeathEvent;

    void Start()
    {
        steeringForce = maxSpeed * 1.5f;
        criticalCollDist = maxSpeed * 0.4f;
        position = transform.position;
        rays = GenGoldenSpiralPoints(nbRays); // Generate the rays
        StartCoroutine(CastPhysicsRays());
    }

    void Update()
    {
        // Select new destination if previous one is reached
        if (Vector3.Distance(transform.position, destination) < destinationValidationRadius)
        {
            destination = SelectRandomDestination();
        }
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Find the Boids in both spheres around the current Boid
        Collider[] collisionsRiskyBoids = Physics.OverlapSphere(transform.position, collisionSphereRadius, LayerMask.GetMask("Boids"));
        Collider[] influencingBoids = Physics.OverlapSphere(transform.position, influenceSphereRadius, LayerMask.GetMask("Boids"));
        // Filter out itself
        collisionsRiskyBoids = collisionsRiskyBoids.Where(boidCol => boidCol.gameObject != gameObject).ToArray();
        // influencingBoids = influencingBoids.Where(boidCol => boidCol.gameObject != gameObject).ToArray();

        float xSum = 0;
        float ySum = 0;
        float zSum = 0;
        awayFromMidPoint = Vector3.zero;
        towardsMidPoint = Vector3.zero;
        averageBoidsDirection = Vector3.zero;
        // float closestBoidDistance = 1;

        if (collisionsRiskyBoids.Length > 0)
        {
            // Separation
            // Not fully tested yet hehe
            collisionsRiskyBoids.OrderBy(boidCol => transform.position - boidCol.transform.position);
            // closestBoidDistance = collisionsRiskyBoids[0].transform.position.magnitude;
            foreach (Collider boidCol in collisionsRiskyBoids)
            {
                xSum += boidCol.transform.position.x;
                ySum += boidCol.transform.position.y;
                zSum += boidCol.transform.position.z;
            }
            Vector3 riskyMiddlePoint = new(xSum / collisionsRiskyBoids.Length, ySum / collisionsRiskyBoids.Length, zSum / collisionsRiskyBoids.Length);
            awayFromMidPoint = transform.position - riskyMiddlePoint;
            // End

            // Alignement
            averageBoidsDirection = influencingBoids.Select(col => col.transform.up)
            .Aggregate(Vector3.zero, (acc, dir) => acc + dir) / influencingBoids.Length; // Sum and average
            // End

            // Cohesion
            Vector3 sumOfPositions = Vector3.zero;
            foreach (Collider boidCol in influencingBoids)
            {
                sumOfPositions += boidCol.gameObject.transform.position;
            }
            Vector3 cohesionMmiddlePoint = sumOfPositions / influencingBoids.Length;
            towardsMidPoint = cohesionMmiddlePoint - transform.position;
            // End
        }


        Vector3 randomSteerForce = Vector3.zero; // TODO: Implement randomSteerForce

        desiredDirection = destination - transform.position;
        // TODO: Apply each steeringStrength independently

        Vector3 desiredDirectionScaled = desiredDirection.normalized * steeringForce;
        Vector3 awayFromObstacleScaled = awayFromObstacle.normalized * (float)Math.Pow(rayMaxDistance / Mathf.Max(hit.distance, 0.0000001f), 3);
        Vector3 towardsMidPointScaled = Vector3.zero;
        Vector3 awayFromMidPointScaled = Vector3.zero;
        Vector3 averageBoidsDirectionScaled = Vector3.zero;

        if (!(hit.distance > criticalCollDist))
        {
            towardsMidPointScaled = towardsMidPoint.normalized * Mathf.Max(Mathf.Pow(Vector3.Distance(transform.position, towardsMidPoint), 2), steeringForce);
            awayFromMidPointScaled = awayFromMidPoint.normalized * Mathf.Max((float)Math.Pow(1 / Mathf.Max(Vector3.Distance(transform.position, awayFromMidPoint), 0.0000001f), 2), steeringForce);
            averageBoidsDirectionScaled = averageBoidsDirection.normalized * (float)Math.Pow(Mathf.Max(hit.distance, 0.0000001f), 2);
        }

        Vector3 desiredVelocity = (
            desiredDirectionScaled
            + awayFromObstacleScaled
            + averageBoidsDirectionScaled
            + towardsMidPointScaled
            + awayFromMidPointScaled
            + randomSteerForce) * maxSpeed;

        if (displayDirections)
        {
            Debug.DrawRay(transform.position, desiredVelocity, Color.cyan);
            Debug.DrawRay(transform.position, awayFromObstacleScaled, Color.magenta);
            // Debug.DrawRay(transform.position, desiredDirectionScaled, Color.blue);

            Debug.DrawRay(transform.position, averageBoidsDirectionScaled, Color.blue);
            Debug.DrawRay(transform.position, towardsMidPointScaled, Color.green);
            Debug.DrawRay(transform.position, awayFromMidPointScaled, Color.yellow);
        }
        Vector3 desiredSteeringForce = desiredVelocity - velocity;
        // Vector3 acceleration = desiredSteeringForce;
        Vector3 acceleration = Vector3.ClampMagnitude(desiredSteeringForce, steeringForce) / 1;

        velocity = Vector3.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);
        position += velocity * Time.deltaTime;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
        transform.Rotate(90, 0, 0, Space.Self);
    }

    public static Vector3 SelectRandomDestination()
    {

        float x = UnityEngine.Random.Range(-25f + 5, 25f - 5);
        float y = UnityEngine.Random.Range(-7.5f + 2, 7.5f - 2);
        float z = UnityEngine.Random.Range(-12.5f + 4, 12.5f - 4);

        return new Vector3(x, y, z);
    }

    private IEnumerator CastPhysicsRays()
    {
        yield return new WaitForSeconds(1f); // For proper layer initialization
        while (true)
        {
            hit = default;
            sortedRays = rays.Where(ray => Vector3.Angle(transform.up, ray) < 120.0f).ToList(); // Keep only the rays going in a half sphere in front of the boid
            sortedRays = sortedRays.OrderBy(ray => Vector3.Angle(transform.up, ray)).ToList(); // Sort the rays by lowest angle compared to the forward of the boid
            if (displayRayCasts)
                DrawRays();

            foreach (Vector3 ray in sortedRays)
            {
                if (Vector3.Angle(transform.up, ray) < dodgeAngle) // Consider only rays in a specified angle in front of the Boid
                {
                    Ray frontRay = new(transform.position, ray * rayMaxDistance);
                    awayFromObstacle = Vector3.zero;
                    if (Physics.Raycast(frontRay, out hit, LayerMask.GetMask("Obstacles")))
                    {
                        Debug.DrawRay(transform.position, ray * rayMaxDistance, Color.white);
                        // Select the closest Vector to forward which does not collide with anything
                        awayFromObstacle = rays.First(ray => !Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacles")));

                        if (hit.distance < criticalCollDist)
                        {
                            awayFromObstacle = hit.normal;
                        }

                        yield return new WaitForSeconds(.1f);
                    }
                }
            }
            yield return null;
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
                    Debug.DrawRay(transform.position, ray * rayMaxDistance, rayColor);
                }
            }
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Debug.Log("Boid Died");
            boidDeathEvent?.Invoke();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Boid") && displayBoidCollisionMsg)
        {
            Debug.Log("Carambolage de Boids");
        }
    }

    void OnDrawGizmos()
    {
        if (displayInfluenceRadius)
        {
            Gizmos.color = new Color(1f, .99f, .8f, .1f);
            Gizmos.DrawSphere(transform.position, influenceSphereRadius);
        }
    }

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

            rays.Add(new Vector3((float)x, (float)y, (float)z).normalized);
        }
        return rays;
    }

}
