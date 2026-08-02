using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class MovieCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos de la película")]
    [SerializeField] private string movieId;
    [SerializeField] private string movieTitle;

    [Header("Video temporal / mock")]
    [SerializeField] private VideoClip videoClip;

    [Header("Video futuro backend / CDN")]
    [SerializeField] private string videoUrl;

    [Header("Trivia")]
    [SerializeField] private Button triviaButton;

    [Tooltip("Actívalo para usar una trivia genérica de prueba sin capturar datos manuales.")]
    [SerializeField] private bool usarDatosDePrueba = false;

    [Tooltip("Trivia real de esta película. Si 'Usar datos de prueba' está activo, se ignora temporalmente esta data.")]
    [SerializeField] private MovieTriviaData triviaData;

    [Header("Referencias visuales")]
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private RectTransform animatedTarget;

    [Header("Animación Hover")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.12f;

    [Header("Animación Selección")]
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;

    private Button button;
    private MovieSelectorController controller;
    private Coroutine scaleRoutine;

    private bool isSelected;
    private bool isSelectionAnimating;

    private MovieTriviaData testTriviaData;

    public string MovieId => movieId;
    public string MovieTitle => movieTitle;
    public VideoClip VideoClip => videoClip;
    public string VideoUrl => videoUrl;

    public MovieTriviaData TriviaData
    {
        get
        {
            if (usarDatosDePrueba)
            {
                if (testTriviaData == null)
                    testTriviaData = CreateTestTriviaData();

                return testTriviaData;
            }

            return triviaData;
        }
    }

    private void Awake()
    {
        button = GetComponent<Button>();

        if (animatedTarget == null)
            animatedTarget = GetComponent<RectTransform>();

        if (selectedBorder != null)
            selectedBorder.SetActive(false);

        if (button != null)
            button.onClick.AddListener(HandleClick);

        if (triviaButton != null)
            triviaButton.onClick.AddListener(HandleTriviaClick);

        if (usarDatosDePrueba)
            testTriviaData = CreateTestTriviaData();
    }

    public void Init(MovieSelectorController selectorController)
    {
        controller = selectorController;
    }

    private void HandleClick()
    {
        if (controller == null)
            return;

        controller.SelectMovie(this);
    }

    private void HandleTriviaClick()
    {
        if (controller == null)
            return;

        controller.OpenTriviaForMovie(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedBorder != null)
            selectedBorder.SetActive(selected);

        if (isSelected && !isSelectionAnimating)
            AnimateScale(normalScale, hoverDuration);
    }

    public void PlaySelectionAnimation()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(SelectionAnimationRoutine());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected || isSelectionAnimating)
            return;

        AnimateScale(hoverScale, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected || isSelectionAnimating)
            return;

        AnimateScale(normalScale, hoverDuration);
    }

    private void AnimateScale(float targetScale, float duration)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale, duration));
    }

    private IEnumerator SelectionAnimationRoutine()
    {
        isSelectionAnimating = true;

        yield return ScaleRoutine(pressedScale, pressDuration);
        yield return ScaleRoutine(normalScale, releaseDuration);

        isSelectionAnimating = false;
        scaleRoutine = null;
    }

    private IEnumerator ScaleRoutine(float targetScale, float duration)
    {
        Vector3 startScale = animatedTarget.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            animatedTarget.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        animatedTarget.localScale = endScale;
    }

    private MovieTriviaData CreateTestTriviaData()
    {
        MovieTriviaData data = new MovieTriviaData();
        data.questions = new List<TriviaQuestionData>();

        data.questions.Add(new TriviaQuestionData
        {
            question = "¿Cuál de las siguientes acciones sí forma parte de las funciones de la CONDUSEF?",
            answers = new List<TriviaAnswerData>
            {
                new TriviaAnswerData
                {
                    answerText = "Resolver controversias entre usuarios e instituciones financieras.",
                    isCorrect = true,
                    wrongJustification = ""
                },
                new TriviaAnswerData
                {
                    answerText = "Autorizar la apertura de nuevos bancos comerciales.",
                    isCorrect = false,
                    wrongJustification = "Esa función no corresponde a CONDUSEF. La CONDUSEF orienta y defiende a usuarios financieros."
                },
                new TriviaAnswerData
                {
                    answerText = "Emitir tarjetas de crédito directamente a los usuarios.",
                    isCorrect = false,
                    wrongJustification = "CONDUSEF no emite productos financieros. Su función es proteger y orientar a los usuarios."
                },
                new TriviaAnswerData
                {
                    answerText = "Fijar diariamente el tipo de cambio del peso.",
                    isCorrect = false,
                    wrongJustification = "El tipo de cambio no es definido por CONDUSEF. Su labor está enfocada en usuarios financieros."
                }
            }
        });

        data.questions.Add(new TriviaQuestionData
        {
            question = "¿Qué hábito ayuda a tener mejor control financiero?",
            answers = new List<TriviaAnswerData>
            {
                new TriviaAnswerData
                {
                    answerText = "Registrar ingresos y gastos de forma constante.",
                    isCorrect = true,
                    wrongJustification = ""
                },
                new TriviaAnswerData
                {
                    answerText = "Comprar sin revisar el presupuesto disponible.",
                    isCorrect = false,
                    wrongJustification = "Comprar sin revisar el presupuesto puede provocar descontrol financiero y endeudamiento."
                },
                new TriviaAnswerData
                {
                    answerText = "Usar todo el crédito disponible cada mes.",
                    isCorrect = false,
                    wrongJustification = "Usar todo el crédito disponible puede afectar tu capacidad de pago y generar intereses."
                },
                new TriviaAnswerData
                {
                    answerText = "No revisar estados de cuenta.",
                    isCorrect = false,
                    wrongJustification = "Revisar estados de cuenta ayuda a detectar cargos, errores y hábitos de consumo."
                }
            }
        });

        data.questions.Add(new TriviaQuestionData
        {
            question = "¿Para qué sirve comparar productos financieros?",
            answers = new List<TriviaAnswerData>
            {
                new TriviaAnswerData
                {
                    answerText = "Para elegir la opción que mejor se adapta a tus necesidades.",
                    isCorrect = true,
                    wrongJustification = ""
                },
                new TriviaAnswerData
                {
                    answerText = "Para contratar siempre el producto más caro.",
                    isCorrect = false,
                    wrongJustification = "El producto más caro no siempre es el mejor. Lo importante es comparar costos, beneficios y condiciones."
                },
                new TriviaAnswerData
                {
                    answerText = "Para evitar leer contratos.",
                    isCorrect = false,
                    wrongJustification = "Leer contratos es necesario para conocer obligaciones, comisiones y condiciones."
                },
                new TriviaAnswerData
                {
                    answerText = "Para ignorar las comisiones.",
                    isCorrect = false,
                    wrongJustification = "Las comisiones son parte importante del costo real de un producto financiero."
                }
            }
        });

        data.questions.Add(new TriviaQuestionData
        {
            question = "¿Qué acción puede ayudarte a evitar problemas con tus finanzas?",
            answers = new List<TriviaAnswerData>
            {
                new TriviaAnswerData
                {
                    answerText = "Leer condiciones antes de contratar un producto financiero.",
                    isCorrect = true,
                    wrongJustification = ""
                },
                new TriviaAnswerData
                {
                    answerText = "Aceptar cualquier crédito sin revisar intereses.",
                    isCorrect = false,
                    wrongJustification = "Antes de aceptar un crédito debes revisar intereses, plazos, comisiones y tu capacidad de pago."
                },
                new TriviaAnswerData
                {
                    answerText = "Compartir contraseñas bancarias con otras personas.",
                    isCorrect = false,
                    wrongJustification = "Compartir contraseñas pone en riesgo tu información y tu dinero."
                },
                new TriviaAnswerData
                {
                    answerText = "Ignorar cargos desconocidos.",
                    isCorrect = false,
                    wrongJustification = "Los cargos desconocidos deben revisarse y reportarse lo antes posible."
                }
            }
        });

        return data;
    }
}