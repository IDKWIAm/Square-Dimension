using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform followingTarget;
    [SerializeField, Range(0f, 1f)] private float paralaxStrenght;
    private Vector3 targetPosition;
    private void Start()
    {
        if (!followingTarget)
            followingTarget = Camera.main.transform;
        targetPosition = followingTarget.position;
    }
    private void Update()
    {
        var delta = followingTarget.position - targetPosition;
        targetPosition = followingTarget.position;
        transform.position += delta * paralaxStrenght;
    }
}
