using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class SpriteSegunDispositivo : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Sprite normal de escritorio, por ejemplo con la tecla E.")]
    [SerializeField] private Sprite spriteEscritorio;

    [Tooltip("Sprite para celular/tablet, por ejemplo sin la tecla E.")]
    [SerializeField] private Sprite spriteMovil;

    [Header("Referencia")]
    [SerializeField] private Image imagenUI;

    [Header("Pruebas")]
    [Tooltip("Actívalo para simular celular directamente desde el Editor.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int SNEF_EsMovilOTablet();
#endif

    private void Awake()
    {
        // Si no lo conectas manualmente,
        // busca automáticamente el Image de este objeto.
        if (imagenUI == null)
            imagenUI = GetComponent<Image>();
    }

    private void Start()
    {
        ActualizarSprite();
    }

    public void ActualizarSprite()
    {
        if (imagenUI == null)
        {
            Debug.LogWarning(
                "SpriteSegunDispositivo: No se encontró un componente Image.",
                this
            );
            return;
        }

        bool esMovil = EsMovilOTablet();

        if (esMovil)
        {
            if (spriteMovil != null)
                imagenUI.sprite = spriteMovil;
        }
        else
        {
            if (spriteEscritorio != null)
                imagenUI.sprite = spriteEscritorio;
        }

        // Mantiene correctamente el aspecto del sprite.
        imagenUI.preserveAspect = true;
    }

    private bool EsMovilOTablet()
    {
#if UNITY_EDITOR

        return forzarMovilEnEditor;

#elif UNITY_WEBGL

        return SNEF_EsMovilOTablet() == 1;

#else

        return Application.isMobilePlatform ||
               SystemInfo.deviceType == DeviceType.Handheld;

#endif
    }
}