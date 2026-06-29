using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Customization/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string avatarId;
        public PlayerCharacterCustomized prefab;
    }

    public Entry[] characters;

    public PlayerCharacterCustomized GetPrefab(string avatarId)
    {
        foreach (var e in characters)
        {
            if (e.avatarId == avatarId) return e.prefab;
        }
        return null;
    }
}