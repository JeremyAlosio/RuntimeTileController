using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(TilemapSaveData))]
public class TileObject : MonoBehaviour
{
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

    public ItemType ItemTypeValue;
    public bool CanBePlacedOnFloor;
    public bool CanBePlacedOnWall;
    public bool CanBePlacedUnderFurniture;
    public bool HasCollision;

    public Vector3 PlacementOffset;
    public float MinorAdjustmentIncrement = 0.001f;


    GameObject _currentlySelectedObject;
    private Tilemap _objectTilemap;
    private TilemapManager _tilemapManager ;
    private Grid _grid;
    private bool _isFullyOverTilemap = false;
    private SelectionMode _currentMode = SelectionMode.None;

    private Vector3 _newPosition;

    void Awake()
    {
        _objectTilemap = GetComponent<Tilemap>();
        _tilemapManager = GetComponentInParent<TilemapManager>();
        _grid = GetComponentInParent<Grid>();

        TileObjectSelected.AddListener(OnTileObjectSelected);
    }

    void OnDestroy()
    {
        TileObjectSelected.RemoveListener(OnTileObjectSelected);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && _currentlySelectedObject == null)
        {
            if (IsMouseOverObject())
            {
                Debug.Log("Selected: " + name);
                _currentlySelectedObject = transform.gameObject;
                _currentMode = SelectionMode.FullMove;
                TileObjectSelected?.Invoke();
            }
        } 
        else if (Input.GetMouseButtonUp(0) && _currentlySelectedObject == transform.gameObject)
        {
            if(_isFullyOverTilemap)
                ExitEditMode();
        }
        else if (Input.GetMouseButtonUp(1) && _currentlySelectedObject == transform.gameObject)
        {
            SwitchMode();
        }
        if (_currentlySelectedObject == transform.gameObject)
        {
            HandleMovementInput();
        }
    }


   //#################################### Object Selection ##########################################################
    bool IsMouseOverObject()
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


    void OnTileObjectSelected()
    {        
        if (_currentlySelectedObject != transform.gameObject)
        {
            _currentlySelectedObject = null;
            _currentMode = SelectionMode.None;
        }
        else {
            _tilemapManager.SetCurrentlySelectedObject(_currentlySelectedObject);
        }
    }

    //#################################### Object Setting ##########################################################

    void SwitchMode()
    {
        if (_currentMode == SelectionMode.None || _currentMode == SelectionMode.MinorAdjustment)
        {
            _currentMode = SelectionMode.FullMove;
        }
        else if (_currentMode == SelectionMode.FullMove)
        {
            _newPosition = transform.position;
            _currentMode = SelectionMode.MinorAdjustment;
            _objectTilemap.color = Color.yellow;
        }
    }

    void ExitEditMode() {
        _currentlySelectedObject = null;
        GetComponent<TilemapSaveData>().UpdatePosition(transform.localPosition);
        _objectTilemap.color = Color.white;
        _currentMode = SelectionMode.None;
    }

    // ####################################################################################################################


    //#################################### Object Movement ##########################################################

    void HandleMovementInput()
    {
        if (_currentMode == SelectionMode.FullMove)
        {
            HandleMouseMovement();
        }
        else if(_currentMode == SelectionMode.MinorAdjustment)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            mousePosition.z = 0f;

            Vector3 movement;

            if (_currentMode == SelectionMode.MinorAdjustment)
            {
                movement = (mousePosition - transform.localPosition).normalized * MinorAdjustmentIncrement;
                Vector3 cellSize = _grid.cellSize;
                Vector3 maxMovement = cellSize / 2f;
                transform.localPosition += movement;
                // Ensure movement does not exceed half of the cell size
                transform.localPosition = new Vector3(
                    Mathf.Clamp(transform.localPosition.x, _newPosition.x - maxMovement.x, _newPosition.x + maxMovement.x),
                    Mathf.Clamp(transform.localPosition.y, _newPosition.y - maxMovement.y, _newPosition.y + maxMovement.y),
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

        _isFullyOverTilemap = CheckObjectBounds();
    }

    Vector3 SnapPositionToGrid(Vector3 position)
    {
        BoundsInt bounds = _objectTilemap.cellBounds;

        Vector3 totalPosition = Vector3.zero;
        int tileCount = 0;

        foreach (Vector3Int tilePosition in bounds.allPositionsWithin)
        {
            if (_objectTilemap.HasTile(tilePosition))
            {
                totalPosition += _objectTilemap.CellToWorld(tilePosition) + PlacementOffset;
                tileCount++;
            }
        }

        Vector3 centerOfTiles = tileCount > 0 ? totalPosition / tileCount : Vector3.zero;
        Vector3Int cellPosition = _grid.WorldToCell(position);
        Vector3 snappedPosition = _grid.GetCellCenterWorld(cellPosition);
        snappedPosition -= centerOfTiles - transform.position;

        return snappedPosition;
    }

    // ####################################################################################################################





    //#################################### Object Bounds ##########################################################

    bool CheckObjectBounds()
    {
        BoundsInt bounds = _objectTilemap.cellBounds;
        bool allTilesValid = true;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (!_objectTilemap.HasTile(position))
                continue;

            Vector3 worldPosition = _objectTilemap.CellToWorld(position);

            //TODO: Figure out why this is necessary for Even x bound objects Objects
            if(GetComponent<TilemapCollider2D>().bounds.size.x % 2 != 0)
            {
                worldPosition += PlacementOffset;
            }
            else {
                if(worldPosition.x > 0) {
                    worldPosition.x += PlacementOffset.x;
                }
                if(worldPosition.y > 0) {
                    worldPosition.y += PlacementOffset.y;
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

        _objectTilemap.color = allTilesValid ? Color.green : Color.red;
        return allTilesValid;
    }

    bool CheckLayerRules(Transform sibling)
    {
        if (sibling.TryGetComponent<TileRoomProperty>(out var siblingTileRoomProperty))
        {
            if (siblingTileRoomProperty.layerDescriptor == TileRoomProperty.LayerDescriptor.FloorLayer && !CanBePlacedOnFloor)
            {
                return false;
            }
            if (siblingTileRoomProperty.layerDescriptor == TileRoomProperty.LayerDescriptor.WallLayer && !CanBePlacedOnWall)
            {
                return false;
            }
        }

        if (sibling.TryGetComponent<TileObject>(out var siblingTileObject))
        {
            if (siblingTileObject.ItemTypeValue == ItemType.Furniture && ItemTypeValue == ItemType.Furniture)
            {
                if(siblingTileObject.CanBePlacedUnderFurniture || CanBePlacedUnderFurniture)
                    return true;
                Debug.Log("Can't place Furniture on other Furniture!");
                return false;
            }
            if (siblingTileObject.ItemTypeValue == ItemType.Device && ItemTypeValue == ItemType.Device)
            {
                return false;
            }
        }

        return true;
    }

}
