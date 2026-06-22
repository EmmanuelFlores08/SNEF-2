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

    [SerializeField] private Button NextCharacterButton;
    [SerializeField] private Button PreviousCharacterButton;

    [SerializeField] private CharacterSelector characterSelector;

    private void Awake()
    {
        HatButton.onClick.AddListener(() => {
            characterSelector.GetActiveCharacter().ChangeBodyPart(CustomizationCatalog.BodyPartType.Hat);
        });

        PantsButton.onClick.AddListener(() => {
            characterSelector.GetActiveCharacter().ChangeBodyPart(CustomizationCatalog.BodyPartType.Pants);
        });

        ShoesButton.onClick.AddListener(() => {
            characterSelector.GetActiveCharacter().ChangeBodyPart(CustomizationCatalog.BodyPartType.Shoes);
        });

        AccesoriesButton.onClick.AddListener(() => {
            characterSelector.GetActiveCharacter().ChangeBodyPart(CustomizationCatalog.BodyPartType.Accesories);
        });

        NextCharacterButton.onClick.AddListener(() => {
            characterSelector.NextCharacter();
        });

        PreviousCharacterButton.onClick.AddListener(() => {
            characterSelector.PreviousCharacter();
        });
    }
}