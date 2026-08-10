using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Pantalla de carga")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressText;

    [Header("Duración mínima de la barra")]
    [Tooltip("Segundos que tarda la barra en llenarse como mínimo, aunque la carga sea instantánea.")]
    [SerializeField] private float minimumLoadTime = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        if (progressText != null) progressText.text = "0%";

        yield return null; // deja que la pantalla se muestre un frame

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        // La barra sube según el tiempo mínimo, no según la carga real (que es instantánea)
        while (timer < minimumLoadTime)
        {
            timer += Time.unscaledDeltaTime;

            float barra = Mathf.Clamp01(timer / minimumLoadTime);

            if (progressBar != null) progressBar.value = barra;
            if (progressText != null) progressText.text = Mathf.RoundToInt(barra * 100f) + "%";

            yield return null;
        }

        // Asegura el 100%
        if (progressBar != null) progressBar.value = 1f;
        if (progressText != null) progressText.text = "100%";

        yield return new WaitForSeconds(0.2f); // breve pausa en el 100%

        // Activa la escena (ya está lista hace rato porque cargó al instante)
        operation.allowSceneActivation = true;

        // Espera a que termine de activarse
        while (!operation.isDone)
            yield return null;

        if (loadingScreen != null) loadingScreen.SetActive(false);
    }
}