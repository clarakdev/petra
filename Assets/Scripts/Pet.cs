using UnityEngine;
using System;

[Serializable]
public class Pet
{
    public string name;
    public Sprite cardImage; // For UI selection
    public GameObject prefab; // For main scene
    public GameObject battlePrefab; // For battle scene
}