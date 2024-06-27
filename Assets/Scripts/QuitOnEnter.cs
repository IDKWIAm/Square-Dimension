using UnityEngine;

public class QuitOnEnter : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Application.Quit();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Application.Quit();
    }
}
