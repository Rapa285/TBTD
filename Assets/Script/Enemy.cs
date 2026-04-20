using System;
using UnityEngine;
using UnityEngine.Splines;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private int health;
    [SerializeField]
    private int damage;
    [SerializeField]
    private int movementSpeed;
    [SerializeField]
    private SplineAnimate splineAnimate;

    void Start()
    {
        splineAnimate = GetComponent<SplineAnimate>();
    }

    void Update()
    {
        if (splineAnimate.NormalizedTime >= 1f)
        {
            Debug.Log("Enemy has reached the end!");
            Destroy(gameObject);
        }
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     // Debug.Log("pip");

    //     if (other.gameObject.CompareTag("MapEnd"))
    //     {
    //         Debug.Log("Enemy reached the end!");
    //         Destroy(gameObject);
    //     }
    // }
    // public abstract void damage();

}
