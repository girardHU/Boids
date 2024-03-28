using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using System;

public class BoidController : MonoBehaviour
{
    // Tweeking Variables
    [SerializeField] private float maxSpeed = 10.0f;
    [SerializeField] private int nbRays = 600;
    [SerializeField] private float rayMaxDistance = 10.0f;
    [SerializeField] private float dodgeAngle = 30.0f;
    [SerializeField] private float collisionSphereRadius = 5.0f;
    [SerializeField] private float influenceSphereRadius = 15.0f;

    [SerializeField] private bool displayInfluenceRadius = false;
    [SerializeField] private bool displayRayCasts = false;
    [SerializeField] private bool displayDirections = false;
    [SerializeField] private bool displayBoidCollisionMsg = false;

    private float steeringForce;
    private float criticalCollDist;

    // Compute Variables
    public Vector3 destination;
    private Vector3 position;
    private Vector3 velocity;

    private Vector3 desiredDirection;
    private Vector3 awayFromObstacle;

    private List<Vector3> rays;
    private List<Vector3> sortedRays;
    private RaycastHit hit;

    private int boidMask;
    private int obstacleMask;

    // Delegates
    public delegate void KillBoid();
    public static event KillBoid BoidDeathEvent;


    void Start()
    {
        boidMask = LayerMask.GetMask("Boids");
        obstacleMask = LayerMask.GetMask("Obstacles");

        steeringForce = maxSpeed * 1.5f;
        criticalCollDist = maxSpeed * 0.4f;
        position = transform.position;

        rays = GenGoldenSpiralPoints(nbRays); // Generate the rays
        StartCoroutine(CastPhysicsRays());
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {

        Vector3 randomSteerForce = Vector3.zero; // TODO: Implement randomSteerForce

        desiredDirection = destination - transform.position;
        float avoidObstacleWeight = (float)Math.Pow(rayMaxDistance / Mathf.Max(hit.distance, 0.0000001f), 3);
        Vector3 awayFromObstacleScaled = awayFromObstacle.normalized * avoidObstacleWeight;

        Vector3 separationVect = ComputeSeparation();
        Collider[] influencingBoids = Physics.OverlapSphere(transform.position, influenceSphereRadius, boidMask);
        Vector3 alignementVect = ComputeAlignement(influencingBoids);
        Vector3 cohesionVect = ComputeCohesion(influencingBoids);

        Vector3 desiredVelocity = (
            desiredDirection.normalized * steeringForce
            + awayFromObstacleScaled
            + alignementVect
            + cohesionVect
            + separationVect
            + randomSteerForce) * maxSpeed;

        if (displayDirections)
        {
            Debug.DrawRay(transform.position, desiredVelocity, Color.cyan);
            Debug.DrawRay(transform.position, awayFromObstacleScaled, Color.magenta);
            // Debug.DrawRay(transform.position, desiredDirectionScaled, Color.blue);

            Debug.DrawRay(transform.position, alignementVect, Color.blue);
            Debug.DrawRay(transform.position, cohesionVect, Color.green);
            Debug.DrawRay(transform.position, separationVect, Color.yellow);
        }
        Vector3 desiredSteeringForce = desiredVelocity - velocity;
        Vector3 acceleration = Vector3.ClampMagnitude(desiredSteeringForce, steeringForce) / 1;

        velocity = Vector3.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);
        position += velocity * Time.deltaTime;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
        transform.Rotate(90, 0, 0, Space.Self);
    }

