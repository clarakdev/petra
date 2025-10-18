using System;

[Serializable]
public class SaveData
{
    public int version = 1;

    //Main data to save 
    public string currentScene;   
    public int currency;          
    public string selectedPetId;  //Selected pet

    //misc. data to save later (inventory etc.)
    public string lastSavedUtc;   //For debug / timestamp
}
