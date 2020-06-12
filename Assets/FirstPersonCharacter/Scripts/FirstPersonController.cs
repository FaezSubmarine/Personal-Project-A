using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    public bool m_IsWalking;
    public float m_WalkSpeed;
    public float m_RunSpeed;
    [SerializeField] [Range(0f, 1f)] public float m_RunstepLenghten;
    public float m_JumpSpeed;
    public float m_GravityMultiplier;
    public MouseLook m_MouseLook;
    public FOVKick m_FovKick = new FOVKick();
    [SerializeField] private bool m_UseHeadBob;
    public CurveControlledBob m_HeadBob = new CurveControlledBob();
    public LerpControlledBob m_JumpBob = new LerpControlledBob();
    public float m_StepInterval;
    [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
    [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
    [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

    [HideInInspector]public Camera m_Camera;
    private bool m_Jump;
    private float m_YRotation;
    private Vector2 m_Input;
    private Vector3 m_MoveDir = Vector3.zero;
    public CharacterController m_CharacterController { get; private set; }
    [HideInInspector] public CollisionFlags m_CollisionFlags;

    Vector3 dist = new Vector3(1, 0, 0);
    Vector3 neutral;
    public float lerpLean = 0.5f;

    public AudioSource m_AudioSource { get; private set; }

    public PlayerState currentState;

    [HideInInspector] public Vector3 moveDir;

    [HideInInspector]public List<LightDetectionBase> listOfLightDetection = new List<LightDetectionBase>();

    [SerializeField] float soundRadius = 100;

    [SerializeField] MaterialWithFloat wall;
    [SerializeField] FloorNoise floorNoise;

    [SerializeField] ScriptableFloat hp;
    [SerializeField] ScriptableFloat shadow;
    public float finalLightMulti()
    {
        float finalFloat = 0.1f;
        foreach(LightDetectionBase eachLight in listOfLightDetection)
        {
            finalFloat = eachLight.calculateLightLevel();
            if (finalFloat >= 2)
            {
                finalFloat = 2;
                return finalFloat;
            }
        }
        return finalFloat;
    }
    public void SetState(PlayerState state)
    {
        if (currentState != null)
            currentState.OnStateExit();

        currentState = state;
        if (currentState != null)
            currentState.OnStateEnter();
    }
    // Use this for initialization
    private void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera,m_CharacterController, m_StepInterval);
            
        m_AudioSource = GetComponent<AudioSource>();
		m_MouseLook.Init(transform , m_Camera.transform);
        neutral = new Vector3(0,m_CharacterController.center.y+ m_CharacterController.height / 2 -0.1f, 0);


        SetState(new walkState(this));
    }

    public void PlayLandingSound()
    {
        m_AudioSource.clip = m_LandSound;
        m_AudioSource.Play();
    }
    // Update is called once per frame
    private void Update()
    {
        shadow.point = finalLightMulti();
        if (Input.GetKeyDown(KeyCode.Semicolon))
        {
            hp.point -= 1;
        }

        currentState.Tick();
    }
    public bool checkWithOverlapCapsule(Vector3 pos, int targetLayer,string tag = "",float radius = 0)
    {
        Vector3 top = pos + new Vector3(0, m_CharacterController.bounds.extents.y, 0);
        Vector3 bottom = pos - new Vector3(0, m_CharacterController.bounds.extents.y, 0);
        Collider[] coll = Physics.OverlapCapsule(top, bottom,
            m_CharacterController.radius + radius, 1 << 0);
        if (coll.Length > 0)
        {
            foreach (Collider singleColl in coll)
            {
                if (tag != "")
                {
                    if (singleColl.CompareTag(tag))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }

            }
        }
        return false;
    }
    private void FixedUpdate()
    {
        currentState.FixedTick();
        m_CollisionFlags = m_CharacterController.Move(moveDir * Time.fixedDeltaTime);

    }

    public void PlayJumpSound()
    {
        m_AudioSource.clip = m_JumpSound;
        m_AudioSource.Play();
        relaySoundInfo(2);
    }

    public void PlayFootStepAudio()
    {
        //int n = Random.Range(1, m_FootstepSounds.Length);
        //m_AudioSource.clip = m_FootstepSounds[n];
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray,out hit,m_CharacterController.height,1<<0))
        {
            m_AudioSource.PlayOneShot(floorNoise.getClip(hit.transform.GetComponent<Renderer>().sharedMaterial));
        }

        relaySoundInfo();
    }
    void relaySoundInfo(float amplifier = 1)
    {
        float maxDist = m_AudioSource.volume * soundRadius;
        Collider[] coll = Physics.OverlapSphere(transform.position,m_AudioSource.volume*soundRadius,1<<10);
        if (coll.Length > 0)
        {
            foreach (Collider singleColl in coll)
            {
                Ray ray = new Ray(transform.position,singleColl.transform.position-transform.position);
                RaycastHit[] hitArray = Physics.RaycastAll(transform.position, singleColl.transform.position - transform.position, soundRadius,1<<0);
                float muffler = 0;
                if (hitArray.Length > 0)
                {
                    foreach (RaycastHit hit in hitArray)
                    {
                        if(hit.transform.TryGetComponent(out Renderer rend))
                            muffler += wall.FindSTC(rend.sharedMaterial);
                    }
                }
                float result = (-Vector3.SqrMagnitude(singleColl.transform.position - transform.position) / (m_AudioSource.volume * amplifier*maxDist)) + maxDist+muffler;
                singleColl.transform.GetComponent<BaseAI>().respondToSound(transform.position,result);
            }
        }
    }
    float lerpTime = 0;
    Vector3 oldPos;
    public void UpdateCameraPosition(float speed)
    {
        leanForPos();
        Vector3 newCameraPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.sqrMagnitude>0 && m_CharacterController.isGrounded)
        {
            m_Camera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                    (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();

            m_Camera.transform.localPosition = newCameraPosition;
            oldPos = m_Camera.transform.localPosition;
            lerpTime = 0;
        }
        else
        {
            newCameraPosition.x = 0;
            newCameraPosition.y = m_CharacterController.center.y + m_CharacterController.height / 2 - 0.1f - m_JumpBob.Offset();
            newCameraPosition.z = 0;

            lerpTime += Time.deltaTime*6;
            lerpTime = Mathf.Min(lerpTime, 1);
            m_Camera.transform.localPosition = Vector3.Lerp(oldPos,newCameraPosition,lerpTime);
        }
    }


    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        float vertical = CrossPlatformInputManager.GetAxis("Vertical");

        bool waswalking = m_IsWalking;
        m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
        // set the desired speed to be walking or running
        speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
        m_Input = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        if (m_IsWalking != waswalking && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }
    }
    void leanForPos()
    {
        m_Camera.transform.localPosition = Vector3.Lerp(neutral - dist,
    neutral + dist, lerpLean);
    }
    void leanForAngle()
    {
        float angle = Mathf.LerpAngle(3, -3, lerpLean);
        m_Camera.transform.localEulerAngles = new Vector3(0, 0, angle);
    }
    public float checkForLeanCollision(bool qPressed)
    {
        Vector3 ghostPos = Vector3.Lerp(m_Camera.transform.TransformDirection(-dist), m_Camera.transform.TransformDirection(dist), lerpLean + (qPressed ? -1 : 1) * Time.deltaTime);
        Collider[] coll = Physics.OverlapSphere(m_Camera.transform.position + ghostPos, 0.65f, 1 << 0);
        if (coll.Length == 0)
        {
            return qPressed ? Mathf.Max(lerpLean - Time.deltaTime, 0) : Mathf.Min(lerpLean + Time.deltaTime, 1);
        }
        else
        {
            Collider[] coll2 = Physics.OverlapSphere(m_Camera.transform.position + ghostPos, 0.55f, 1 << 0);
            if (coll2.Length > 0)
            {
                const float multi = 10f;
                return qPressed ? Mathf.Min(lerpLean + Time.deltaTime * multi, 0.5f) : Mathf.Max(lerpLean - Time.deltaTime * multi, 0.5f);
            }
        }

        return lerpLean;
    }
    public void RotateView()
    {
        leanForAngle();

        m_MouseLook.LookRotation (transform, m_Camera.transform);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }
        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }
}