    private Vector3 ComputeSeparation()
    {
        // Finding nearby Boids colliders and filter itself out
        Collider[] collisionsRiskyBoids = Physics.OverlapSphere(transform.position, collisionSphereRadius, boidMask);
        collisionsRiskyBoids = collisionsRiskyBoids.Where(boidCol => boidCol.gameObject != gameObject).ToArray();

        if (!(hit.distance > criticalCollDist)) // A remplacer par un meilleur weighting system
            return Vector3.zero;

        // Separation
        // Not fully tested yet hehe
        Vector3 awayFromMidPoint = Vector3.zero;

        if (collisionsRiskyBoids.Length > 0)
        {
            Vector3 sumOfPositions = Vector3.zero;
            collisionsRiskyBoids.OrderBy(boidCol => transform.position - boidCol.transform.position);
            foreach (Collider boidCol in collisionsRiskyBoids)
            {
                sumOfPositions += boidCol.gameObject.transform.position;
            }
            Vector3 riskyMiddlePoint = sumOfPositions / collisionsRiskyBoids.Length;
            awayFromMidPoint = transform.position - riskyMiddlePoint;
        }

        float weight = Mathf.Max((float)Math.Pow(1 / Mathf.Max(Vector3.Distance(transform.position, awayFromMidPoint), 0.0000001f), 2), steeringForce);
        return awayFromMidPoint.normalized * weight;

    }

    private Vector3 ComputeCohesion(Collider[] influencingBoids)
    {
        if (!(hit.distance > criticalCollDist)) // A remplacer par un meilleur weighting system
            return Vector3.zero;

        // Cohesion
        Vector3 towardsMidPoint = Vector3.zero;
        Vector3 sumOfPositions = Vector3.zero;
        foreach (Collider boidCol in influencingBoids)
        {
            sumOfPositions += boidCol.gameObject.transform.position;
        }
        Vector3 cohesionMmiddlePoint = sumOfPositions / influencingBoids.Length;
        towardsMidPoint = cohesionMmiddlePoint - transform.position;

        float weight = Mathf.Max(Vector3.Distance(transform.position, towardsMidPoint), steeringForce);
        return towardsMidPoint.normalized * weight;

    }

    private Vector3 ComputeAlignement(Collider[] influencingBoids)
    {
        if (!(hit.distance > criticalCollDist)) // A remplacer par un meilleur weighting system
            return Vector3.zero;

        // Alignement
        Vector3 averageBoidsDirection = influencingBoids.Select(col => col.transform.up)
        .Aggregate(Vector3.zero, (acc, dir) => acc + dir) / influencingBoids.Length; // Sum and average

        float weight = Mathf.Max((float)Math.Pow(Mathf.Max(hit.distance, 0.0000001f), 2), steeringForce);
        return averageBoidsDirection.normalized * weight;
    }

    private IEnumerator CastPhysicsRays()
    {
        yield return new WaitForSeconds(.1f); // For proper layer initialization
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
                    if (Physics.Raycast(frontRay, out hit, obstacleMask))
                    {
                        if (hit.collider.CompareTag("Wall")) // I have to double check, could not figure why the LayerMask in RayCast is not enough
                        {
                            Debug.DrawRay(transform.position, ray * rayMaxDistance, Color.white);
                            // Select the closest Vector to forward which does not collide with anything
                            awayFromObstacle = rays.First(ray => !Physics.Raycast(transform.position, ray, rayMaxDistance, obstacleMask));
                            Debug.Log(hit.collider.gameObject.name);
                            Debug.Log(Vector3.zero);
                            if (hit.distance < criticalCollDist)
                            {
                                awayFromObstacle = hit.normal;
                            }

                            yield return new WaitForSeconds(.1f);
                        }
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
                if (Physics.Raycast(transform.position, ray, rayMaxDistance, obstacleMask)) // color forward rays in Red if they hit an obstacle
                {
                    rayColor = Color.red;
                    Debug.DrawRay(transform.position, ray * rayMaxDistance, rayColor);
                }
            }
        }


    }

    void OnEnable()
    {
        TargetController.NewDestinationEvent += (dest) => { Debug.Log(dest); destination = dest; };
    }

    void OnDisable()
    {
        TargetController.NewDestinationEvent -= (dest) => { destination = dest; };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Debug.Log("Boid Died");
            BoidDeathEvent?.Invoke();
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
