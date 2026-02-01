using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem confettiEffect;
    [SerializeField] private ParticleSystem circleWipeParticleEffect;
    [SerializeField] private Image circleWipeImage;

    [Header("Timing")]
    [SerializeField] private float confettiDuration = 2f;
    [SerializeField] private float circleParticleDuration = 1.5f;
    [SerializeField] private float circleExpandDuration = 1f;
    [SerializeField] private float circleShrinkDuration = 0.8f;


    private void Start()
    {
        CallSceneEntranceTransition();
    }
    public void CallTransitionCoroutine()
    {
        StartCoroutine(TransitionCoroutine());
    }

    public void CallSceneEntranceTransition()
    {
        StartCoroutine(SceneEntranceTransition());
    }  
    
    public void CallMainMenuTransition()
    {
        StartCoroutine(MainMenuTransition());
    }


    private IEnumerator TransitionCoroutine()
    {
        AudioManager.PlaySound(SoundType.CONFETTISOUND, 0.5f, 1f);
        AudioManager.PlaySound(SoundType.WINSOUND, 0.7f, 1f);
        confettiEffect.Play();
        circleWipeParticleEffect.Play();
        yield return new WaitForSeconds(confettiDuration);


        float elapsed = 0f;
        float maxDuration = Mathf.Max(circleParticleDuration, circleExpandDuration);

        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;

            if (circleWipeImage != null && elapsed < circleExpandDuration)
            {
                float scale = Mathf.Lerp(0f, 30f, elapsed / circleExpandDuration);
                circleWipeImage.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }


        if (circleWipeImage != null)
        {
            circleWipeImage.transform.localScale = Vector3.one * 30f;
        }

       
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private IEnumerator SceneEntranceTransition()
    {
       
        circleWipeImage.transform.localScale = Vector3.one * 30f;

        yield return new WaitForEndOfFrame();

        float elapsed = 0f;
        while (elapsed < circleShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(30f, 0f, elapsed / circleShrinkDuration);
            circleWipeImage.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        circleWipeImage.transform.localScale = Vector3.zero;
    }

    private IEnumerator MainMenuTransition()
    {
        float elapsed = 0f;
        float maxDuration = Mathf.Max(circleParticleDuration, circleExpandDuration);

        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;

            if (circleWipeImage != null && elapsed < circleExpandDuration)
            {
                float scale = Mathf.Lerp(0f, 30f, elapsed / circleExpandDuration);
                circleWipeImage.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }


        if (circleWipeImage != null)
        {
            circleWipeImage.transform.localScale = Vector3.one * 30f;
        }


        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}
