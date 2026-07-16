using UnityEngine;
using UnityEngine.UI;

public class KitPreviewPanel : MonoBehaviour
{
    [Header("Imagen del fondo grande")]
    [SerializeField] private Image backgroundImage;

    [Header("Imágenes de los objetos del kit")]
    [SerializeField] private Image[] objectImages; // 2 imágenes (o las que tenga el kit)

    public void ShowKit(PhotoKitCatalog.PhotoKit kit)
    {
        gameObject.SetActive(true);

        // Fondo
        if (backgroundImage != null)
        {
            backgroundImage.sprite = kit.backgroundSprite;
            backgroundImage.enabled = (kit.backgroundSprite != null);
            backgroundImage.preserveAspect = true;
        }

        // Objetos
        for (int i = 0; i < objectImages.Length; i++)
        {
            if (objectImages[i] == null) continue;

            if (kit.objects != null && i < kit.objects.Length && kit.objects[i].previewSprite != null)
            {
                objectImages[i].sprite = kit.objects[i].previewSprite;
                objectImages[i].enabled = true;
                objectImages[i].preserveAspect = true;
            }
            else
            {
                objectImages[i].enabled = false; // ese slot no tiene objeto/imagen
            }
        }
    }

    public void Clear()
    {
        if (backgroundImage != null) backgroundImage.enabled = false;

        foreach (var img in objectImages)
            if (img != null) img.enabled = false;
    }
}