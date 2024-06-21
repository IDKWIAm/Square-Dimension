using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ��������� ������
    [Header("Player")]
    [SerializeField] float speed = 8f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float coyoteTime = 0.08f;
    [SerializeField] float maxFallingSpeed = 15f;
    [SerializeField] public bool weaponEnabled = true;
    [SerializeField] float weaponShowTime = 0.1f;
    [SerializeField] float attackCooldown = 0.12f;

    // ��������� �������� ������
    [Header("Double Jump")]
    [SerializeField] bool doubleJumpEnabled;

    // ��������� �����
    [Header("Dash")]
    [SerializeField] bool dashEnabled;
    [SerializeField] float pressingInterval = 0.2f;
    [SerializeField] float dashTime = 1.5f;
    [SerializeField] float dashSpeed = 8f;
    [SerializeField] float dashCooldown = 1f;

    // ��������� �������� �������
    [Header("Fast Falling")]
    [SerializeField] bool fastFallingEnabled;
    [SerializeField] float fastFallingMultiplier = 3f;

    [Space]

    // ������ �� �������
    [SerializeField] Transform groundChecker;
    [SerializeField] GameObject weapon;
    [SerializeField] GameObject playerSprite;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask IgnoreOWLayer;
    [SerializeField] AudioSource swingSound;
    


    private bool _faceRight;

    private bool _isMovingX;

    private bool _doubleJumpPerformed;
    private bool _dashPerformed;

    private bool _dashRight;
    private bool _dashLeft;
    private bool _dPressed;
    private bool _aPressed;

    private float _dashIntervalTimer;
    [HideInInspector] public float _KBCounter;
    private float _coyoteCounter;
    private float _attackTimer;

    private float _defaultGravity;

    private Rigidbody2D _playerRigitbody;
    private Animator _playerAnimator;

    private void Start()
    {
        Init();
    }

    void Update()
    {
        // ������ �� ���������, ���� �������� ������� ����������� �� ����
        if (Time.timeScale == 0) return;
        TimerUpdate();
        MoveUpdate();
        DashUpdate();
        JumpUpdate();
        AttackUpdate();
    }

    
    private void Init()
    {
        // �������� ������
        _playerRigitbody = GetComponent<Rigidbody2D>();
        _playerAnimator = GetComponent<Animator>();

        // � ��������� �������� �� ���������
        _defaultGravity = _playerRigitbody.gravityScale;
        if (transform.localScale.x > 0) _faceRight = true;
    }

    // �������� ����� �� ����� �� �����
    private bool IsGrounded()
    {
        if (groundChecker == null) return true;
        return Physics2D.OverlapCircle(groundChecker.position, 0.2f, groundLayer);
    }

    // �������� ��������� �� ��� ������ ������
    private bool CoyoteJumpEnable()
    {
        return _coyoteCounter > 0;
    }

    // ������� ��������
    private void TimerUpdate()
    {
        // ������� ������� ����� ��������� ��� �����
        if (_aPressed || _dPressed)
        {
            _dashIntervalTimer -= Time.deltaTime;
            if (_dashIntervalTimer <= 0)
            {
                _aPressed = false;
                _dPressed = false;
            }
        }
        // ����� �����
        if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
        // ����� ������ ������
        if (_coyoteCounter > 0) _coyoteCounter -= Time.deltaTime;
    }

    // ������������
    private void MoveUpdate()
    {
        // ���������� �������� �� ��������� � ������ �����
        _isMovingX = false;
        _playerAnimator?.SetBool("IsRunning", false);

        // ���� ��� ��������� ������ (KnockBack) �� ������������ ����������
        if (_KBCounter > 0)
        {
            _KBCounter -= Time.deltaTime;
            return;
        }

        // ������������ �����
        if (Input.GetKey(KeyCode.A))
        {
            _playerRigitbody.velocity = new Vector2(-speed, _playerRigitbody.velocity.y);
            _isMovingX = true;

            // ��������� ���������, ���� ������� � ������ �������
            if (_faceRight)
            {
                MirrorCharacter();
                _faceRight = false;
            }
        }
        // � ������
        if (Input.GetKey(KeyCode.D))
        {
            _playerRigitbody.velocity = new Vector2(speed, _playerRigitbody.velocity.y);
            _isMovingX = true;

            // ��������� ���������, ���� ������� � ������ �������
            if (!_faceRight)
            {
                MirrorCharacter();
                _faceRight = true;
            }
        }

        // ���� ����� �� �������� ������� ����������� � ����� ���������, �������� ����������� ����� �� X
        if (!_isMovingX && !_dashRight && !_dashLeft)
        {
            _playerRigitbody.velocity = new Vector2(0, _playerRigitbody.velocity.y);
        }

        // ���� ���� ��������� �� X �� ������� ��� ���������
        if (_isMovingX) _playerAnimator?.SetBool("IsRunning", true);
    }

    // ��������� ���������
    private void MirrorCharacter()
    {
        var scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void JumpUpdate()
    {
        // ���������� �������� �� ��������� � ������ �����
        _playerRigitbody.gravityScale = _defaultGravity;

        // ������� ������ ������������� ���������
        if (Input.GetKeyDown(KeyCode.S))
        {
            playerSprite.layer = IgnoreOWLayer;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            playerSprite.layer = playerLayer;
        }

        // ���� ����� �� ����� �������� ������ ������ � ���������� ������� ������
        if (IsGrounded())
        {
            _coyoteCounter = coyoteTime;
            _doubleJumpPerformed = false;
        }

        // ���� ����� ���������� ������� ����, �� �������� �������
        if (Input.GetKey(KeyCode.S) && fastFallingEnabled)
        {
            _playerRigitbody.gravityScale *= fastFallingMultiplier;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ������: ���� �� �����, ���� ��� ������� ������ ������
            if (IsGrounded() || CoyoteJumpEnable())
            {
                _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, jumpForce);
            }
            // ������� ������ ���� ���������� ���������� �� ��������� � �� ��������
            else if (doubleJumpEnabled && !_doubleJumpPerformed)
            {
                _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, jumpForce);
                _doubleJumpPerformed = true;
            }
        }

        // ����������� �� �������� �������. ���� ������ �������, ���������� ���������
        if (_playerRigitbody.velocity.y < -maxFallingSpeed)
        {
            _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, -maxFallingSpeed);
        }
    }

    // ����� � ������� ���
    private void AttackUpdate()
    {
        if (Time.timeScale == 0) return;
        if (weaponEnabled == false) return;

        // ���� ����� ����������, �� ������� (�������� ������)
        if (Input.GetMouseButtonDown(0) && _attackTimer <= 0)
        {
            weapon?.SetActive(true);
            Invoke("DisableWeapon", weaponShowTime);
            swingSound?.PlayOneShot(swingSound.clip);
            _attackTimer = attackCooldown;
        }
    }

    // ����� (������ � ����� ��������� ��������)
    private void DashUpdate()
    {
        // �������� ����� �� ����� ����� ������
        if (_dashRight)
        {
            _playerRigitbody.velocity = new Vector2(dashSpeed, _playerRigitbody.velocity.y);
        }
        // � �����
        else if (_dashLeft)
        {
            _playerRigitbody.velocity = new Vector2(-dashSpeed, _playerRigitbody.velocity.y);
        }

        // ���� ��� ��������, ������� �� ������
        if (!dashEnabled) return;

        // ����� �� ����� ���������� �����
        if (IsGrounded() || CoyoteJumpEnable()) _dashPerformed = false;

        // �������� ����� �� ����� ������� �����
        //// ����� �����
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (_aPressed)
            {
                // ���� ��� ������ ��� ���� ������� ������ � ����� ��������, ��������� ���
                if (IsGrounded() || CoyoteJumpEnable() || !_dashPerformed)
                {
                    _aPressed = false;
                    _dashPerformed = true;
                    _dashIntervalTimer = 0;
                    _dashLeft = true;
                    dashEnabled = false;
                    Invoke("DisableDashLeft", dashTime);
                    Invoke("ActivateDash", dashTime + dashCooldown);
                    _playerAnimator?.SetTrigger("Dash");
                }
            }
            // �������, ��� ������ ���� ������ � ��������� ������
            _aPressed = true;
            _dPressed = false;
            _dashIntervalTimer = pressingInterval;
        }

        //// ����� ������
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (_dPressed)
            {
                // ���� ��� ������ ��� ���� ������� ������ � ����� ��������, ��������� ���
                if (IsGrounded() || CoyoteJumpEnable() || !_dashPerformed)
                {
                    _dPressed = false;
                    _dashPerformed = true;
                    _dashIntervalTimer = 0;
                    _dashRight = true;
                    dashEnabled = false;
                    Invoke("DisableDashRight", dashTime);
                    Invoke("ActivateDash", dashTime + dashCooldown);
                    _playerAnimator?.SetTrigger("Dash");
                }
            }
            // �������, ��� ������ ���� ������ � ��������� ������
            _dPressed = true;
            _aPressed = false;
            _dashIntervalTimer = pressingInterval;
        }
    }

    // ������ ��������������� ������, ������� ���������� ����� Invoke, ����� �� ������������ ������ �������
    private void DisableDashRight()
    {
        _dashRight = false;
    }

    private void DisableDashLeft()
    {
        _dashLeft = false;
    }

    private void ActivateDash()
    {
        dashEnabled = true;
    }

    private void DisableWeapon()
    {
        weapon?.SetActive(false);
    }

    // ������ ��� ��������� �����������. ��������� ��������, ������� ������� ����.
    public void Knock(float KBForce, float KBTime, bool knockFromRight)
    {
        // ���� ��� ���������, �� ��������
        if (_KBCounter > 0) return;

        // ����� ��������� �����
        DisableDashLeft();
        DisableDashRight();
        // ��������� ������ 
        _KBCounter = KBTime;

        // � ���������� ������, � ����������� �� ����, � ����� ������� ������ ����
        if (knockFromRight) _playerRigitbody.velocity = new Vector2(-KBForce, KBForce);
        else _playerRigitbody.velocity = new Vector2(KBForce, KBForce);
    }
}
