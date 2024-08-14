using TMPro;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void OnEnable()
    {
        // Subscribe to the static event in TileObjectProperty
        TileObjectProperty.TileObjectSelected.AddListener(OnTileObjectSelected);
    }

    private void OnDisable()
    {
        // Unsubscribe from the static event in TileObjectProperty
        TileObjectProperty.TileObjectSelected.RemoveListener(OnTileObjectSelected);
    }

    // This function will be called when a tile object is selected
    private void OnTileObjectSelected()
    {
        // Get the currently selected object
        GameObject selectedObject = TileObjectProperty.GetCurrentlySelectedObject();

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
