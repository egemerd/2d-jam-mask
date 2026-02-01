using System;
using UnityEngine;

public class Finish : MonoBehaviour
{

    public event Action OnPlayerFinish; 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerFinish?.Invoke();
        }   
    }
}
