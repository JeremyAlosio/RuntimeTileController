using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TilemapSaveManager : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs; // Reference to all possible prefabs

    private const string UniqueIDsKey = "TilemapUniqueIDs";

    [Button]
    public void SaveTilemapData()
    {
        List<string> uniqueIDs = new List<string>();
        TilemapSaveData[] saveDataList = FindObjectsOfType<TilemapSaveData>();
        foreach (var saveData in saveDataList)
        {
            ES3.Save($"Tilemap_{saveData.uniqueID}_prefabName", saveData.prefabName);
            ES3.Save($"Tilemap_{saveData.uniqueID}_position", saveData.position);
            ES3.Save($"Tilemap_{saveData.uniqueID}_uniqueID", saveData.uniqueID);
            uniqueIDs.Add(saveData.uniqueID);
        }

        ES3.Save(UniqueIDsKey, uniqueIDs);
    }

    [Button]
    public void LoadTilemapData()
    {
        List<string> uniqueIDs = GetSavedUniqueIDs();

        TilemapSaveData[] activeObjects = FindObjectsOfType<TilemapSaveData>();
        foreach (var obj in activeObjects)
        {
            if (uniqueIDs.Contains(obj.uniqueID))
            {
                Destroy(obj.gameObject);
            }
        }

        foreach (var uniqueID in uniqueIDs)
        {
            string prefabName = ES3.Load<string>($"Tilemap_{uniqueID}_prefabName");
            Vector3 position = ES3.Load<Vector3>($"Tilemap_{uniqueID}_position");
            string loadedUniqueID = ES3.Load<string>($"Tilemap_{uniqueID}_uniqueID");
            
            GameObject prefab = GetPrefabByName(prefabName);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
                instance.GetComponent<TilemapSaveData>().uniqueID = loadedUniqueID;
                instance.GetComponent<TilemapSaveData>().prefabName = prefabName;
                instance.GetComponent<TilemapSaveData>().position = position;
            }
            else
            {
                Debug.LogError($"Prefab with name '{prefabName}' could not be loaded.");
            }
        }
    }

    // Retrieve all unique IDs from saved data
    private List<string> GetSavedUniqueIDs()
    {
        // Load the list of unique IDs
        if (ES3.KeyExists(UniqueIDsKey))
        {
            return ES3.Load<List<string>>(UniqueIDsKey);
        }
        else
        {
            // Return an empty list if no unique IDs are found
            return new List<string>();
        }
    }

    private GameObject GetPrefabByName(string prefabName)
    {
        foreach (var prefab in prefabs)
        {
            if (prefab.name == prefabName)
            {
                return prefab;
            }
        }
        Debug.LogError($"Prefab with name '{prefabName}' not found.");
        return null;
    }
}
