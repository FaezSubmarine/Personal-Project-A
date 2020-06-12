using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardAction
{
    BaseAI baseAI;
    
    public GuardAction()
    {

    }
    public GuardAction(BaseAI baseAI)
    {
        this.baseAI = baseAI;
    }
    public void chaseToKillIK(int layerIndex)
    {
        Debug.Log("chase ik");
        float angle = Vector3.Angle(baseAI.transform.forward, (Vector3)baseAI.followTarget - baseAI.transform.position);
        float headAngle = Vector3.Angle(baseAI.head.forward, (Vector3)baseAI.followTarget - baseAI.transform.position);


        Debug.DrawRay(baseAI.head.transform.position, (Vector3)baseAI.followTarget - baseAI.head.transform.position, Color.magenta);

        if (headAngle > baseAI.visionHalfAngle / 4)
        {
            if (!baseAI.headRotLerp.turnt)
            {
                baseAI.headRotLerp.originalRot = baseAI.head.rotation;
                baseAI.headRotLerp.setTurnt(true);
            }
            else
            {
                baseAI.anim.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Inverse(baseAI.head.parent.rotation) *
                    baseAI.headRotLerp.lerpResult(Quaternion.LookRotation((Vector3)baseAI.followTarget - baseAI.head.position)));
            }
        }
        else
        {
            baseAI.headRotLerp.timing = 0;
            baseAI.headRotLerp.setTurnt(false);
        }

        if (angle > baseAI.visionHalfAngle / 2)
        {
            if (!baseAI.rotLerp.turnt)
            {
                baseAI.rotLerp.originalRot = baseAI.transform.rotation;
                baseAI.rotLerp.setTurnt(true);
            }
            else
            {
                float yBodyRot = Quaternion.LookRotation((Vector3)baseAI.followTarget - baseAI.transform.position).eulerAngles.y;
                baseAI.transform.rotation = baseAI.rotLerp.lerpResult(Quaternion.Euler(baseAI.transform.eulerAngles.x, yBodyRot, baseAI.transform.eulerAngles.z));
            }
        }
        else
        {
            baseAI.rotLerp.timing = 0;
            baseAI.rotLerp.setTurnt(false);
        }
    }
    public NodeStates ChaseToKill()
    {
        Debug.Log("chase ");
        if (baseAI.searchPatrol)
            Object.Destroy(baseAI.searchPatrol);

        baseAI.searchPatrolInt = 0;
        if (baseAI.visionSubject)
            baseAI.followTarget = baseAI.visionSubject.position;

        if (baseAI.fpc == null && baseAI.visionSubject)
        {
            baseAI.fpc = baseAI.visionSubject.GetComponent<FirstPersonController>();
        }
        if (baseAI.visionSubject == null)
        {
            baseAI.fpc = null;
        }

        baseAI.detectionLevel += Time.deltaTime * ((-Vector3.SqrMagnitude((Vector3)baseAI.followTarget - baseAI.transform.position) / baseAI.visionRange * baseAI.fpc.finalLightMulti()) + baseAI.visionRange);
        baseAI.detectionLevel = Mathf.Min(baseAI.detectionLevel, baseAI.DangerThreshold /*+ baseAI.debugFloat*/);

        if (baseAI.detectionLevel < baseAI.suspiciousThreshold)
        {
            
            Debug.Log(baseAI.transform.name+ " agent speed " + baseAI.agent.speed);
            return NodeStates.FAILURE;
        }
        Debug.DrawRay(baseAI.head.position, baseAI.head.forward);
        if (baseAI.currentIK != chaseToKillIK)
        {
            baseAI.currentIK = chaseToKillIK;
        }
        if (baseAI.detectionLevel < baseAI.DangerThreshold)
        {
            return NodeStates.RUNNING;
        }
        if (!baseAI.searchPatrol)
        {
            Object.Destroy(baseAI.searchPatrol);
        }
        baseAI.agent.isStopped = baseAI.agent.remainingDistance<3;

        baseAI.agent.SetDestination((Vector3)baseAI.followTarget);

        baseAI.agent.speed = baseAI.runSpeed;
        return NodeStates.RUNNING;
    }
}
