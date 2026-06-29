using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationUI : MonoBehaviour
{
    [SerializeField] private Button HatButton;
    [SerializeField] private Button PantsButton;
    [SerializeField] private Button ShoesButton;
    [SerializeField] private Button AccesoriesButton;

    private PlayerCharacterCustomized character;

    public void SetCharacter(PlayerCharacterCustomized newCharacter)
    {
        character = newCharacter;
    }

    private void Awake()
    {
        HatButton.onClick.AddListener(() => {
            if (character != null) character.ChangeBodyPart(CustomizationCatalog.BodyPartType.Hat);
        });

        PantsButton.onClick.AddListener(() => {
            if (character != null) character.ChangeBodyPart(CustomizationCatalog.BodyPartType.Pants);
        });

        ShoesButton.onClick.AddListener(() => {
            if (character != null) character.ChangeBodyPart(CustomizationCatalog.BodyPartType.Shoes);
        });

        AccesoriesButton.onClick.AddListener(() => {
            if (character != null) character.ChangeBodyPart(CustomizationCatalog.BodyPartType.Accesories);
        });
    }
}