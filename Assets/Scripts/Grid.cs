using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public enum DropType
    {
        EMPTY,
        NORMAL
    };

    [System.Serializable]
    public struct DropPrefab
    {
        public DropType type;
        public GameObject prefab;
    };


    [SerializeField]
    private int rows = 8;
    [SerializeField]
    private int cols = 8;
    [SerializeField]
    private float tileSize = 1;
    [SerializeField]
    public float moveTime = 0.5f;
    private float fillTime = 0f;

    public GameObject tilePrefab;
    public GameObject buttonPrefab;
    private List<Button> buttonList; //spawner checkbox


    public DropPrefab[] dropPrefabs;
    private Dictionary<DropType, GameObject> dropPrefabDict;

    public Drop[,] drops;
    //private bool inverse = false;
    private Drop pressedDrop;
    private Drop enteredDrop;
    private List<Drop> matchingDrops;
    private List<Drop> horizontalDrops;
    private List<Drop> verticalDrops;
    private Vector2 firstPressPos;
    private Vector2 secondPressPos;
    private Vector2 currentSwipe;
    public bool pressed = false;


    public int dropEmptyPoolSize;
    public int dropNormalPoolSize;
    public List<GameObject> dropEmptyPool = new List<GameObject>();
    public List<GameObject> dropNormalPool = new List<GameObject>();


    void Awake()
    {
        GenerateGrid();
        GenerateDrops();
        fillTime = 0;
        StartFill();
    }


    private void GenerateGrid()  // Create background tiles
    {

        Camera.main.orthographicSize = (cols / 2f) / Screen.width * Screen.height;

        buttonList = new List<Button>();
        for (int col = 0; col < cols; col++)
        {
            GameObject button1 = (GameObject)Instantiate(buttonPrefab, transform);
            float posX1 = col;
            float posY1 = rows + 1;
            button1.transform.localPosition = new Vector2(posX1, posY1);

            buttonList.Add(button1.GetComponent<Button>());

            for (int row = 0; row < rows; row++)
            {
                GameObject tile = (GameObject)Instantiate(tilePrefab, transform);
                float posX = col * tileSize;
                float posY = row * tileSize;
                tile.transform.localPosition = new Vector2(posX, posY);
            }
        }
        float gridW = cols * tileSize;
        float gridH = rows * tileSize;
        transform.position = new Vector2(-gridW / 2 + (tileSize / 2), -gridH / 2 + (tileSize / 2));
    }

    private void GenerateDrops()  // Generate and Pooling Drops
    {
        dropPrefabDict = new Dictionary<DropType, GameObject>();

        for (int i = 0; i < dropPrefabs.Length; i++)
        {
            if (!dropPrefabDict.ContainsKey(dropPrefabs[i].type))
            {
                dropPrefabDict.Add(dropPrefabs[i].type, dropPrefabs[i].prefab);
            }
        }
        drops = new Drop[cols, rows];
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                SpawnNewDrop(col, row, DropType.EMPTY);
            }
        }

        for (int i = 0; i < dropNormalPoolSize; i++)
        {
            GameObject newDrop = (GameObject)Instantiate(dropPrefabDict[DropType.NORMAL], GetWorldPosition(100, 100), Quaternion.identity);
            newDrop.transform.parent = transform;
            newDrop.SetActive(false);
            dropNormalPool.Add(newDrop);
        }
    }

    public void StartFill()
    {
        bool needsRefill = true;
        while (needsRefill)
        {
            while (FillStep())
            {

            }
            needsRefill = ClearAllValidMatches();
        }
    }

    public void Update()  //Swipe Control
    {
        if (Input.GetMouseButtonDown(0))
        {
            firstPressPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pressed = true;

            RaycastHit2D hit = Physics2D.Raycast(firstPressPos, Vector2.zero);
            if (hit.collider != null && hit.collider.tag == "Drop")
            {
                PressDrop(hit.transform.GetComponent<Drop>());
            }

        }
        if (Input.GetMouseButtonUp(0))
        {
            pressed = false;
            secondPressPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentSwipe = secondPressPos - firstPressPos;
            currentSwipe = new Vector2(secondPressPos.x - firstPressPos.x, secondPressPos.y - firstPressPos.y);
            currentSwipe.Normalize();

            RaycastHit2D hit = Physics2D.Raycast(firstPressPos, Vector2.zero);
           
            if (hit.collider != null && hit.collider.tag == "Button")
            {
                hit.transform.GetComponent<Button>().Press();
            }
            if (pressedDrop)
            {
                if (currentSwipe.y > 0.5f && currentSwipe.x > -0.5f && currentSwipe.x < 0.5f && pressedDrop.Y > 0)
                {
                    EnterDrop(drops[pressedDrop.X, pressedDrop.Y - 1]);
                    ReleaseDrop();
                }

                if (currentSwipe.y < 0.5f && currentSwipe.x > -0.5f && currentSwipe.x < 0.5f && pressedDrop.Y < rows - 1)
                {
                    EnterDrop(drops[pressedDrop.X, pressedDrop.Y + 1]);
                    ReleaseDrop();
                }

                if (currentSwipe.x < 0.5f && currentSwipe.y > -0.5f && currentSwipe.y < 0.5f && pressedDrop.X > 0)
                {
                    EnterDrop(drops[pressedDrop.X - 1, pressedDrop.Y]);
                    ReleaseDrop();
                }

                if (currentSwipe.x > 0.5f && currentSwipe.y > -0.5f && currentSwipe.y < 0.5f && pressedDrop.X < cols - 1)
                {
                    EnterDrop(drops[pressedDrop.X + 1, pressedDrop.Y]);
                    ReleaseDrop();
                }
            }
        }
       
    }

    public Vector2 GetWorldPosition(int x, int y) // Get Drops position
    {
        return new Vector2(transform.position.x + x, transform.position.y + rows - 1 - y);
    }

    public Drop SpawnNewDrop(int x, int y, DropType type) // Empty Drop Spawn
    {
        GameObject newDrop = (GameObject)Instantiate(dropPrefabDict[type], GetWorldPosition(x, y), Quaternion.identity);
        newDrop.transform.parent = transform;
        if (type == DropType.EMPTY) { dropEmptyPool.Add(newDrop); newDrop.transform.name = "Empty_" + x.ToString() + " - " + y.ToString(); }
        drops[x, y] = newDrop.GetComponent<Drop>();
        drops[x, y].Init(x, y, this, type);
        return drops[x, y];
    }


    public Drop GetPoolDrop(int x, int y, DropType type) //Get Drop From Pool
    {
        GameObject newDrop = null;
        for (int i = 0; i < dropEmptyPoolSize; i++)
        {
            if (!dropEmptyPool[i].activeInHierarchy)
            {
                newDrop = dropEmptyPool[i].gameObject;
                break;
            }
        }
        newDrop.SetActive(true);
        newDrop.transform.parent = transform;
        drops[x, y] = newDrop.GetComponent<Drop>();
        drops[x, y].Init(x, y, this, type);
        return drops[x, y];
    }


    public IEnumerator Fill() // Fill Grid with Normal Drops
    {
        bool needsRefill = true;
        while (needsRefill)
        {
            yield return new WaitForSeconds(fillTime * 2f);
            while (FillStep())
            {
                yield return new WaitForSeconds(fillTime);
            }
            needsRefill = ClearAllValidMatches();
        }
    }

    public bool FillStep()// Fill Grid 
    {
        bool movedDrop = false;

        for (int y = rows - 2; y >= 0; y--)
        {
            for (int loopX = 0; loopX < cols; loopX++)
            {
                int x = loopX;
                Drop drop = drops[x, y];
                if (drop.IsMovable())
                {
                    Drop dropBelow = drops[x, y + 1];
                    if (dropBelow.Type == DropType.EMPTY)
                    {
                        dropBelow.gameObject.SetActive(false);

                        drop.MoveableComponent.Move(x, y + 1, fillTime);
                        drops[x, y + 1] = drop;
                        GetPoolDrop(x, y, DropType.EMPTY);
                        movedDrop = true;
                    }
                }
            }
        }
        for (int x = 0; x < cols; x++)
        {
            Drop dropBelow = drops[x, 0];
            if (dropBelow.Type == DropType.EMPTY)
            {
                if (buttonList[x].Pressed == true)
                {
                    dropBelow.gameObject.SetActive(false);
                    GameObject newDrop = null;
                    for (int i = 0; i < dropNormalPoolSize; i++)
                    {
                        if (!dropNormalPool[i].activeInHierarchy)
                        {
                            newDrop = dropNormalPool[i].gameObject;
                            break;
                        }
                    }
                    newDrop.transform.position = GetWorldPosition(x, -1);
                    newDrop.transform.localScale = new Vector3(1, 1, 1);
                    newDrop.SetActive(true);
                    drops[x, 0] = newDrop.GetComponent<Drop>();
                    drops[x, 0].Init(x, -1, this, DropType.NORMAL);
                    drops[x, 0].ClearableComponent.Refresh();
                    drops[x, 0].MoveableComponent.Move(x, 0, fillTime);
                    drops[x, 0].ColorComponent.SetColor((ColorDrop.ColorType)Random.Range(0, drops[x, 0].ColorComponent.NumColors));
                    movedDrop = true;
                }
            }
        }
        return movedDrop;
    }


    public bool IsAdjacent(Drop drop1, Drop drop2) // Adjacent Control
    {
        return (drop1.X == drop2.X && (int)Mathf.Abs(drop1.Y - drop2.Y) == 1) || (drop1.Y == drop2.Y && (int)Mathf.Abs(drop1.X - drop2.X) == 1);
    }


    public void SwapDrops(Drop drop1, Drop drop2)  // Swap Drops 
    {
        fillTime = moveTime;
        if (drop1.IsMovable() && drop2.IsMovable())
        {
            drops[drop1.X, drop1.Y] = drop2;
            drops[drop2.X, drop2.Y] = drop1;
            if (GetMatch(drop1, drop2.X, drop2.Y) != null || GetMatch(drop2, drop1.X, drop1.Y) != null)
            {
                int drop1X = drop1.X;
                int drop1Y = drop1.Y;
                drop1.MoveableComponent.Move(drop2.X, drop2.Y, fillTime);
                LeanTween.scale(drop1.gameObject, new Vector3(1.75f, 1.75f, 1.75f), fillTime / 1.5f).setLoopPingPong(1);
                drop2.MoveableComponent.Move(drop1X, drop1Y, fillTime);
                LeanTween.scale(drop2.gameObject, new Vector3(1.75f, 1.75f, 1.75f), fillTime / 1.5f).setLoopPingPong(1);

                LeanTween.delayedCall(fillTime * 1.5f, () =>
                {
                    ClearAllValidMatches();
                    LeanTween.delayedCall(fillTime * 1.5f, () =>
                    {
                        StartCoroutine(Fill());
                    });
                });

            }
            else
            {
                drops[drop1.X, drop1.Y] = drop1;
                drops[drop2.X, drop2.Y] = drop2;

                int drop1X = drop1.X;
                int drop1Y = drop1.Y;
                drop1.MoveableComponent.Move(drop2.X, drop2.Y, fillTime);
                drop2.MoveableComponent.Move(drop1X, drop1Y, fillTime);
                LeanTween.delayedCall(fillTime * 1.1f, () =>
                {
                    drop1X = drop1.X;
                    drop1Y = drop1.Y;
                    drop1.MoveableComponent.Move(drop2.X, drop2.Y, fillTime);
                    drop2.MoveableComponent.Move(drop1X, drop1Y, fillTime);
                });

            }


        }
    }


    public void PressDrop(Drop _drop)
    {
        pressedDrop = _drop;
    }
    public void EnterDrop(Drop _drop)
    {
        enteredDrop = _drop;
    }

    public void ReleaseDrop()
    {
        if (IsAdjacent(pressedDrop, enteredDrop))
        {
            SwapDrops(pressedDrop, enteredDrop);
        }
        pressedDrop = null;
        enteredDrop = null;
    }

    public List<Drop> GetMatch(Drop drop, int newX, int newY)  // Control All Matches
    {
        if (drop.IsColored())
        {
            matchingDrops = new List<Drop>();
            horizontalDrops = new List<Drop>();
            verticalDrops = new List<Drop>();

            if (HorizontalMatch(drop, newX, newY) != null)
            {

                return HorizontalMatch(drop, newX, newY);
            }
            if (VerticalMatch(drop, newX, newY) != null)
            {
                return VerticalMatch(drop, newX, newY);
            }
        }
        return null;

    }



    private List<Drop> HorizontalMatch(Drop drop, int newX, int newY) // Horizontal Match Control
    {
        horizontalDrops.Clear();
        verticalDrops.Clear();
        ColorDrop.ColorType color = drop.ColorComponent.Color;
        horizontalDrops.Add(drop);
        for (int dir = 0; dir <= 1; dir++)
        {
            for (int xOffset = 1; xOffset < cols; xOffset++)
            {
                int x;
                if (dir == 0) { x = newX - xOffset; }
                else { x = newX + xOffset; }
                if (x < 0 || x >= cols) { break; }
                if (drops[x, newY].IsColored() && drops[x, newY].ColorComponent.Color == color)
                {
                    horizontalDrops.Add(drops[x, newY]);
                }
                else { break; }
            }
        }
        if (horizontalDrops.Count >= 3)
        {
            for (int i = 0; i < horizontalDrops.Count; i++)
            {
                matchingDrops.Add(horizontalDrops[i]);
            }
        }

        if (horizontalDrops.Count >= 3)
        {
            for (int i = 0; i < horizontalDrops.Count; i++)
            {
                matchingDrops.Add(horizontalDrops[i]);
            }
        }


        if (horizontalDrops.Count >= 3)
        {
            for (int i = 0; i < horizontalDrops.Count; i++)
            {
                for (int dir = 0; dir <= 1; dir++)
                {
                    for (int yOffset = 1; yOffset < rows; yOffset++)
                    {
                        int y;
                        if (dir == 0) { y = newY - yOffset; }
                        else { y = newY + yOffset; }
                        if (y < 0 || y >= rows) { break; }
                        if (drops[horizontalDrops[i].X, y].IsColored() && drops[horizontalDrops[i].X, y].ColorComponent.Color == color)
                        {
                            verticalDrops.Add(drops[horizontalDrops[i].X, y]);
                        }
                        else { break; }
                    }
                }
                if (verticalDrops.Count < 2)
                {
                    verticalDrops.Clear();
                }
                else
                {
                    for (int j = 0; j < verticalDrops.Count; j++)
                    {
                        matchingDrops.Add(verticalDrops[j]);
                    }
                    break;
                }
            }
        }
        if (matchingDrops.Count >= 3)
        {
            return matchingDrops;
        }
        return null;
    }

    private List<Drop> VerticalMatch(Drop drop, int newX, int newY)// Vertical Match Control
    {
        horizontalDrops.Clear();
        verticalDrops.Clear();
        ColorDrop.ColorType color = drop.ColorComponent.Color;
        verticalDrops.Add(drop);
        for (int dir = 0; dir <= 1; dir++)
        {
            for (int yOffset = 1; yOffset < rows; yOffset++)
            {
                int y;
                if (dir == 0) { y = newY - yOffset; }
                else { y = newY + yOffset; }

                if (y < 0 || y >= rows) { break; }

                if (drops[newX, y].IsColored() && drops[newX, y].ColorComponent.Color == color)
                {
                    verticalDrops.Add(drops[newX, y]);
                }
                else { break; }
            }
        }
        if (verticalDrops.Count >= 3)
        {
            for (int i = 0; i < verticalDrops.Count; i++)
            {
                matchingDrops.Add(verticalDrops[i]);
            }
        }

        if (verticalDrops.Count >= 3)
        {
            for (int i = 0; i < verticalDrops.Count; i++)
            {
                for (int dir = 0; dir <= 1; dir++)
                {
                    for (int xOffset = 1; xOffset < cols; xOffset++)
                    {
                        int x;
                        if (dir == 0) { x = newX - xOffset; }
                        else { x = newX + xOffset; }
                        if (x < 0 || x >= cols) { break; }
                        if (drops[x, verticalDrops[i].Y].IsColored() && drops[x, verticalDrops[i].Y].ColorComponent.Color == color)
                        {
                            horizontalDrops.Add(drops[x, verticalDrops[i].Y]);
                        }
                        else { break; }
                    }
                }
                if (horizontalDrops.Count < 2) { horizontalDrops.Clear(); }
                else
                {
                    for (int j = 0; j < horizontalDrops.Count; j++)
                    {
                        matchingDrops.Add(horizontalDrops[j]);
                    }
                    break;
                }
            }
        }
        if (matchingDrops.Count >= 3)
        {
            return matchingDrops;
        }
        return null;
    }



    public bool ClearAllValidMatches() // Clear All Matches in Grid
    {
        bool needsRefill = false;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (drops[x, y].IsClearable())
                {
                    List<Drop> match = GetMatch(drops[x, y], x, y);
                    if (match != null)
                    {
                        for (int i = 0; i < match.Count; i++)
                        {
                            if (ClearDrop(match[i].X, match[i].Y))
                            {
                                needsRefill = true;
                            }
                        }
                    }
                }

            }
        }
        return needsRefill;
    }
    public bool ClearDrop(int x, int y) // Release drop to pool and locate empty pool drop
    {
        if (drops[x, y].IsClearable() && !drops[x, y].ClearableComponent.IsBeingCleared)
        {
            drops[x, y].ClearableComponent.Clear(fillTime);
            GetPoolDrop(x, y, DropType.EMPTY);
            return true;
        }
        return false;
    }


}