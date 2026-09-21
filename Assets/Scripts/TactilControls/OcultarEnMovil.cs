using UnityEngine;
using System.Runtime.InteropServices;

public class OcultarEnMovil : MonoBehaviour
{
    [Header("Elemento a ocultar")]
    [Tooltip("Si se deja vacío, se ocultará el mismo GameObject que contiene este script.")]
    [SerializeField] private GameObject elementoAOcultar;

    [Header("Pruebas")]
    [Tooltip("Permite simular un celular desde el Editor.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int SNEF_EsMovilOTablet();
#endif

    private void Start()
    {
        if (elementoAOcultar == null)
            elementoAOcultar = gameObject;

        if (EsMovilOTablet())
            elementoAOcultar.SetActive(false);
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