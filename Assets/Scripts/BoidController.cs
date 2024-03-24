using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoidController : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10.0f;
    [SerializeField] private float steeringForce = 8.0f;
    [SerializeField] private int nbRays = 1200;
    [SerializeField] private float rayMaxDistance = 4.0f;
    [SerializeField] private float dodgeAngle = 40.0f;
    [SerializeField] private float destinationValidationRadius = 5.0f;
    [SerializeField] private float destinationChoosingRange = 100.0f;
    [SerializeField] private bool debug = false;

    private Vector3 destination;
    private Vector3 position;
    private Vector3 velocity;
    [SerializeField] private Vector3 desiredDirection;
    private List<Vector3> rays;
    private List<Vector3> sortedRays;

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
        Vector3 randomSteerForce = Vector3.zero; // TODO: Implement steerforce

        desiredDirection = destination - transform.position;
        Vector3 desiredVelocity = (desiredDirection + randomSteerForce).normalized * maxSpeed;
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
                if (Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacle")))
                {
                    // Select the closest Vector to forward which does not collide with anything
                    // TODO : increase steering force according to how close to the obstacle the boid is
                    destination = sortedRays.First(ray => !Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacle")));
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
                if (Physics.Raycast(transform.position, ray, rayMaxDistance, LayerMask.GetMask("Obstacle"))) // color forward rays in Red if they hit an obstacle
                {
                    rayColor = Color.red;
                }
            }
            Debug.DrawRay(transform.position, ray * rayMaxDistance, rayColor);
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

            rays.Add(new Vector3((float)x, (float)y, (float)z));
        }
        return rays;
    }

    void FixedUpdate()
    {
        // separation: steer to avoid crowding local flockmates

        // alignment: steer towards the average heading of local flockmates

        // cohesion: steer to move towards the average position (center of mass) of local flockmates

    }
}
