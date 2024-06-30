using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed;
    [SerializeField] float jumpForce;
    [SerializeField] float maxFallSpeed;
    [SerializeField] int extraJumpsValue;
    [SerializeField] ParticleSystem runParticles;
    [SerializeField] ParticleSystem jumpParticles;
    [SerializeField] ParticleSystem landingParticles;

    [Header("Dash")]
    [SerializeField] float dashingPower;
    [SerializeField] float dashingTime;
    [SerializeField] float dashingCooldown;
    private bool _canDash = true;
    private bool _isDashing;
    
    [Header("Cool Jump")]
    [SerializeField] float coyoteTime;
    private float _coyoteTimeCounter;
    [SerializeField] float jumpBufferTime;
    private float _jumpBufferCounter;
    
    [Header("Wall Slide")]
    [SerializeField] float wallSlidingSpeed;
    [SerializeField] Transform wallCheck;
    [SerializeField] LayerMask wallLayer;
    private bool _isWallSliding;
    
    [Header("Wall Jump")]
    private bool isWallJumping;
    private float wallJumpingDirection;
    [SerializeField] float wallJumpingTime;
    private float wallJumpingCounter;
    [SerializeField] float wallJumpingDuration;
    private Vector2 wallJumpingPower = new Vector2(8f, 16f);
    
    [Header("Other Components")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator animator;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] TrailRenderer trailRenderer;
    
    private bool _isFacingRight = true;
    private float _horizontal;
    private float _extraJumps;
    private bool _inAirLastFrame;
    void Start()
    {
        _extraJumps = extraJumpsValue;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        if (_isDashing) return;
        if (!isWallJumping)
        {
            Flip();
        }
        JumpUpdate();
        FallUpdate();
        DashUpdate();
        WallSlide();
        WallJump();
    }
    private void FixedUpdate()
    {
        if (_isDashing) return;
        MoveUpdate();
    }
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }
    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }
    private void MoveUpdate()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");
        if (!isWallJumping)
            rb.velocity = new Vector2(_horizontal * speed, rb.velocity.y);
        
        if (_horizontal == 0)
            animator.SetBool("isRunning", false);
        
        else
            animator.SetBool("isRunning", true);
        
        if (IsGrounded() && rb.velocity.x != 0)
            runParticles.Play();
    }
    private void Flip()
    {
        if (_isFacingRight && _horizontal < 0f || !_isFacingRight && _horizontal > 0f)
        {
            _isFacingRight = !_isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
    private void JumpUpdate()
    {
        if (rb.velocity.y < -maxFallSpeed)
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);

        if (IsGrounded())
        {
            _coyoteTimeCounter = coyoteTime;
            if (_inAirLastFrame)
                landingParticles.Play();
        }
        else
            _coyoteTimeCounter -= Time.deltaTime;
        
        if (Input.GetButtonDown("Jump"))
            _jumpBufferCounter = jumpBufferTime;
        
        else
            _jumpBufferCounter -= Time.deltaTime;
        
        if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            _jumpBufferCounter = 0;
            _extraJumps = extraJumpsValue;
        }
        else if (Input.GetButtonDown("Jump") && _extraJumps > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            _extraJumps--;
            _coyoteTimeCounter = 0;
            jumpParticles.gameObject.SetActive(true);
        }

        if (IsGrounded()) _inAirLastFrame = false;
        else _inAirLastFrame = true;
    }

    private void FallUpdate()
    {
        if (Input.GetKeyDown(KeyCode.S)) gameObject.layer = 8;
        if (Input.GetKeyUp(KeyCode.S)) gameObject.layer = 0;
    }

    private void DashUpdate()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && _canDash)
        {
            StartCoroutine(Dash());
        }
    }
    private IEnumerator Dash()
    {
        _canDash = false;
        _isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0);
        trailRenderer.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        trailRenderer.emitting = false;
        rb.gravityScale = originalGravity;
        _isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        _canDash = true;
    }
    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && _horizontal != 0)
        {
            _isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
            _isWallSliding = false;
    }
    private void WallJump()
    {
        if (_isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;
            if (transform.localScale.x != wallJumpingDirection)
            {
                _isFacingRight = !_isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }
    private void StopWallJumping()
    {
        isWallJumping = false;
    }
}