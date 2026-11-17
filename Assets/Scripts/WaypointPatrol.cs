using UnityEngine;
using UnityEngine.AI;

// Охоронець по черзі обходить точки маршруту — на землі та на платформах.
[RequireComponent(typeof(NavMeshAgent))]
public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0.1f)] private float reachDistance = 0.6f;

    private NavMeshAgent agent;
    private int currentIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GoToCurrent();
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || agent.pathPending || agent.isOnOffMeshLink)
            return;

        if (agent.remainingDistance <= reachDistance)
        {
            Debug.Log($"{name} досяг точки {waypoints[currentIndex].name}");
            currentIndex = (currentIndex + 1) % waypoints.Length;
            GoToCurrent();
        }
    }

    private void GoToCurrent()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentIndex] == null)
            return;

        agent.SetDestination(waypoints[currentIndex].position);
    }
}
