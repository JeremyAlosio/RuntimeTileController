using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(TilemapSaveData))]
public class TileObjectProperty : MonoBehaviour
{
    private static GameObject currentlySelectedObject;
    public static UnityEvent TileObjectSelected = new UnityEvent();

    public enum ItemType
    {
        Furniture,
        Device
    }

    public enum SelectionMode
    {
        None,
        FullMove,
        MinorAdjustment
    }

    public ItemType itemType;
    public bool canBePlacedOnFloor;
    public bool canBePlacedOnWall;
    public bool canBePlacedUnderFurniture;
    public bool canBeWalkedOver;
    public bool hasCollision;

    public Vector3 placementOffset;
    public float minorAdjustmentIncrement = 0.01f; // Increment for minor adjustments

    private Tilemap objectTilemap;
    private TilemapCollider2D tilemapCollider;
    private Grid grid;
    private bool isFullyOverTilemap = false;
    private SelectionMode currentMode = SelectionMode.None;

    private Vector3 newPosition;

    void Awake()
    {
        objectTilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        grid = GetComponentInParent<Grid>();

        TileObjectSelected.AddListener(OnTileObjectSelected);
    }

    void OnDestroy()
    {
        TileObjectSelected.RemoveListener(OnTileObjectSelected);
    }

    public void ToggleSelectionMode()
    {
        if (currentlySelectedObject = transform.gameObject)
        {
            Debug.Log("Switching Mode");
            SwitchMode();
        }
        else
        {
            Debug.Log("Selected: " + name);
            currentlySelectedObject = transform.gameObject;
            currentMode = SelectionMode.FullMove;
            TileObjectSelected?.Invoke();
        }
    }

    void OnTileObjectSelected()
    {        
        if (currentlySelectedObject != transform.gameObject)
        {
            currentlySelectedObject = null;
            currentMode = SelectionMode.None;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentlySelectedObject == null)
        {
            if (IsMouseOverObject())
            {
                ToggleSelectionMode();
            }
        } 
        else if (Input.GetMouseButtonUp(0) && currentlySelectedObject == transform.gameObject)
        {
            if(isFullyOverTilemap)
                ExitEditMode();
        }
        else if (Input.GetMouseButtonUp(1) && currentlySelectedObject == transform.gameObject)
        {
            ToggleSelectionMode();
        }
        if (currentlySelectedObject == transform.gameObject)
        {
            HandleMovementInput();
        }
    }

