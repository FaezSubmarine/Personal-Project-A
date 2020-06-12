using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public abstract class PlayerState
{
    public PlayerState(FirstPersonController fpc) { this.fpc = fpc; }

    protected FirstPersonController fpc;

    public abstract void Tick();
    public abstract void FixedTick();

    public virtual void OnStateEnter() { }
    public virtual void OnStateExit() { }

}

public class walkState : PlayerState
{
    private Vector2 m_Input;
    const float m_StickToGroundForce = 10;

    private float m_StepCycle;
    private float m_NextStep;
    float lerpCrouch;
    bool crouching;

    const float standingHeight = 1.8f;
    const float crouchSpeed = 3;
    const float crouchHeight = 0.7f;

    public walkState(FirstPersonController fpc): base(fpc)
    {
        //Debug.Log("walk");
    }
    public override void OnStateEnter()
    {
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;

    }
    public override void OnStateExit()
    {
        if (fpc.lerpLean != 0.5f)
        {
            fpc.StartCoroutine(normalLean());
        }
    }
    IEnumerator normalLean()
    {
        while (fpc.lerpLean != 0.5f)
        {
            Debug.Log(fpc.lerpLean);
            fpc.lerpLean = (fpc.lerpLean < 0.5f) ? Mathf.Min(fpc.lerpLean + Time.deltaTime, 0.5f) :
            Mathf.Max(fpc.lerpLean - Time.deltaTime, 0.5f);
            yield return null;
        }
    }
    float normalized(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }
    public override void Tick()
    {
        fpc.RotateView();
        // the jump state needs to read here to make sure it is not missed
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (crouching)
            {
                Ray ray = new Ray(fpc.m_Camera.transform.position- new Vector3(0,fpc.m_CharacterController.height/2,0), Vector3.up);
                Debug.DrawRay(ray.origin, ray.direction, Color.red);
                if (!Physics.SphereCast(ray,fpc.m_CharacterController.radius,standingHeight-crouchHeight,1<<0))
                {
                    crouching = false;
                    fpc.StartCoroutine(crouch());
                }
            }
            else
            {
                crouching = true;
                fpc.StartCoroutine(crouch());
            }

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            fpc.SetState(new JumpState(fpc, true));
        }
        if (Input.GetKey(KeyCode.Q))
        {
            fpc.lerpLean = fpc.checkForLeanCollision(true);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            fpc.lerpLean = fpc.checkForLeanCollision(false);
        }
        else
        {
            fpc.lerpLean = (fpc.lerpLean < 0.5f) ? Mathf.Min(fpc.lerpLean + Time.deltaTime, 0.5f) : 
                        Mathf.Max(fpc.lerpLean - Time.deltaTime, 0.5f);
        }
        if (fpc.checkWithOverlapCapsule(fpc.transform.position + fpc.transform.forward * 0.3f, 1 << 0, "Ladder")
&& Input.GetKeyDown(KeyCode.F))
        {
            fpc.SetState(new ClimbState(fpc));
        }
        Collider[] coll = Physics.OverlapSphere(fpc.transform.position + fpc.transform.forward * 0.3f, 0.3f, 1 << 0);
        if (coll.Length>0)
        {
            for(int i = 0; i < coll.Length; ++i)
            {
                Bounds bound = coll[i].bounds;
                float topY = bound.center.y + bound.extents.y;
                if (Mathf.Abs(topY - fpc.m_Camera.transform.position.y) < fpc.m_CharacterController.height / 2)
                {
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        fpc.SetState(new LedgeGrabState(fpc, coll[i]));
                        return;
                    }
                }
            }
        }
    }
    public override void FixedTick()
    {
        if (!fpc.m_CharacterController.isGrounded)
        {
            fpc.SetState(new JumpState(fpc, false));
            return;
        }
        float speed;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        bool waswalking = fpc.m_IsWalking;
        
        fpc.m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
        speed = fpc.m_IsWalking ? fpc.m_WalkSpeed : fpc.m_RunSpeed;
        speed *= crouching ? 0.5f : 1;

        fpc.m_AudioSource.volume = speed / fpc.m_RunSpeed;
        m_Input = new Vector2(horizontal, vertical);
        
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }
        
        if (fpc.m_IsWalking != waswalking && fpc.m_CharacterController.velocity.sqrMagnitude > 0)
        {
            fpc.StopAllCoroutines();
            fpc.StartCoroutine(!fpc.m_IsWalking ? fpc.m_FovKick.FOVKickUp() : fpc.m_FovKick.FOVKickDown());
        }
        Vector3 desiredMove = fpc.transform.forward * m_Input.y + fpc.transform.right * m_Input.x;
        
        RaycastHit hitInfo;
        Physics.SphereCast(fpc.transform.position, fpc.m_CharacterController.radius, Vector3.down, out hitInfo,
                           fpc.m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        fpc.moveDir.x = desiredMove.x * speed;
        fpc.moveDir.y = -m_StickToGroundForce;
        fpc.moveDir.z = desiredMove.z * speed;
        
        ProgressStepCycle(speed);
        fpc.UpdateCameraPosition(speed);

        fpc.m_MouseLook.UpdateCursorLock();
    }
    void ProgressStepCycle(float speed)
    {
        if (fpc.m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (fpc.m_CharacterController.velocity.magnitude + (speed * (fpc.m_IsWalking ? 1f : fpc.m_RunstepLenghten))) *
                         Time.fixedDeltaTime;
        }
        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + fpc.m_StepInterval;

        fpc.PlayFootStepAudio();
    }
    IEnumerator crouch()
    {
        //Debug.Log((lerpCrouch != 0 && !crouching) + " "+ (lerpCrouch != 1 && crouching));
        while ((lerpCrouch != 0 && !crouching) || (lerpCrouch != 1 && crouching)) {
            //lerpCrouch 1 is crouched, lerpcrouch 0 is standing
            lerpCrouch = crouching?Mathf.Min(lerpCrouch + Time.deltaTime*crouchSpeed, 1): 
                Mathf.Max(lerpCrouch - Time.deltaTime * crouchSpeed, 0);

            float finalHeight = Mathf.Lerp(standingHeight, crouchHeight, lerpCrouch);
            fpc.m_CharacterController.height = finalHeight;

            fpc.m_CharacterController.center = new Vector3(0, -1+(finalHeight / 2), 0);
            yield return null;
        }
    }
}

