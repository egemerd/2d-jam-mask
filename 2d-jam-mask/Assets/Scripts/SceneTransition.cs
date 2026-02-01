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

        // Ensure circle is fully expanded
        if (circleWipeImage != null)
        {
            circleWipeImage.transform.localScale = Vector3.one * 30f;
        }

        // Step 4: Load next scene
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
        // Start with circle fully covering screen
        circleWipeImage.transform.localScale = Vector3.one * 30f;

        yield return new WaitForEndOfFrame();

        // Shrink circle to reveal scene
        float elapsed = 0f;
        while (elapsed < circleShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(30f, 0f, elapsed / circleShrinkDuration);
            circleWipeImage.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Ensure circle is fully shrunk
        circleWipeImage.transform.localScale = Vector3.zero;
    }
}
