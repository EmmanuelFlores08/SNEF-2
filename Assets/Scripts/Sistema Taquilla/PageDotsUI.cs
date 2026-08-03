using UnityEngine;
using UnityEngine.UI;

public class PageDotsUI : MonoBehaviour
{
    [Header("Los puntos (en orden de izquierda a derecha)")]
    [SerializeField] private Image[] dots;

    [Header("Colores")]
    [SerializeField] private Color activeColor = new Color(0.4f, 0.9f, 0.3f, 1f);   // verde
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f); // gris

    // Enciende el punto de la página actual, apaga los demás, oculta los que sobren
    public void SetPages(int totalPages, int currentPage)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;

            if (i < totalPages)
            {
                dots[i].gameObject.SetActive(true);
                dots[i].color = (i == currentPage) ? activeColor : inactiveColor;
            }
            else
            {
                dots[i].gameObject.SetActive(false);
            }
        }
    }
}