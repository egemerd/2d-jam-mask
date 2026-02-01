using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Finish finish;
    [SerializeField] private SceneTransition sceneTransition;
    private void Awake()
    {
        Instance = this;    
    }

    

    private void OnEnable()
    {
        if (finish != null)
        {
            finish.OnPlayerFinish += HandlePlayerFinish;
        }
    }

    private void OnDisable()
    {
        if (finish != null)
        {
            finish.OnPlayerFinish -= HandlePlayerFinish;
        }
    }

    private void HandlePlayerFinish()
    {
        sceneTransition.CallTransitionCoroutine();  
    }

    
    
    
}