using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Замість телепорту по NavMesh Link агент стрибає по параболі.
[RequireComponent(typeof(NavMeshAgent))]
public class AgentJumper : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float jumpDuration = 0.7f;
    [SerializeField, Min(0f)] private float extraHeight = 1.5f;

    private NavMeshAgent agent;
    private bool isJumping;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
    }

    private void Update()
    {
        if (agent.isOnOffMeshLink && !isJumping)
            StartCoroutine(Jump());
    }

    private IEnumerator Jump()
    {
        isJumping = true;

        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        Vector3 end = link.endPos + Vector3.up * agent.baseOffset;
        float height = Mathf.Abs(end.y - start.y) * 0.5f + extraHeight;

        Vector3 flatDirection = end - start;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDirection);

        Debug.Log($"{name} стрибає {(end.y > start.y ? "вгору" : "вниз")} на висоту {end.y:F1}");

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime / jumpDuration;
            float arc = 4f * height * time * (1f - time);
            transform.position = Vector3.Lerp(start, end, time) + Vector3.up * arc;
            yield return null;
        }

        transform.position = end;
        agent.CompleteOffMeshLink();
        isJumping = false;
    }
}
