using System;
using UnityEngine;

public class Finish : MonoBehaviour
{
    public bool isFinished = false;
    public event Action OnPlayerFinish; 
    
    private void Awake()
    {
        isFinished = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isFinished)
        {
            isFinished = true;
            OnPlayerFinish?.Invoke();
        }   
    }

}
