using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilemapManager : MonoBehaviour
{
    public GameObject ObjectToSpawn { get; set; }

    public void CreateNewObject() {
        Instantiate(ObjectToSpawn, transform.position, Quaternion.identity, transform);
    }
}
