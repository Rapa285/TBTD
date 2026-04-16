using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private int health;
    [SerializeField]
    private int damage;
    [SerializeField]
    private int movementSpeed;

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("pip");

        if (other.gameObject.CompareTag("MapEnd"))
        {
            Debug.Log("Enemy reached the end!");
            Destroy(gameObject);
        }
    }
    // public abstract void damage();

}
