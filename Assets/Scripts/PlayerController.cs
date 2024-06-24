using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Настройки игрока
    [Header("Player")]
    [SerializeField] float speed = 8f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float coyoteTime = 0.08f;
    [SerializeField] float maxFallingSpeed = 15f;
    [SerializeField] public bool weaponEnabled = true;
    [SerializeField] float weaponShowTime = 0.1f;
    [SerializeField] float attackCooldown = 0.12f;

    // Настройки двойного прыжка
    [Header("Double Jump")]
    [SerializeField] bool doubleJumpEnabled;

    // Настройки рывка
    [Header("Dash")]
    [SerializeField] bool dashEnabled;
    [SerializeField] float pressingInterval = 0.2f;
    [SerializeField] float dashTime = 1.5f;
    [SerializeField] float dashSpeed = 8f;
    [SerializeField] float dashCooldown = 1f;

    // Настройки быстрого падения
    [Header("Fast Falling")]
    [SerializeField] bool fastFallingEnabled;
    [SerializeField] float fastFallingMultiplier = 3f;

    [Space]

    // Ссылки на объекты
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
        // Ничего не выполняем, если скорость времени установлена на ноль
        if (Time.timeScale == 0) return;
        TimerUpdate();
        MoveUpdate();
        DashUpdate();
        JumpUpdate();
        AttackUpdate();
    }

    
    private void Init()
    {
        // Создание ссылок
        _playerRigitbody = GetComponent<Rigidbody2D>();
        _playerAnimator = GetComponent<Animator>();

        // И установка значений по умолчанию
        _defaultGravity = _playerRigitbody.gravityScale;
        if (transform.localScale.x > 0) _faceRight = true;
    }

    // Проверка стоит ли игрок на земле
    private bool IsGrounded()
    {
        if (groundChecker == null) return true;
        return Physics2D.OverlapCircle(groundChecker.position, 0.2f, groundLayer);
    }

    // Проверка действует ли еще прыжок койота
    private bool CoyoteJumpEnable()
    {
        return _coyoteCounter > 0;
    }

    // Подсчет таймеров
    private void TimerUpdate()
    {
        // Подсчет времени между нажатиями для рывка
        if (_aPressed || _dPressed)
        {
            _dashIntervalTimer -= Time.deltaTime;
            if (_dashIntervalTimer <= 0)
            {
                _aPressed = false;
                _dPressed = false;
            }
        }
        // Откат атаки
        if (_attackTimer > 0) _attackTimer -= Time.deltaTime;
        // Время прыжка койота
        if (_coyoteCounter > 0) _coyoteCounter -= Time.deltaTime;
    }

    // Передвижение
    private void MoveUpdate()
    {
        // Выставляем значения по умолчанию в каждом кадре
        _isMovingX = false;
        _playerAnimator?.SetBool("IsRunning", false);

        // Если еще действует отдача (KnockBack) то передвижение отлючается
        if (_KBCounter > 0)
        {
            _KBCounter -= Time.deltaTime;
            return;
        }

        // Передвижение влево
        if (Input.GetKey(KeyCode.A))
        {
            _playerRigitbody.velocity = new Vector2(-speed, _playerRigitbody.velocity.y);
            _isMovingX = true;

            // Отражение персонажа, если смотрит в другую сторону
            if (_faceRight)
            {
                MirrorCharacter();
                _faceRight = false;
            }
        }
        // И вправо
        if (Input.GetKey(KeyCode.D))
        {
            _playerRigitbody.velocity = new Vector2(speed, _playerRigitbody.velocity.y);
            _isMovingX = true;

            // Отражение персонажа, если смотрит в другую сторону
            if (!_faceRight)
            {
                MirrorCharacter();
                _faceRight = true;
            }
        }

        // Если игрок не нажимает клавиши передвиения и рывок неактивен, обнуляем перемещение перса по X
        if (!_isMovingX && !_dashRight && !_dashLeft)
        {
            _playerRigitbody.velocity = new Vector2(0, _playerRigitbody.velocity.y);
        }

        // Если перс двигается по X то говорим это аниматору
        if (_isMovingX) _playerAnimator?.SetBool("IsRunning", true);
    }

    // Отражение персонажа
    private void MirrorCharacter()
    {
        var scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void JumpUpdate()
    {
        // Выставляем значение по умолчанию в каждом кадре
        _playerRigitbody.gravityScale = _defaultGravity;

        // Падение сквозь односторонние платформы
        if (Input.GetKeyDown(KeyCode.S))
        {
            playerSprite.layer = IgnoreOWLayer;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            playerSprite.layer = playerLayer;
        }

        // Если игрок на земле включаем таймер койота и откатываем двойной прыжок
        if (IsGrounded())
        {
            _coyoteCounter = coyoteTime;
            _doubleJumpPerformed = false;
        }

        // Если игрок удерживает клавишу вниз, то ускоряем падение
        if (Input.GetKey(KeyCode.S) && fastFallingEnabled)
        {
            _playerRigitbody.gravityScale *= fastFallingMultiplier;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Прыжок: либо на земле, либо еще активен прыжок койота
            if (IsGrounded() || CoyoteJumpEnable())
            {
                _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, jumpForce);
            }
            // Двойной прыжок если предыдущие требования не выполнены и он доступен
            else if (doubleJumpEnabled && !_doubleJumpPerformed)
            {
                _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, jumpForce);
                _doubleJumpPerformed = true;
            }
        }

        // Ограничение на скорость падения. Если падает быстрее, выставляем минималку
        if (_playerRigitbody.velocity.y < -maxFallingSpeed)
        {
            _playerRigitbody.velocity = new Vector2(_playerRigitbody.velocity.x, -maxFallingSpeed);
        }
    }

    // Атака в ближнем бою
    private void AttackUpdate()
    {
        if (Time.timeScale == 0) return;
        if (weaponEnabled == false) return;

        // Если атака откатилась, то атакуем (включаем спрайт)
        if (Input.GetMouseButtonDown(0) && _attackTimer <= 0)
        {
            weapon?.SetActive(true);
            Invoke("DisableWeapon", weaponShowTime);
            swingSound?.PlayOneShot(swingSound.clip);
            _attackTimer = attackCooldown;
        }
    }

    // Рывок (вправо и влево проверяем отдельно)
    private void DashUpdate()
    {
        // Движение перса во время рывка вправо
        if (_dashRight)
        {
            _playerRigitbody.velocity = new Vector2(dashSpeed, _playerRigitbody.velocity.y);
        }
        // И влево
        else if (_dashLeft)
        {
            _playerRigitbody.velocity = new Vector2(-dashSpeed, _playerRigitbody.velocity.y);
        }

        // Если дэш отключен, выходим из метода
        if (!dashEnabled) return;

        // Когда на земле откатываем рывок
        if (IsGrounded() || CoyoteJumpEnable()) _dashPerformed = false;

        // Проверка хочет ли игрок сделать рывок
        //// Рывок влево
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (_aPressed)
            {
                // Если эта кнопка уже была недавно нажата и рывок доступен, совершаем его
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
            // Говорим, что кнопка была нажата и запускаем таймер
            _aPressed = true;
            _dPressed = false;
            _dashIntervalTimer = pressingInterval;
        }

        //// Рывок вправо
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (_dPressed)
            {
                // Если эта кнопка уже была недавно нажата и рывок доступен, совершаем его
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
            // Говорим, что кнопка была нажата и запускаем таймер
            _dPressed = true;
            _aPressed = false;
            _dashIntervalTimer = pressingInterval;
        }
    }

    // Четыре вспомогательных метода, которые вызываются через Invoke, чтобы не использовать лишние таймеры
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

    // Отдача при получении повреждений. Вызывется объектом, который наносит урон.
    public void Knock(float KBForce, float KBTime, bool knockFromRight)
    {
        // Если уже действует, то отменяем
        if (_KBCounter > 0) return;

        // Иначе отключаем рывок
        DisableDashLeft();
        DisableDashRight();
        // Запускаем таймер 
        _KBCounter = KBTime;

        // И откидываем игрока, в зависимости от того, с какой стороны пришел удар
        if (knockFromRight) _playerRigitbody.velocity = new Vector2(-KBForce, KBForce);
        else _playerRigitbody.velocity = new Vector2(KBForce, KBForce);
    }
}
