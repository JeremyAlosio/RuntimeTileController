using System;
using UnityEngine;

public class TilemapSaveData : MonoBehaviour
{
    [ES3Serializable]
    public string prefabName;

    [ES3Serializable]
    public Vector3 position;
    [ES3Serializable]
    public string uniqueID; // Unique identifier for each instance

    void Awake()
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = name;
        }

        // Generate a new UUID if one does not already exist
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }

    public void UpdatePosition(Vector3 newPosition) {
        position = newPosition;
    }
}