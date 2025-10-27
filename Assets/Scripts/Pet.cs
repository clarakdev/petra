using UnityEngine;
using System;

[Serializable]
public class Pet
{
    // Keep the original names used by your teammates
    public string name;                 // used by other scripts (like PetSelectionManager & BattleSpawner)
    public Sprite cardImage;            // used for UI selection
    public GameObject prefab;           // used for spawning in main scene
    public GameObject battlePrefab;     // used for spawning in battle scene

    // Optional aliases (for your newer scripts)
    public string petName => name;      // allows PetAccessoryManager to still use petName
    public GameObject petPrefab => prefab; // allows PetSelectionManager to use petPrefab safely
}
