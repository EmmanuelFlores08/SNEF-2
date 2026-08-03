using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class MovieTriviaController : MonoBehaviour
{
    [Header("Cámara para los clics (World Space)")]
    [SerializeField] private Camera triviaEventCamera;

    [Header("Canvas / raíz")]
    [SerializeField] private GameObject triviaCanvasRoot;
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Paneles")]
    [SerializeField] private GameObject panelPreguntas;
    [SerializeField] private GameObject panelRespuestaIncorrecta;
    [SerializeField] private GameObject panelResultado;

    [Header("Pregunta")]
    [SerializeField] private GameObject textoPregunta;

    [Header("Opciones de respuesta")]
    [SerializeField] private Button[] answerButtons = new Button[4];
    [SerializeField] private GameObject[] answerTextObjects = new GameObject[4];

    [Header("Panel de respuesta incorrecta")]
    [SerializeField] private GameObject textoJustificacion;
    [SerializeField] private Button buttonSiguiente;

    [Header("Panel de resultado")]
    [SerializeField] private Image[] estrellas = new Image[4];
    [SerializeField] private GameObject textoRespuestasCorrectas;
    [SerializeField] private Button buttonTerminar;

    [Header("Configuración")]
    [SerializeField] private int maxQuestions = 4;

    [Header("Colores resultado")]
    [SerializeField] private Color starOnColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private Color starOffColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("Animaciones")]
    [SerializeField] private float panelFadeDuration = 0.18f;
    [SerializeField] private float panelScaleFrom = 0.94f;
    [SerializeField] private float correctDelayBeforeNext = 0.18f;
    [SerializeField] private float buttonPulseScale = 1.05f;
    [SerializeField] private float buttonPulseDuration = 0.10f;
    [SerializeField] private float shakeDistance = 10f;
    [SerializeField] private float shakeDuration = 0.18f;

    private MovieTriviaData activeTrivia;
    private Action<int, int> onTriviaFinished;

    private int currentQuestionIndex;
    private int correctAnswers;
    private int totalQuestions;

    private bool initialized;
    private bool isResolvingAnswer;
    private bool waitingAfterWrongAnswer;

    private readonly Dictionary<RectTransform, Vector3> originalScales = new Dictionary<RectTransform, Vector3>();

    private void Awake()
    {
        EnsureInitialized();
        CloseInstant();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (triviaCanvasRoot == null)
            triviaCanvasRoot = gameObject;

        if (worldSpaceCanvas == null)
            worldSpaceCanvas = GetComponentInChildren<Canvas>(true);

        if (worldSpaceCanvas != null && worldSpaceCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (Camera.main != null)
                worldSpaceCanvas.worldCamera = Camera.main;

            GraphicRaycaster raycaster = worldSpaceCanvas.GetComponent<GraphicRaycaster>();

            if (raycaster == null)
                raycaster = worldSpaceCanvas.gameObject.AddComponent<GraphicRaycaster>();

            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int capturedIndex = i;

            if (answerButtons[i] != null)
                answerButtons[i].onClick.AddListener(() => SelectAnswer(capturedIndex));
        }

        if (buttonSiguiente != null)
            buttonSiguiente.onClick.AddListener(ContinueAfterWrongAnswer);

        if (buttonTerminar != null)
            buttonTerminar.onClick.AddListener(FinishTrivia);
    }

    public void OpenTrivia(MovieTriviaData triviaData, string movieTitle, Action<int, int> finishCallback)
    {
        EnsureInitialized();
        if (worldSpaceCanvas != null && worldSpaceCanvas.renderMode == RenderMode.WorldSpace)
        {
        Camera cam = (triviaEventCamera != null) ? triviaEventCamera : Camera.main;
        if (cam != null)
            worldSpaceCanvas.worldCamera = cam;
        }
        activeTrivia = triviaData;
        onTriviaFinished = finishCallback;

        currentQuestionIndex = 0;
        correctAnswers = 0;
        totalQuestions = 0;

        isResolvingAnswer = false;
        waitingAfterWrongAnswer = false;

        if (activeTrivia == null || activeTrivia.questions == null || activeTrivia.questions.Count == 0)
        {
            Debug.LogWarning($"MovieTriviaController: La película '{movieTitle}' no tiene trivia configurada.");
            return;
        }

        totalQuestions = Mathf.Min(activeTrivia.questions.Count, maxQuestions);

        if (totalQuestions <= 0)
        {
            Debug.LogWarning($"MovieTriviaController: La película '{movieTitle}' no tiene preguntas válidas.");
            return;
        }

        if (activeTrivia.questions.Count != 4)
        {
            Debug.LogWarning($"MovieTriviaController: La trivia de '{movieTitle}' debería tener 4 preguntas. Actualmente tiene {activeTrivia.questions.Count}.");
        }

        if (triviaCanvasRoot != null)
            triviaCanvasRoot.SetActive(true);

        ResetStars();
        ShowQuestion(currentQuestionIndex);
    }

    public void CloseInstant()
    {
        StopAllCoroutines();

        isResolvingAnswer = false;
        waitingAfterWrongAnswer = false;

        if (panelPreguntas != null)
            panelPreguntas.SetActive(false);

        if (panelRespuestaIncorrecta != null)
            panelRespuestaIncorrecta.SetActive(false);

        if (panelResultado != null)
            panelResultado.SetActive(false);

        if (triviaCanvasRoot != null)
            triviaCanvasRoot.SetActive(false);
    }

    private void ShowQuestion(int questionIndex)
    {
        if (activeTrivia == null || questionIndex >= totalQuestions)
        {
            ShowResult();
            return;
        }

        StopAllCoroutines();

        isResolvingAnswer = false;
        waitingAfterWrongAnswer = false;

        TriviaQuestionData question = activeTrivia.questions[questionIndex];

        if (question.answers == null || question.answers.Count < 2 || question.answers.Count > 4)
        {
            Debug.LogWarning($"MovieTriviaController: La pregunta {questionIndex + 1} debe tener entre 2 y 4 respuestas.");
        }

        SetPanel(panelPreguntas, true);
        SetPanel(panelRespuestaIncorrecta, false);
        SetPanel(panelResultado, false);

        SetUIText(textoPregunta, question.question);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];

            if (button == null)
                continue;

            bool hasAnswer = question.answers != null && i < question.answers.Count && i < 4;

            button.gameObject.SetActive(hasAnswer);
            button.interactable = hasAnswer;

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
                buttonRect.localScale = GetOriginalScale(buttonRect);

            if (hasAnswer)
            {
                GameObject textTarget = GetAnswerTextTarget(i);
                SetUIText(textTarget, question.answers[i].answerText);
            }
        }

        StartCoroutine(ShowPanelRoutine(panelPreguntas));
    }

    private void SelectAnswer(int answerIndex)
    {
        
        Debug.Log($"SelectAnswer llamado: {answerIndex}");

        if (isResolvingAnswer || waitingAfterWrongAnswer)
            return;

        if (activeTrivia == null || currentQuestionIndex >= totalQuestions)
            return;

        TriviaQuestionData question = activeTrivia.questions[currentQuestionIndex];

        if (question.answers == null || answerIndex < 0 || answerIndex >= question.answers.Count)
            return;

        isResolvingAnswer = true;
        SetAnswerButtonsInteractable(false);

        TriviaAnswerData selectedAnswer = question.answers[answerIndex];
        Button selectedButton = answerButtons[answerIndex];

        if (selectedAnswer.isCorrect)
        {
            correctAnswers++;
            StartCoroutine(CorrectAnswerRoutine(selectedButton));
        }
        else
        {
            StartCoroutine(IncorrectAnswerRoutine(selectedButton, selectedAnswer));
        }
    }

    private IEnumerator CorrectAnswerRoutine(Button selectedButton)
    {
        if (selectedButton != null)
            yield return StartCoroutine(PulseRoutine(selectedButton.GetComponent<RectTransform>()));

        yield return new WaitForSecondsRealtime(correctDelayBeforeNext);

        GoToNextQuestion();
    }

    private IEnumerator IncorrectAnswerRoutine(Button selectedButton, TriviaAnswerData selectedAnswer)
    {
        if (selectedButton != null)
            yield return StartCoroutine(ShakeRoutine(selectedButton.GetComponent<RectTransform>()));

        string justification = selectedAnswer.wrongJustification;

        if (string.IsNullOrWhiteSpace(justification))
            justification = "La respuesta es incorrecta. Revisa la información del video para identificar la opción correcta.";

        SetUIText(textoJustificacion, justification);

        SetPanel(panelPreguntas, false);
        SetPanel(panelRespuestaIncorrecta, true);
        SetPanel(panelResultado, false);

        waitingAfterWrongAnswer = true;
        isResolvingAnswer = false;

        StartCoroutine(ShowPanelRoutine(panelRespuestaIncorrecta));
    }

    private void ContinueAfterWrongAnswer()
    {
        if (!waitingAfterWrongAnswer)
            return;

        waitingAfterWrongAnswer = false;
        GoToNextQuestion();
    }

    private void GoToNextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex >= totalQuestions)
            ShowResult();
        else
            ShowQuestion(currentQuestionIndex);
    }

    private void ShowResult()
    {
        StopAllCoroutines();

        isResolvingAnswer = false;
        waitingAfterWrongAnswer = false;

        SetPanel(panelPreguntas, false);
        SetPanel(panelRespuestaIncorrecta, false);
        SetPanel(panelResultado, true);

        SetUIText(textoRespuestasCorrectas, $"{correctAnswers}/{totalQuestions} respuestas correctas");

        UpdateStars();

        StartCoroutine(ResultRoutine());
    }

    private IEnumerator ResultRoutine()
    {
        yield return StartCoroutine(ShowPanelRoutine(panelResultado));

        for (int i = 0; i < estrellas.Length; i++)
        {
            if (estrellas[i] == null)
                continue;

            bool earned = i < correctAnswers;

            ApplyStarColor(estrellas[i], earned ? starOnColor : starOffColor);

            RectTransform starRect = estrellas[i].GetComponent<RectTransform>();

            if (earned)
                yield return StartCoroutine(StarPopRoutine(starRect));
            else if (starRect != null)
                starRect.localScale = GetOriginalScale(starRect);
        }
    }

    private void FinishTrivia()
    {
        int finalCorrectAnswers = correctAnswers;
        int finalTotalQuestions = totalQuestions;

        CloseInstant();

        onTriviaFinished?.Invoke(finalCorrectAnswers, finalTotalQuestions);
        onTriviaFinished = null;
    }

    private void UpdateStars()
    {
        for (int i = 0; i < estrellas.Length; i++)
        {
            if (estrellas[i] == null)
            {
                Debug.LogWarning($"MovieTriviaController: La estrella en índice {i} no está asignada.");
                continue;
            }

            bool earned = i < correctAnswers;
            ApplyStarColor(estrellas[i], earned ? starOnColor : starOffColor);

            Debug.Log($"MovieTriviaController: Estrella {i + 1} => {(earned ? "AMARILLA" : "GRIS")}");
        }
    }

    private void ResetStars()
    {
        for (int i = 0; i < estrellas.Length; i++)
        {
            if (estrellas[i] == null)
                continue;

            ApplyStarColor(estrellas[i], starOffColor);

            RectTransform starRect = estrellas[i].GetComponent<RectTransform>();
            if (starRect != null)
                starRect.localScale = GetOriginalScale(starRect);
        }
    }

    private void ApplyStarColor(Image star, Color color)
    {
        if (star == null)
            return;

        star.color = color;

        Graphic[] childGraphics = star.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in childGraphics)
        {
            if (graphic != null)
                graphic.color = color;
        }
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null && button.gameObject.activeSelf)
                button.interactable = interactable;
        }
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private GameObject GetAnswerTextTarget(int index)
    {
        if (answerTextObjects != null &&
            index >= 0 &&
            index < answerTextObjects.Length &&
            answerTextObjects[index] != null)
        {
            return answerTextObjects[index];
        }

        if (answerButtons != null &&
            index >= 0 &&
            index < answerButtons.Length &&
            answerButtons[index] != null)
        {
            Transform child = answerButtons[index].transform.Find("TextoRespuesta");

            if (child != null)
                return child.gameObject;

            return answerButtons[index].gameObject;
        }

        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private Vector3 GetOriginalScale(RectTransform rect)
    {
        if (rect == null)
            return Vector3.one;

        if (!originalScales.ContainsKey(rect))
            originalScales.Add(rect, rect.localScale);

        return originalScales[rect];
    }

    private IEnumerator ShowPanelRoutine(GameObject panel)
    {
        if (panel == null)
            yield break;

        RectTransform rect = panel.GetComponent<RectTransform>();
        CanvasGroup group = GetOrAddCanvasGroup(panel);

        Vector3 baseScale = rect != null ? GetOriginalScale(rect) : Vector3.one;

        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (rect != null)
            rect.localScale = baseScale * panelScaleFrom;

        float elapsed = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / panelFadeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (group != null)
                group.alpha = eased;

            if (rect != null)
                rect.localScale = Vector3.Lerp(baseScale * panelScaleFrom, baseScale, eased);

            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        if (rect != null)
            rect.localScale = baseScale;
    }

    private IEnumerator PulseRoutine(RectTransform rect)
    {
        if (rect == null)
            yield break;

        Vector3 baseScale = GetOriginalScale(rect);
        Vector3 bigScale = baseScale * buttonPulseScale;

        float halfDuration = buttonPulseDuration * 0.5f;

        yield return ScaleRoutine(rect, baseScale, bigScale, halfDuration);
        yield return ScaleRoutine(rect, bigScale, baseScale, halfDuration);
    }

    private IEnumerator StarPopRoutine(RectTransform rect)
    {
        if (rect == null)
            yield break;

        Vector3 baseScale = GetOriginalScale(rect);

        yield return ScaleRoutine(rect, baseScale * 0.65f, baseScale * 1.18f, 0.10f);
        yield return ScaleRoutine(rect, baseScale * 1.18f, baseScale, 0.08f);
    }

    private IEnumerator ShakeRoutine(RectTransform rect)
    {
        if (rect == null)
            yield break;

        Vector3 basePosition = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / shakeDuration);
            float strength = 1f - t;

            float offsetX = Mathf.Sin(t * Mathf.PI * 8f) * shakeDistance * strength;
            rect.anchoredPosition = basePosition + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        rect.anchoredPosition = basePosition;
    }

    private IEnumerator ScaleRoutine(RectTransform rect, Vector3 from, Vector3 to, float duration)
    {
        if (rect == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            rect.localScale = Vector3.Lerp(from, to, eased);

            yield return null;
        }

        rect.localScale = to;
    }

    private void SetUIText(GameObject target, string value)
    {
        if (target == null)
            return;

        Text legacyText = target.GetComponent<Text>();

        if (legacyText != null)
        {
            legacyText.text = value;
            return;
        }

        if (TrySetTextMeshProText(target, value))
            return;

        legacyText = target.GetComponentInChildren<Text>(true);

        if (legacyText != null)
        {
            legacyText.text = value;
            return;
        }

        Debug.LogWarning($"MovieTriviaController: No se encontró componente de texto en {target.name}.");
    }

    private bool TrySetTextMeshProText(GameObject target, string value)
    {
        Component[] components = target.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type type = component.GetType();
            string typeName = type.Name;

            if (typeName != "TextMeshProUGUI" && typeName != "TextMeshPro" && typeName != "TMP_Text")
                continue;

            PropertyInfo textProperty = type.GetProperty("text");

            if (textProperty == null || !textProperty.CanWrite)
                continue;

            textProperty.SetValue(component, value, null);
            return true;
        }

        return false;
    }
}