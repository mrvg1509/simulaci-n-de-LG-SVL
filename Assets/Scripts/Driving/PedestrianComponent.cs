/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum PedestrainState
{
    None,
    Idle,
    Walking,
    Crossing
};

public class PedestrianComponent : MonoBehaviour
{
    public enum ControlType
    {
        Automatic,
        Waypoints,
        Manual,
    }

    [HideInInspector]
    public ControlType Control = ControlType.Automatic;

    List<Vector3> targets;
    List<float> idle;
    bool waypointLoop;

    private int currentTargetIndex = 0;
    public float idleTime = 0f;
    public float targetRange = 1f;

    private Vector3 currentTargetPos;
    private Transform currentTargetT;
    private NavMeshAgent agent;
    private Animator anim;
    private PedestrainState thisPedState = PedestrainState.None;
    private bool isInit = false;

    public void WalkRandomly(bool enable)
    {
        if (!enable)
        {
            Control = ControlType.Manual;
            thisPedState = PedestrainState.None;
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            return;
        }

        agent.avoidancePriority = Random.Range(1, 100);

        var position = agent.transform.position;
        MapPedestrianSegmentBuilder closest = null;
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        foreach (var seg in PedestrianManager.Instance.pedSegments)
        {
            for (int i = 0; i < seg.segment.targetWorldPositions.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(position - seg.segment.targetWorldPositions[i]);
                if (distance < closestDistance)
                {
                    closest = seg;
                    closestIndex = i;
                    closestDistance = distance;
                }
            }
        }
        targets = closest.segment.targetWorldPositions;

        currentTargetIndex = closestIndex;
        int prevTargetIndex = closestIndex == 0 ? targets.Count - 1 : closestIndex - 1;

        currentTargetPos = GetRandomTargetPosition(currentTargetIndex);

        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
        Control = ControlType.Automatic;
    }

    public void FollowWaypoints(List<Api.WalkWaypoint> waypoints, bool loop)
    {
        Control = ControlType.Automatic;

        agent.avoidancePriority = 0;

        targets = waypoints.Select(wp => wp.Position).ToList();
        idle = waypoints.Select(wp => wp.Idle).ToList();

        currentTargetIndex = 0;
        currentTargetPos = targets[0];

        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
        Control = ControlType.Waypoints;
        waypointLoop = loop;
    }

    public void InitManual(Vector3 position, Quaternion rotation)
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.avoidancePriority = 0;

        agent.Warp(position);
        agent.transform.rotation = rotation;

        thisPedState = PedestrainState.None;
        isInit = true;

        Control = ControlType.Manual;
    }

    public void InitPed(List<Vector3> pedSpawnerTargets)
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        targets = pedSpawnerTargets;

        if ((int)Random.Range(0, 2) == 0)
            targets.Reverse();

        agent.avoidancePriority = (int)Random.Range(1, 100); // set to 0 for no avoidance

        // get random pos index
        currentTargetIndex = Random.Range(0, targets.Count);
        int prevTargetIndex = currentTargetIndex == 0 ? targets.Count - 1 : currentTargetIndex - 1;
        
        agent.Warp(GetRandomTargetPosition(prevTargetIndex));

        currentTargetPos = GetRandomTargetPosition(currentTargetIndex);

        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
        isInit = true;
    }

    private void OnEnable()
    {
        if (!isInit) return;

        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
    }

    private void Update()
    {
        if (Time.deltaTime == 0.0f)
        {
            return;
        }

        if (Control == ControlType.Automatic)
        {
            if (IsRandomIdle())
                StartCoroutine(ChangePedState());

            if (IsPedAtDestination())
                SetPedNextAction();

        }
        else if (Control == ControlType.Waypoints)
        {
            if (IsPedAtDestination())
            {
                Api.ApiManager.Instance.AddWaypointReached(gameObject, currentTargetIndex);

                if (currentTargetIndex == targets.Count - 1 && !waypointLoop)
                {
                    WalkRandomly(false);
                }
                else
                {
                    StartCoroutine(IdleAnimation(idle[currentTargetIndex]));
                    SetPedNextAction();
                }
            }
        }

        SetAnimationControllerParameters();
    }

    public void SetPedDestination(Transform target) // demo pedestrian control
    {
        if (agent == null || target == null) return;

        currentTargetT = target;
        currentTargetPos = target.position;
        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
    }

    public void SetPedAnimation(string triggerName)
    {
        if (anim == null) return;

        AnimatorControllerParameter[] tempACP = anim.parameters;
        for (int i = 0; i < tempACP.Length; i++)
        {
            if (triggerName == tempACP[i].name)
                anim.SetTrigger(triggerName);
        }
    }

    private void SetPedNextAction()
    {
        if (!agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            currentTargetPos = GetNextTarget(); //GetRandomTargetPosition();
            agent.SetDestination(currentTargetPos);
            thisPedState = PedestrainState.Walking;
        }
    }

    private IEnumerator LookAtTarget() // demo pedestrian control
    {
        if (agent == null) yield break;

        Quaternion tempQ = agent.transform.rotation;
        float elapsedTime = 0f;
        float speed = 0.5f;
        while (elapsedTime < 0.5f)
        {
            agent.transform.rotation = Quaternion.Lerp(tempQ, currentTargetT.rotation, (elapsedTime/speed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ChangePedState()
    {
        return IdleAnimation(Random.Range(idleTime * 0.5f, idleTime));
    }

    private IEnumerator IdleAnimation(float duration)
    {
        if (agent == null) yield break;

        thisPedState = PedestrainState.Idle;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        agent.isStopped = false;
        thisPedState = PedestrainState.Walking;
    }

    private bool IsPedAtDestination()
    {
        if (!agent.pathPending && !agent.hasPath && thisPedState == PedestrainState.Walking)
            return true;
        return false;
    }

    private bool IsRandomIdle()
    {
        if ((int)Random.Range(0, 1000) < 1 && thisPedState == PedestrainState.Walking)
            return true;
        return false;
    }

    private void SetAnimationControllerParameters()
    {
        if (agent == null || anim == null) return;

        Vector3 s = agent.velocity.normalized; //agent.transform.InverseTransformDirection(agent.velocity).normalized;
        anim.SetFloat("speed", Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)));
        //anim.SetFloat("turn", s.z); TODO blend better animation
    }

    private Vector3 GetNextTarget()
    {
        currentTargetIndex = currentTargetIndex == targets.Count - 1 ? 0 : currentTargetIndex + 1;
        return targets[currentTargetIndex];
    }

    private Vector3 GetRandomTargetPosition(int index)
    {
        Vector3 tempV = targets[index];

        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh || count > 10000)
        {
            Vector3 randomPoint = tempV + Random.insideUnitSphere * targetRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                tempV = hit.position;
                isInNavMesh = true;
            }
            count++;
        }

        return tempV;
    }
}
