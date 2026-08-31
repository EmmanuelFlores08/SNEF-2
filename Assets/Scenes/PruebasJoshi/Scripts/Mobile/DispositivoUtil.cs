using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Detección centralizada de dispositivo (móvil/tablet vs escritorio).
/// Usa la MISMA lógica que SpriteSegunDispositivo, incluido el puente
/// JavaScript para WebGL (donde Application.isMobilePlatform no es confiable).
/// </summary>
public static class DispositivoUtil
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int SNEF_EsMovilOTablet();
#endif

    /// <summary>
    /// Devuelve true si el juego corre en móvil o tablet.
    /// </summary>
    /// <param name="forzarMovilEnEditor">Simula móvil dentro del Editor para probar.</param>
    public static bool EsMovilOTablet(bool forzarMovilEnEditor = false)
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