    void HandleMovementInput()
    {
        if (currentMode == SelectionMode.FullMove)
        {
            HandleMouseMovement();
        }
        else if(currentMode == SelectionMode.MinorAdjustment)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            mousePosition.z = 0f;

            Vector3 movement;

            if (currentMode == SelectionMode.MinorAdjustment)
            {
                movement = (mousePosition - transform.localPosition).normalized * minorAdjustmentIncrement;
                Vector3 cellSize = grid.cellSize;
                Vector3 maxMovement = cellSize / 2f;
                transform.localPosition += movement;
                // Ensure movement does not exceed half of the cell size
                transform.localPosition = new Vector3(
                    Mathf.Clamp(transform.localPosition.x, newPosition.x - maxMovement.x, newPosition.x + maxMovement.x),
                    Mathf.Clamp(transform.localPosition.y, newPosition.y - maxMovement.y, newPosition.y + maxMovement.y),
                    transform.localPosition.z
                );
            }
        }
    }

    void HandleMouseMovement()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = 0f;

        mousePosition = SnapPositionToGrid(mousePosition);
        transform.position = mousePosition;

        isFullyOverTilemap = CheckObjectBounds();
    }

    void SwitchMode()
    {
        if (currentMode == SelectionMode.None || currentMode == SelectionMode.MinorAdjustment)
        {
            currentMode = SelectionMode.FullMove;
        }
        else if (currentMode == SelectionMode.FullMove)
        {
            newPosition = transform.position;
            currentMode = SelectionMode.MinorAdjustment;
            objectTilemap.color = Color.yellow;
        }
    }

    void ExitEditMode() {
        currentlySelectedObject = null;
        GetComponent<TilemapSaveData>().UpdatePosition(transform.localPosition);
        objectTilemap.color = Color.white;
        currentMode = SelectionMode.None;
    }

    Vector3 SnapPositionToGrid(Vector3 position)
    {
        BoundsInt bounds = objectTilemap.cellBounds;

        Vector3 totalPosition = Vector3.zero;
        int tileCount = 0;

        foreach (Vector3Int tilePosition in bounds.allPositionsWithin)
        {
            if (objectTilemap.HasTile(tilePosition))
            {
                totalPosition += objectTilemap.CellToWorld(tilePosition) + placementOffset;
                tileCount++;
            }
        }

        Vector3 centerOfTiles = tileCount > 0 ? totalPosition / tileCount : Vector3.zero;
        Vector3Int cellPosition = grid.WorldToCell(position);
        Vector3 snappedPosition = grid.GetCellCenterWorld(cellPosition);
        snappedPosition -= centerOfTiles - transform.position;

        return snappedPosition;
    }

    bool CheckObjectBounds()
    {
        BoundsInt bounds = objectTilemap.cellBounds;
        bool allTilesValid = true;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (!objectTilemap.HasTile(position))
                continue;

            Vector3 worldPosition = objectTilemap.CellToWorld(position);

            //TODO: Figure out why this is necessary for Even x bound objects Objects
            if(GetComponent<TilemapCollider2D>().bounds.size.x % 2 != 0)
            {
                worldPosition += placementOffset;
            }
            else {
                if(worldPosition.x > 0) {
                    worldPosition.x += placementOffset.x;
                }
                if(worldPosition.y > 0) {
                    worldPosition.y += placementOffset.y;
                }
            }
            bool tileIsValid = false;

            foreach (Transform sibling in transform.parent)
            {
                if (sibling != transform)
                {
                    TilemapCollider2D siblingCollider = sibling.GetComponent<TilemapCollider2D>();
                    if (siblingCollider != null)
                    {
                        // Check if the world position of the tile is inside the sibling's bounds
                        if (siblingCollider.OverlapPoint(worldPosition))
                        {
                            if (CheckLayerRules(sibling))
                            {
                                tileIsValid = true;
                            }
                            else
                            {
                                tileIsValid = false;
                                break;
                            }
                        }
                    }
                }
            }

            if (!tileIsValid)
            {
                allTilesValid = false;
                break;
            }
        }

        objectTilemap.color = allTilesValid ? Color.green : Color.red;
        return allTilesValid;
    }

    bool CheckLayerRules(Transform sibling)
    {
        if (sibling.TryGetComponent<TileRoomProperty>(out var siblingTileRoomProperty))
        {
            if (siblingTileRoomProperty.layerDescriptor == TileRoomProperty.LayerDescriptor.FloorLayer && !canBePlacedOnFloor)
            {
                return false;
            }
            if (siblingTileRoomProperty.layerDescriptor == TileRoomProperty.LayerDescriptor.WallLayer && !canBePlacedOnWall)
            {
                return false;
            }
        }

        if (sibling.TryGetComponent<TileObjectProperty>(out var siblingTileObjectProperty))
        {
            if (siblingTileObjectProperty.itemType == ItemType.Furniture && itemType == ItemType.Furniture)
            {
                if(siblingTileObjectProperty.canBePlacedUnderFurniture || canBePlacedUnderFurniture)
                    return true;
                Debug.Log("Can't place Furniture on other Furniture!");
                return false;
            }
            if (siblingTileObjectProperty.itemType == ItemType.Device && itemType == ItemType.Device)
            {
                return false;
            }
        }

        return true;
    }


    private bool IsMouseOverObject()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        // Store the currently selected object and its order in layer
        GameObject topObject = null;
        int topOrderInLayer = int.MinValue;

        foreach (Transform sibling in transform.parent)
        {
            TilemapCollider2D siblingCollider = sibling.GetComponent<TilemapCollider2D>();
            TilemapRenderer siblingRenderer = sibling.GetComponent<TilemapRenderer>();

            if (siblingCollider != null && siblingRenderer != null && siblingCollider.OverlapPoint(mousePosition))
            {
                int siblingOrderInLayer = siblingRenderer.sortingOrder;

                if (siblingOrderInLayer > topOrderInLayer)
                {
                    topOrderInLayer = siblingOrderInLayer;
                    topObject = sibling.gameObject;
                }
            }
        }

        // Check if the topObject is the current object
        if (topObject == gameObject)
        {
            return true;
        }

        return false;
    }


    public static GameObject GetCurrentlySelectedObject()
    {
        return currentlySelectedObject;
    }
}
