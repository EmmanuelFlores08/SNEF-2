using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private PlayerCharacterCustomized[] characters; // los personajes ya en escena
    private int currentIndex = 0;

    private void Start(){
        ShowCharacter(currentIndex);
    }

    public void NextCharacter(){
        currentIndex = (currentIndex + 1) % characters.Length;
        ShowCharacter(currentIndex);
    }

    public void PreviousCharacter(){
        currentIndex = (currentIndex - 1 + characters.Length) % characters.Length;
        ShowCharacter(currentIndex);
    }

    public PlayerCharacterCustomized GetActiveCharacter(){
        return characters[currentIndex];
    }

    private void ShowCharacter(int index){
        for (int i = 0; i < characters.Length; i++){
            characters[i].gameObject.SetActive(i == index);
        }
    }
}