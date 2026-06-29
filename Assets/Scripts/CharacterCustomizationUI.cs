using UnityEngine;

public class CharacterCustomizationUI : MonoBehaviour
{
    [SerializeField] private ClothingCategoryUI[] categories; // Sombreros, Prendas, Calzado, Accesorios

    private PlayerCharacterCustomized character;

    public void SetCharacter(PlayerCharacterCustomized newCharacter)
    {
        character = newCharacter;
        foreach (var cat in categories)
            cat.SetCharacter(character);
    }
}