using UnityEngine;
using System.Collections;

public class FirstPersonController : MonoBehaviour 
{
    [SerializeField]
    private float _runSpeed = 8.0f;
    [SerializeField]
    private float _jogSpeed = 5.0f;
    [SerializeField]
    private float _walkSpeed = 3.0f;

    [SerializeField]
    private float _runStrafeSpeed = 5.0f;
    [SerializeField]
    private float _jogStrafeSpeed = 3.0f;
    [SerializeField]
    private float _walkStrafeSpeed = 1.5f;
    [SerializeField]
    private float _jumpPower = 5.0f;  

    [SerializeField]
    private bool _walkByDefault = false;

    [SerializeField]
    private bool _lockCursor = true;

    [SerializeField]
    private AdvancedSettings advanced = new AdvancedSettings();     // The container for the advanced settings ( done this way so that the advanced setting are exposed under a foldout
    
    [System.Serializable]
    public class AdvancedSettings                                                       // The advanced settings
    {
        public float gravityMultiplier = 1f;                                            // Changes the way gravity effect the player ( realistic gravity can look bad for jumping in game )
        public PhysicMaterial zeroFrictionMaterial;                                     // Material used for zero friction simulation
        public PhysicMaterial highFrictionMaterial;                                     // Material used for high friction ( can stop character sliding down slopes )
        public float groundStickyEffect = 5f;											// power of 'stick to ground' effect - prevents bumping down slopes.
    }

    private CapsuleCollider _capsuleCollider;
    private const float _jumpRayLength = 0.7f;

    public bool Grounded { get; private set; }

    private Vector2 _input;
    private IComparer _rayHitComparer;

    void Awake()
    {
        // Setup references to components
        _capsuleCollider = collider as CapsuleCollider;
        Grounded = true;
        _rayHitComparer = new RayHitComparer();
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (Input.GetMouseButtonUp(0))
        {
            Screen.lockCursor = _lockCursor;
        }
	}

    public void FixedUpdate()
    {
        float speed = _jogSpeed;

        // Read input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool jump = Input.GetButton("Jump");

        bool walkOrJog = Input.GetKey(KeyCode.LeftShift);
        speed = _walkByDefault ? (walkOrJog ? _jogSpeed : _walkSpeed) : (walkOrJog ? _walkSpeed : _jogSpeed);

        _input = new Vector2(h, v);

        // normalize input if it exceeds 1 in combined length: This avoids being able to move faster diagonally.
        if (_input.sqrMagnitude > 1)
        {
            _input.Normalize();
        }

        // Determine if we are walking or jogging to determine current strafe speed.
        float strafeSpeed = walkOrJog ? _walkStrafeSpeed : _jogStrafeSpeed;
        Vector3 desiredMove = transform.forward * _input.y * speed + transform.right * _input.x * strafeSpeed;

        // preserving current y velocity (for falling, gravity)
        float yvel = rigidbody.velocity.y;

        // Add jump velocity
        if (Grounded && jump)
        {
            yvel += _jumpPower;
            Grounded = false;
        }

        // Set the rigidbody's velocity according to the ground angle and desired move
        rigidbody.velocity = desiredMove + Vector3.up * yvel;

        // Use low/high friction depending on whether we're moving or not
        if (desiredMove.magnitude > 0 || !Grounded)
        {
            collider.material = advanced.zeroFrictionMaterial;
        }
        else
        {
            collider.material = advanced.highFrictionMaterial;
        }

        // Ground Check:
        // Create a ray that points down from the centre of the character.
        Ray ray = new Ray(transform.position, -transform.up);

        // Raycast slightly further than the capsule (as determined by the _jumpRayCastLength)
        RaycastHit[] hits = Physics.RaycastAll(ray, _capsuleCollider.height * _jumpRayLength);
        System.Array.Sort(hits, _rayHitComparer);

        if (Grounded || rigidbody.velocity.y < _jumpPower * 0.5f)
        {
            // default value if nothing is hit
            Grounded = false;

            // Check every collider hit by the ray
            for (int i = 0; i < hits.Length; i++)
            {
                // Check it's not a trigger
                if (!hits[i].collider.isTrigger)
                {
                    // The character is grounded, and we store the ground angle (calculated from the normal)
                    Grounded = true;

                    // stick to surface - helps character stick to ground - specially when running down slopes.
                    // TODO: I think this line is responsible for some of the jitter while moving down slopes.
                    //if (rigidbody.velocity.y <= 0) {
                    rigidbody.position = Vector3.MoveTowards(rigidbody.position, hits[i].point + Vector3.up * _capsuleCollider.height * 0.5f, Time.deltaTime * advanced.groundStickyEffect);
                    //}
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
                    break;
                }
            }
        }

        // Draw a ray that reflects the current grounded check ray.
        Debug.DrawRay(ray.origin, ray.direction * _capsuleCollider.height * _jumpRayLength, Grounded ? Color.green : Color.red);

        // Add gravity
        Vector3 gravity = Physics.gravity;
        rigidbody.AddForce(gravity * (advanced.gravityMultiplier - 1));
    }

    void OnDisable()
    {
        Screen.lockCursor = false;
    }

    //used for comparing distances
    class RayHitComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((RaycastHit)x).distance.CompareTo(((RaycastHit)y).distance);
        }
    }
}
