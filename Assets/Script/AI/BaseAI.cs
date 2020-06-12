using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class QuaternionLerper
{
    public bool turnt { get; private set; }
    public float timing = 0;
    public Quaternion originalRot;
    public Quaternion lerpResult(Quaternion targetRot, float speed = 1)
    {
        Quaternion result = Quaternion.Lerp(originalRot, targetRot, timing);
        timing = Mathf.Min(timing + Time.deltaTime * speed, 1);
        return result;
    }
    public void setTurnt(bool newTurnt)
    {
        timing = 0;
        turnt = newTurnt;
    }
}

public class BaseAI : MonoBehaviour
{
    public float visionRange = 1;
    public float visionHalfAngle = 50;  //Prev was 30
    public Transform head { get; protected set; }
    [HideInInspector]public Transform visionSubject;

    [HideInInspector] public float detectionLevel;
    protected float maxDetectionLevel = 100;

    [SerializeField]protected float detectionVisionRaise = 5f;
    protected float detectionSoundRaise = 20f;

    public float suspiciousThreshold = 50f;
    public float DangerThreshold = 90f;

    protected float detectionDecay = 4f;

    protected Node rootNode;
    protected Node normalPatrolNode;
    [SerializeField] protected Transform waypoint;
    protected int waypointNum = 0;
    protected int oldWaypointNum = 0;
    public NavMeshAgent agent { get; protected set; }
    public Vector3? followTarget;
    [HideInInspector]public FirstPersonController fpc;
    public GameObject searchPatrol { get; protected set; }
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    [HideInInspector]public int searchPatrolInt;
    
    Transform currentFloor;
    protected Vector3 oldHit;
    Vector3 oldDir;

    Vector3 newHeadDir;
    
    bool yReset;

    public Animator anim { get; protected set; }

    public delegate void CurrentIK(int layerIndex);

    public CurrentIK currentIK;

    public QuaternionLerper rotLerp { get; protected set; }
    public QuaternionLerper headRotLerp { get; protected set; }

    protected AudioSource audioSource;
    [SerializeField] protected FloorNoise floorNoise;
    [SerializeField] protected MaterialWithFloat wallSTC;

    float lerpFloat = 0.5f;
    float pingPongCounter = 0.5f;

