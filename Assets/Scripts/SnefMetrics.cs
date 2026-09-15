using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class SnefMetrics
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SnefMetric(string tipo, string sujeto);
#endif

    public static void Send(string tipo, string sujeto)
    {
        if (string.IsNullOrEmpty(tipo) || string.IsNullOrEmpty(sujeto))
        {
            Debug.LogWarning($"[SNEF Metrics] Evento ignorado por datos incompletos. tipo='{tipo}', sujeto='{sujeto}'");
            return;
        }

        try
        {
#if UNITY_EDITOR
            Debug.Log($"[SNEF Metrics] Editor envio simulado. tipo='{tipo}', sujeto='{sujeto}'");
#elif UNITY_WEBGL
            SnefMetric(tipo, sujeto);
#endif
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SNEF Metrics] No se pudo enviar la metrica. tipo='{tipo}', sujeto='{sujeto}'. {exception.Message}");
        }
    }
}
