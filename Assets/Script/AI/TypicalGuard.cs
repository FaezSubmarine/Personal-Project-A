using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class TypicalGuard : BaseAI
{
    Node detectAndChaseNode;
    Node searchSequence;

    GuardAction guardAction;
    [SerializeField]float debugFloat = -10;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.SetDestination(waypoint.GetChild(waypointNum).position);
        anim = GetComponent<Animator>();

        head = anim.GetBoneTransform(HumanBodyBones.Head);
        rotLerp = new QuaternionLerper();
        headRotLerp = new QuaternionLerper();
        guardAction = new GuardAction(this);
    }
    void Start()
    {
        normalPatrolNode = new ActionNode(normalPatrol);
        detectAndChaseNode = new VisionNode(new ActionNode(guardAction.ChaseToKill), this);
        searchSequence = new Sequence(new List<Node> { new ActionNode(lastSeenLocation), new ActionNode(lookingAround), new ActionNode(searchingAround) });

        rootNode = new Selector(new List<Node> { detectAndChaseNode,searchSequence,normalPatrolNode });
    }

    // Update is called once per frame
    void Update()
    {
        parallelVision();
        anim.SetBool("walk", Vector3.SqrMagnitude(agent.velocity)>0);
        rootNode.Evaluate();
    }
    private void OnAnimatorIK(int layerIndex)
    {
        currentIK?.Invoke(layerIndex);
    }
}