    protected void patrolIK(int layerIndex)
    {
        Debug.Log("patrol ik ");
        Vector3 vel = agent.velocity;
        vel.y = 0;
        if (!rotLerp.turnt)
        {
            Debug.Log("turnt true");
            rotLerp.originalRot = transform.rotation;
            rotLerp.setTurnt(true);
        }
        else
        {
            //TODO: MAYBE FACE ACCORDING TO VELOCITY???

            Debug.DrawRay(transform.position, agent.velocity*10);

            transform.rotation = rotLerp.lerpResult(Quaternion.LookRotation(vel));
            //if(transform.rotation == Quaternion.LookRotation(vel))
            //{
            //    rotLerp.setTurnt(false);
            //}
        }
        if (!headRotLerp.turnt)
        {
            headRotLerp.originalRot = head.rotation;
            headRotLerp.setTurnt(true);
        }
        else
        {
            Debug.DrawRay(head.position, transform.right, Color.green);

            anim.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Inverse(head.parent.rotation) *
                    headRotLerp.lerpResult(Quaternion.LookRotation(newHeadDir),5));
        }
    }
    public void makeFootStepNoise()
    {
        Collider[] soundSphere = Physics.OverlapSphere(transform.position, audioSource.maxDistance, 1 << 8);
        Ray ray = new Ray();
        RaycastHit hit = new RaycastHit();
        if (soundSphere.Length > 0)
        {
            ray = new Ray(transform.position, soundSphere[0].transform.position-transform.position);
            float finalVolumeDecibel = 0;

            RaycastHit[] hitArray = Physics.RaycastAll(ray, Vector3.Magnitude(ray.direction),1<<0);
            if (hitArray.Length > 0)
            {
                foreach(RaycastHit eachHit in hitArray)
                {
                    finalVolumeDecibel -= wallSTC.FindSTC((eachHit.transform.GetComponent<Renderer>().sharedMaterial));
                    Debug.Log("wallSTC " + wallSTC.normalizedDecibel(finalVolumeDecibel));
                    if (wallSTC.normalizedDecibel(finalVolumeDecibel) <=0)
                    {
                        return;
                    }
                }
            }

            ray = new Ray(transform.position + Vector3.up * 2, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * (transform.localScale.y + 2), Color.magenta);

            hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, transform.localScale.y + 2, 1 << 0))
            {
                //Debug.Log("hit " + hit.transform + " material " + hit.transform.GetComponent<Renderer>().sharedMaterial);
                audioSource.volume = wallSTC.normalizedDecibel(finalVolumeDecibel);
                audioSource.PlayOneShot(floorNoise.getClip(hit.transform.GetComponent<Renderer>().sharedMaterial));
            }
        }

        //audioSource.PlayOneShot();
    }
    protected NodeStates normalPatrol()
    {
        if(currentIK != patrolIK)
        {
            rotLerp.setTurnt(false);
            headRotLerp.setTurnt(false);
            currentIK = patrolIK;
        }
        agent.isStopped = false;
        agent.SetDestination(waypoint.GetChild(waypointNum).position);
        if(fpc == null)
        {
            detectionLevel = Mathf.Max(detectionLevel - detectionDecay*Time.deltaTime, 0);
        }
        if (agent.remainingDistance < 0.2f)
        {
            waypointNum = (int)Mathf.Repeat(waypointNum + 1, waypoint.childCount);
            agent.SetDestination(waypoint.GetChild(waypointNum).position);
            rotLerp.setTurnt(false);
            headRotLerp.setTurnt(false);
            return NodeStates.RUNNING;
        }
        return NodeStates.RUNNING;
    }
    public void respondToSound(Vector3 pos,float volume)
    {
        followTarget = pos;
        detectionLevel += volume;
        detectionLevel = Mathf.Clamp(detectionLevel,0,DangerThreshold);
        //Debug.Log("follow target " + followTarget+" "+detectionLevel);

    }
    public bool VisionCheck(int targetLayer, int ignoreLayer, out Transform hit)
    {
        //Debug.DrawRay(head.position, head.forward*10, Color.blue);
        Collider[] coll = Physics.OverlapSphere(transform.position,visionRange,targetLayer);
        if (coll.Length > 0)
        {
            foreach (Collider singleColl in coll)
            {
                if (Vector3.SqrMagnitude(singleColl.transform.position - head.position) < 1)
                {
                    hit = singleColl.transform;
                    return true;
                }
                RaycastHit raycastHit = new RaycastHit();
                Ray ray = new Ray(head.position, singleColl.transform.position - head.position);
                //Debug.DrawRay(ray.origin, ray.direction,Color.cyan);
                Physics.Raycast(ray, out raycastHit, visionRange, ~ignoreLayer);

                if (raycastHit.transform == singleColl.transform)
                {
                    if (Vector3.Angle(head.forward,
                        raycastHit.transform.position - head.position)
                        < visionHalfAngle)
                    {
                        hit = raycastHit.transform;
                        return true;
                    }
                }
            }
        }
        hit = null;
        return false;
    }
    protected void parallelVision()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position+new Vector3(0,1), Vector3.down);
        if (Physics.Raycast(ray,out hit,transform.localScale.y+2,1<<0))
        {
            if (currentFloor == null || currentFloor != hit.transform)
            {
                currentFloor = hit.transform;
                //headRot = null;
            }
            if (!oldHit.Equals(hit.normal))
            {
                headRotLerp.setTurnt(false);
                oldHit = hit.normal;
            }
            newHeadDir = Vector3.Cross(transform.right, hit.normal);
        }
    }

    protected NodeStates conversationMaker()
    {
        //todo: Make conversation!
        return NodeStates.FAILURE;
    }
    protected NodeStates lookingAround()
    {
        Debug.Log("looking around");

        if (searchPatrol)
        {
            return NodeStates.SUCCESS;
        }
        Quaternion minRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y - 30, transform.eulerAngles.z);
        Quaternion maxRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + 30, transform.eulerAngles.z);
        pingPongCounter += Time.deltaTime;
        lerpFloat = Mathf.PingPong(pingPongCounter, 1);
        head.rotation = Quaternion.Lerp(minRot, maxRot, lerpFloat);
        if (pingPongCounter > 3f)
        {
            searchPatrol = new GameObject();
            for (int i = 0; i < 5; ++i)
            {
                NavMeshHit hit;
                Vector3 randomPoint;
                do
                {
                    randomPoint = transform.position + Random.insideUnitSphere * 20;
                } while (!NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas));
                GameObject searchWaypoint = Instantiate(waypoint.GetChild(0), hit.position, Quaternion.identity).gameObject;
                searchWaypoint.transform.parent = searchPatrol.transform;
            }
            pingPongCounter = 0.5f;
            lerpFloat = 0.5f;
        }
        return NodeStates.RUNNING;
    }
    protected NodeStates searchingAround()
    {
        if (!searchPatrol)
        {
            return NodeStates.FAILURE;
        }
        agent.speed = walkSpeed;

        if (currentIK != patrolIK)
        {
            rotLerp.setTurnt(false);
            headRotLerp.setTurnt(false);
            currentIK = patrolIK;
        }
        Debug.Log("searching around");
        agent.isStopped = false;
        agent.SetDestination(searchPatrol.transform.GetChild(searchPatrolInt).position);
        if (agent.remainingDistance < 0.2f)
        {
            ++searchPatrolInt;
            if (searchPatrolInt == searchPatrol.transform.childCount)
            {
                Destroy(searchPatrol);
                searchPatrolInt = 0;
                detectionLevel = 0;
                followTarget = null;
                return NodeStates.SUCCESS;
            }
            agent.SetDestination(waypoint.GetChild(searchPatrolInt).position);
            return NodeStates.RUNNING;
        }
        return NodeStates.RUNNING;
    }
    protected NodeStates lastSeenLocation()
    {
        if (searchPatrol)
        {
            return NodeStates.SUCCESS;
        }
        if (followTarget == null || detectionLevel < DangerThreshold)
        {
            return NodeStates.FAILURE;
        }

        agent.SetDestination((Vector3)followTarget);
        agent.isStopped = false;
        if (agent.remainingDistance < 1)
        {
            //agent.isStopped = true;
            return NodeStates.SUCCESS;
        }
        return NodeStates.RUNNING;
    }
}
