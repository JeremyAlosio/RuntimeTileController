using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TileRoomProperty : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }

    // Enum to describe the layer
    public enum LayerDescriptor
    {
        FloorLayer,
        WallLayer
    }

    public LayerDescriptor layerDescriptor;

}
