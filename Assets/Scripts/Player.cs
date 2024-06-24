using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private int extraJumpsValue;
    
    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldown;
    private bool _canDash = true;
    private bool _isDashing;
    
    [Header("Cool Jump")]
    [SerializeField] private float coyoteTime;
    private float _coyoteTimeCounter;
    [SerializeField] private float jumpBufferTime;
    private float _jumpBufferCounter;
    
    [Header("Wall Slide")]
    [SerializeField] private float wallSlidingSpeed;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private bool _isWallSliding;
    
    [Header("Wall Jump")]
    private bool isWallJumping;
    private float wallJumpingDirection;
    [SerializeField] private float wallJumpingTime;
    private float wallJumpingCounter;
    [SerializeField] private float wallJumpingDuration;
    private Vector2 wallJumpingPower = new Vector2(8f, 16f);
    
    [Header("Other Components")]
    [SerializeField] private Rigidbody2D rigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer trailRenderer;
    
    private bool _isFacingRight = true;
    private float _horizontal;
    private float _extraJumps;
    void Start()
    {
        _extraJumps = extraJumpsValue;
        rigidbody = GetComponent<Rigidbody2D>();
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
        {
            rigidbody.velocity = new Vector2(_horizontal * speed, rigidbody.velocity.y);
        }
        if (_horizontal == 0)
        {
            animator.SetBool("isRunning", false);
        }
        else
        {
            animator.SetBool("isRunning", true);
        }
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
        if (IsGrounded())
        {
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0)
        {
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpForce);
            _jumpBufferCounter = 0;
            _extraJumps = extraJumpsValue;
        }
        else if (Input.GetButtonDown("Jump") && _extraJumps > 0)
        {
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpForce);
            _extraJumps--;
            _coyoteTimeCounter = 0;
        }
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
        float originalGravity = rigidbody.gravityScale;
        rigidbody.gravityScale = 0;
        rigidbody.velocity = new Vector2(transform.localScale.x * dashingPower, 0);
        trailRenderer.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        trailRenderer.emitting = false;
        rigidbody.gravityScale = originalGravity;
        _isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        _canDash = true;
    }
    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && _horizontal != 0)
        {
            _isWallSliding = true;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Clamp(rigidbody.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            _isWallSliding = false;
        }
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
            rigidbody.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
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