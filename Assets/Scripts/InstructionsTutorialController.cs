using System.Collections.Generic;
using Controller;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsTutorialController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject instructionsRoot;
    [SerializeField] private Image slideImage;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button startTutorialButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button understoodButton;

    [Header("Slides")]
    [SerializeField] private Sprite welcomeSlide;
    [SerializeField] private Sprite controlsPcSlide;
    [SerializeField] private Sprite controlsMobileSlide;
    [SerializeField] private Sprite cinemaRoomsSlide;
    [SerializeField] private Sprite shopSlide;
    [SerializeField] private Sprite photoSetSlide;

    [Header("Input")]
    [SerializeField] private KeyCode openKey = KeyCode.Q;

    [Header("Gameplay lock")]
    [SerializeField] private GameObject touchControlsRoot;
    [SerializeField] private CursorLockManager cursorLockManager;
    [SerializeField] private MonoBehaviour[] extraComponentsToDisableWhileOpen;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Editor test")]
    [SerializeField] private bool forceMobileInEditor;

    private readonly List<BehaviourState> behaviourStates =
        new List<BehaviourState>();

    private readonly HashSet<Behaviour> savedBehaviours =
        new HashSet<Behaviour>();

    private bool isOpen;
    private bool autoShowEvaluated;
    private bool touchControlsStateSaved;
    private bool touchControlsWereActive;
    private bool cursorStateSaved;
    private bool cursorManagerWasInInterfaceMode;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private int currentSlideIndex;

    private const int WelcomeIndex = 0;
    private const int ControlsIndex = 1;
    private const int LastSlideIndex = 4;

    private void Awake()
    {
        ResolveMissingReferences();
        ConfigureButtonListeners();

        if (instructionsRoot != null)
            instructionsRoot.SetActive(false);
    }

    private void Start()
    {
        TryEvaluateAutomaticOpen();
    }

    private void OnEnable()
    {
        if (SnefBridge.Instance != null)
            SnefBridge.Instance.OnStateChanged += HandleSnefStateChanged;
    }

    private void OnDisable()
    {
        if (SnefBridge.Instance != null)
            SnefBridge.Instance.OnStateChanged -= HandleSnefStateChanged;
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey) && !isOpen)
            OpenTutorial();
    }

    public void OpenTutorial()
    {
        if (isOpen)
            return;

        ResolveMissingReferences();

        isOpen = true;
        currentSlideIndex = WelcomeIndex;

        if (instructionsRoot != null)
            instructionsRoot.SetActive(true);

        ApplyGameplayLock();
        ShowCurrentSlide();

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();
    }

    public void CloseTutorial()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (instructionsRoot != null)
            instructionsRoot.SetActive(false);

        RestoreGameplayLock();

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCerrarMenu();
    }

    [ContextMenu("Instructions/Open tutorial")]
    private void OpenTutorialFromContextMenu()
    {
        OpenTutorial();
    }

    [ContextMenu("Instructions/Close tutorial")]
    private void CloseTutorialFromContextMenu()
    {
        CloseTutorial();
    }

    private void HandleSnefStateChanged()
    {
        TryEvaluateAutomaticOpen();
    }

    private void TryEvaluateAutomaticOpen()
    {
        if (autoShowEvaluated)
            return;

        SnefBridge bridge = SnefBridge.Instance;
        if (bridge == null || !bridge.HasServerState ||
            !bridge.InitialServerStateReceived)
        {
            return;
        }

        autoShowEvaluated = true;

        if (bridge.IsGuest || bridge.InitialServerAvatarWasNull)
            OpenTutorial();
    }

    private void ConfigureButtonListeners()
    {
        RemoveButtonListeners();

        if (skipButton != null)
            skipButton.onClick.AddListener(CloseTutorial);

        if (startTutorialButton != null)
            startTutorialButton.onClick.AddListener(GoToControlsSlide);

        if (previousButton != null)
            previousButton.onClick.AddListener(GoToPreviousSlide);

        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNextSlide);

        if (understoodButton != null)
            understoodButton.onClick.AddListener(CloseTutorial);
    }

    private void RemoveButtonListeners()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(CloseTutorial);

        if (startTutorialButton != null)
            startTutorialButton.onClick.RemoveListener(GoToControlsSlide);

        if (previousButton != null)
            previousButton.onClick.RemoveListener(GoToPreviousSlide);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(GoToNextSlide);

        if (understoodButton != null)
            understoodButton.onClick.RemoveListener(CloseTutorial);
    }

    private void GoToControlsSlide()
    {
        currentSlideIndex = ControlsIndex;
        ShowCurrentSlide();
    }

    private void GoToPreviousSlide()
    {
        if (currentSlideIndex <= WelcomeIndex)
            return;

        currentSlideIndex--;
        ShowCurrentSlide();
    }

    private void GoToNextSlide()
    {
        if (currentSlideIndex >= LastSlideIndex)
            return;

        currentSlideIndex++;
        ShowCurrentSlide();
    }

    private void ShowCurrentSlide()
    {
        if (slideImage != null)
        {
            slideImage.sprite = GetSpriteForSlide(currentSlideIndex);
            slideImage.preserveAspect = true;
        }

        bool isWelcome = currentSlideIndex == WelcomeIndex;
        bool isLast = currentSlideIndex == LastSlideIndex;

        SetButtonVisible(skipButton, isWelcome);
        SetButtonVisible(startTutorialButton, isWelcome);
        SetButtonVisible(previousButton, !isWelcome);
        SetButtonVisible(nextButton, !isWelcome && !isLast);
        SetButtonVisible(understoodButton, isLast);
    }

    private Sprite GetSpriteForSlide(int index)
    {
        switch (index)
        {
            case WelcomeIndex:
                return welcomeSlide;
            case ControlsIndex:
                return IsMobileOrTablet() ? controlsMobileSlide : controlsPcSlide;
            case 2:
                return cinemaRoomsSlide;
            case 3:
                return shopSlide;
            case LastSlideIndex:
                return photoSetSlide;
            default:
                return welcomeSlide;
        }
    }

    private bool IsMobileOrTablet()
    {
        return DispositivoUtil.EsMovilOTablet(forceMobileInEditor);
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void ApplyGameplayLock()
    {
        SaveAndDisableGameplayBehaviours();

        if (playerRigidbody == null)
            playerRigidbody = FindFirstSceneObject<Rigidbody>();

        if (playerRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            playerRigidbody.linearVelocity = Vector3.zero;
#else
            playerRigidbody.velocity = Vector3.zero;
#endif
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        SaveAndHideTouchControls();
        SaveAndShowCursor();
    }

    private void RestoreGameplayLock()
    {
        for (int i = behaviourStates.Count - 1; i >= 0; i--)
        {
            BehaviourState state = behaviourStates[i];
            if (state.Behaviour != null)
                state.Behaviour.enabled = state.WasEnabled;
        }

        behaviourStates.Clear();
        savedBehaviours.Clear();

        RestoreTouchControls();
        RestoreCursor();
    }

    private void SaveAndDisableGameplayBehaviours()
    {
        behaviourStates.Clear();
        savedBehaviours.Clear();

        MovePlayerInput playerInput = FindFirstSceneObject<MovePlayerInput>();
        if (playerInput != null)
        {
            CharacterMover mover = playerInput.GetComponent<CharacterMover>();
            if (mover != null)
                mover.ResetToIdle();

            SaveAndDisable(playerInput);
            SaveAndDisable(mover);

            if (playerInput.Camera != null)
                SaveAndDisable(playerInput.Camera);
        }

        SaveAllAndDisable<CinemaRoomTrigger>();
        SaveAllAndDisable<TiendaObjetosTrigger>();
        SaveAllAndDisable<PhotoRoomTrigger>();
        SaveAllAndDisable<ChangeAvatarTrigger>();
        SaveAllAndDisable<AirHockeyInteraction>();

        if (extraComponentsToDisableWhileOpen == null)
            return;

        foreach (MonoBehaviour component in extraComponentsToDisableWhileOpen)
            SaveAndDisable(component);
    }

    private void SaveAllAndDisable<T>() where T : Behaviour
    {
        T[] components = FindSceneObjects<T>();
        foreach (T component in components)
            SaveAndDisable(component);
    }

    private void SaveAndDisable(Behaviour behaviour)
    {
        if (behaviour == null || behaviour == this ||
            savedBehaviours.Contains(behaviour))
        {
            return;
        }

        savedBehaviours.Add(behaviour);
        behaviourStates.Add(new BehaviourState(behaviour, behaviour.enabled));
        behaviour.enabled = false;
    }

    private void SaveAndHideTouchControls()
    {
        if (touchControlsRoot == null)
        {
            TouchControlsVisibility touchControls =
                FindFirstSceneObject<TouchControlsVisibility>();

            if (touchControls != null)
                touchControlsRoot = touchControls.gameObject;
        }

        if (touchControlsRoot == null || touchControlsStateSaved)
            return;

        touchControlsWereActive = touchControlsRoot.activeSelf;
        touchControlsStateSaved = true;
        touchControlsRoot.SetActive(false);
    }

    private void RestoreTouchControls()
    {
        if (touchControlsRoot == null || !touchControlsStateSaved)
            return;

        touchControlsRoot.SetActive(touchControlsWereActive);
        touchControlsStateSaved = false;
    }

    private void SaveAndShowCursor()
    {
        if (cursorStateSaved)
            return;

        if (cursorLockManager == null)
            cursorLockManager = FindFirstSceneObject<CursorLockManager>();

        cursorStateSaved = true;

        if (cursorLockManager != null)
        {
            cursorManagerWasInInterfaceMode =
                cursorLockManager.IsInInterfaceMode();

            cursorLockManager.SetInterfaceMode(true);
            return;
        }

        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreCursor()
    {
        if (!cursorStateSaved)
            return;

        if (cursorLockManager != null)
        {
            cursorLockManager.SetInterfaceMode(
                cursorManagerWasInInterfaceMode
            );
        }
        else
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }

        cursorStateSaved = false;
    }

    private void ResolveMissingReferences()
    {
        if (instructionsRoot == null)
        {
            GameObject foundRoot = FindSceneGameObject("Instrucciones");
            if (foundRoot != null)
                instructionsRoot = foundRoot;
        }

        if (instructionsRoot == null)
            return;

        Transform slide = FindChildRecursive(
            instructionsRoot.transform,
            "Slide"
        );

        if (slideImage == null && slide != null)
            slideImage = slide.GetComponent<Image>();

        if (skipButton == null)
            skipButton = FindButton("btn-omitir");

        if (startTutorialButton == null)
            startTutorialButton = FindButton("btn-verTutorial");

        if (previousButton == null)
            previousButton = FindButton("btn-anterior");

        if (nextButton == null)
            nextButton = FindButton("btn-siguiente");

        if (understoodButton == null)
            understoodButton = FindButton("btn-entendido");
    }

    private Button FindButton(string objectName)
    {
        Transform child = FindChildRecursive(
            instructionsRoot.transform,
            objectName
        );

        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
            return null;

        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in allObjects)
        {
            if (candidate.name == objectName &&
                candidate.scene.IsValid() &&
                candidate.hideFlags == HideFlags.None)
            {
                return candidate;
            }
        }

        return null;
    }

    private static T FindFirstSceneObject<T>() where T : Object
    {
        T[] objects = FindSceneObjects<T>();
        return objects.Length > 0 ? objects[0] : null;
    }

    private static T[] FindSceneObjects<T>() where T : Object
    {
        T[] allObjects = Resources.FindObjectsOfTypeAll<T>();
        List<T> sceneObjects = new List<T>();

        foreach (T candidate in allObjects)
        {
            if (candidate == null)
                continue;

            GameObject owner = null;

            if (candidate is GameObject gameObject)
                owner = gameObject;
            else if (candidate is Component component)
                owner = component.gameObject;

            if (owner != null &&
                owner.scene.IsValid() &&
                owner.hideFlags == HideFlags.None)
            {
                sceneObjects.Add(candidate);
            }
        }

        return sceneObjects.ToArray();
    }

    private readonly struct BehaviourState
    {
        public BehaviourState(Behaviour behaviour, bool wasEnabled)
        {
            Behaviour = behaviour;
            WasEnabled = wasEnabled;
        }

        public Behaviour Behaviour { get; }
        public bool WasEnabled { get; }
    }
}
