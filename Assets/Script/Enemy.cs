using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("pip");

        if (other.gameObject.CompareTag("MapEnd"))
        {
            Debug.Log("Enemy reached the end!");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("hit "+other.gameObject.tag);
        }
    }
    // public abstract void damage();

}
