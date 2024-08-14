using TMPro;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    public TextMeshProUGUI text;

    void OnEnable()
    {
        // Subscribe to the static event in TileObject
        TileObject.TileObjectSelected.AddListener(OnTileObjectSelected);
    }

    void OnDisable()
    {
        // Unsubscribe from the static event in TileObject
        TileObject.TileObjectSelected.RemoveListener(OnTileObjectSelected);
    }

    // This function will be called when a tile object is selected
    void OnTileObjectSelected()
    {
        // Get the currently selected object
        GameObject selectedObject = FindObjectOfType<TilemapManager>().CurrentlySelectedObject; //Sorry I'm lazy it's a fuckin Debug 

        if (selectedObject != null)
        {
            // Update the text with the name of the selected object
            text.text = selectedObject.name;
        }
        else
        {
            // Clear the text if no object is selected
            text.text = "No Object Selected";
        }
    }
}