public class JumpState : PlayerState
{
    private Vector2 m_Input;
    const float m_StickToGroundForce = 10;
    bool jump;
    public JumpState(FirstPersonController fpc, bool jump) : base(fpc)
    {
        //Debug.Log("Jump");
        this.jump = jump;
    }
    public override void OnStateEnter()
    {
        fpc.moveDir.y = 0;
    }
    public override void OnStateExit()
    {
        fpc.StartCoroutine(fpc.m_JumpBob.DoBobCycle());
        fpc.PlayLandingSound();
        //m_MoveDir.y = 0f;
    }
    public override void Tick()
    {
        fpc.RotateView();

        if(fpc.m_CharacterController.isGrounded)
            fpc.SetState(new walkState(fpc));

        if (fpc.checkWithOverlapCapsule(fpc.transform.position + fpc.transform.forward * 0.3f, 1 << 0, "Ladder")
    && Input.GetKeyDown(KeyCode.F))
        {
            fpc.SetState(new ClimbState(fpc));
        }
        Collider[] coll = Physics.OverlapSphere(fpc.transform.position + fpc.transform.forward * 0.3f, 0.3f, 1 << 0);
        if (coll.Length > 0)
        {
            for (int i = 0; i < coll.Length; ++i)
            {
                Bounds bound = coll[i].bounds;
                float topY = bound.center.y + bound.extents.y;
                if (Mathf.Abs(topY - fpc.m_Camera.transform.position.y) < fpc.m_CharacterController.height / 2)
                {
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        //TODO: actually make a ledge hang class
                        fpc.SetState(new LedgeGrabState(fpc, coll[i]));
                        return;
                    }
                }
            }
        }

    }
    public override void FixedTick()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        m_Input = new Vector2(horizontal, vertical);

        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }
        Vector3 desiredMove = fpc.transform.forward * m_Input.y + fpc.transform.right * m_Input.x;
        fpc.moveDir.x = desiredMove.x * fpc.m_WalkSpeed;
        fpc.moveDir.z = desiredMove.z * fpc.m_WalkSpeed;
        if (jump)
        {
            fpc.moveDir.y = fpc.m_JumpSpeed;
            fpc.PlayJumpSound();
            jump = false;
        }
        else
            fpc.moveDir += Physics.gravity* fpc.m_GravityMultiplier * Time.fixedDeltaTime;

        fpc.m_MouseLook.UpdateCursorLock();
    }
}
public class LedgeGrabState : PlayerState
{
    Collider coll;
    public LedgeGrabState(FirstPersonController fpc,Collider coll) : base(fpc)
    {
        this.coll = coll;

    }
    public override void OnStateEnter()
    {
        Debug.Log("ledge grab");
        fpc.moveDir = Vector3.zero;
    }
    public override void OnStateExit()
    {

    }
    public override void Tick()
    {
        fpc.RotateView();
        if (Input.GetKeyUp(KeyCode.F))
        {
            fpc.SetState(new walkState(fpc));
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            fpc.SetState(new JumpState(fpc, true));
        }
        if (fpc.checkWithOverlapCapsule(fpc.transform.position, 1 << 0, "Ladder"))
        {
            fpc.SetState(new ClimbState(fpc));
        }
        Bounds bound = coll.bounds;
        float topY = bound.center.y + bound.extents.y;
        if (Mathf.Abs(topY - fpc.m_Camera.transform.position.y) > fpc.m_CharacterController.height)
        {
            fpc.SetState(new walkState(fpc));
        }
    }
    public override void FixedTick()
    {
        fpc.moveDir.y += Time.fixedDeltaTime*5;
    }


}
public class ClimbState: PlayerState
{
    public ClimbState(FirstPersonController fpc) : base(fpc)
    {

    }
    public override void OnStateEnter()
    {
        fpc.moveDir = Vector3.zero;
    }
    public override void OnStateExit()
    {

    }
    public override void Tick()
    {
        fpc.RotateView();
        if (Input.GetKeyDown(KeyCode.F))
        {
            fpc.SetState(new walkState(fpc));
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            fpc.SetState(new JumpState(fpc, true));
        }
        if (!fpc.checkWithOverlapCapsule(fpc.transform.position, 1 << 0, "Ladder"))
        {
            fpc.SetState(new walkState(fpc));
        }
    }
    public override void FixedTick()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 m_Input = new Vector2(horizontal, vertical);

        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }
        fpc.moveDir = fpc.transform.right * m_Input.x;
        fpc.moveDir.y = m_Input.y;

        fpc.UpdateCameraPosition(fpc.m_WalkSpeed);
        fpc.m_MouseLook.UpdateCursorLock();
    }


}