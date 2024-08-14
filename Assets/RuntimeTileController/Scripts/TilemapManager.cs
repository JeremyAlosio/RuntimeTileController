using Sirenix.OdinInspector;
using UnityEngine;

public class TilemapManager : MonoBehaviour
{
    public GameObject CurrentlySelectedObject { get; set; }
    public GameObject ObjectToSpawn { get; set; }

    public void SetCurrentlySelectedObject(GameObject selectedObject) {
        CurrentlySelectedObject = selectedObject;
    }

    [Button]
    public void DestroyCurrentlySelectedObject() {
        Destroy(CurrentlySelectedObject.gameObject);
    }

    public void CreateNewObject() {
        Instantiate(ObjectToSpawn, transform.position, Quaternion.identity, transform);
    }
}
