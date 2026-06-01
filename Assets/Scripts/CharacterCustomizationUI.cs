using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationUI : MonoBehaviour
{
    [SerializeField] private Button HatButton;
    [SerializeField] private Button PantsButton;  // ← AGREGADO: se usaba pero no estaba declarado
    [SerializeField] private Button ShoesButton;  // ← AGREGADO: se usaba pero no estaba declarado
    [SerializeField] private PlayerCharacterCustomized playerCharacterCustomized;

    private void Awake()
    {
        HatButton.onClick.AddListener(() => {
            Debug.Log("HatButton");
            playerCharacterCustomized.ChangeBodyPart(PlayerCharacterCustomized.BodyPartType.Hat);
        });

        PantsButton.onClick.AddListener(() => {
            Debug.Log("PantsButton");
            playerCharacterCustomized.ChangeBodyPart(PlayerCharacterCustomized.BodyPartType.Pants);
        });

        ShoesButton.onClick.AddListener(() => {
            Debug.Log("ShoesButton"); // ← CORREGIDO: decía "Pants Button" en el log de Shoes
            playerCharacterCustomized.ChangeBodyPart(PlayerCharacterCustomized.BodyPartType.Shoes);
        });
    }
}
