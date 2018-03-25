using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Extensions;
using UnityEngine;
using System.Linq;
using DefaultNamespace;
using Random = UnityEngine.Random;

public class BoardBehaviour : MonoBehaviour
{
    public int Width;
    public int Height;
    public int BorderSize;
    public int MaxGemTypes;
    public GameObject TilePrefab;
    public GameObject[] GemPrefabs;
    public Transform TileContainer;
    public Transform GemContainer;

    private const float GEM_MOVE_TIME = 0.2f;
    private const float MATCH_CHECK_DELAY = 0.1f;
    private const int MAX_FILL_ATTEMPTS = 200;
    
    

    private TileBehaviour[,] tiles;
    private GameGemBehaviour[,] allGameGems;

    private TileBehaviour clickedTile;
    private TileBehaviour targetTile;


    // Use this for initialization
    void Start()
    {
        tiles = new TileBehaviour[Width, Height];
        allGameGems = new GameGemBehaviour[Width, Height];

        SetupCamera();
        SetupTiles();

        FillBoard();
    }

    #region Setup

    private void SetupTiles()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                tiles[i, j] =
                    Instantiate(TilePrefab,
                                new Vector3(i, j), Quaternion.identity, parent: TileContainer)
                       .SetName(string.Format("Tile [{0}:{1}]", i, j))
                       .GetComponent<TileBehaviour>()
                       .Init(i, j, this);
            }
        }
    }


    private void SetupCamera()
    {
        var camera = Camera.main;

        camera.gameObject.SetPosition(
            (float) (Width - 1) / 2,
            (float) (Height - 1) / 2,
            -10f);

        var aspectRatio = (float) Screen.width / Screen.height;
        var verticalSize = (float) Height / 2 + BorderSize;
        var horizontalSize = (float) Width / 2 + BorderSize / aspectRatio;

        camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    private void FillBoard()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                var attempts = 0;

                var index = new BoardIndex(i, j);
                do{
                    if (attempts > 0){
                        ClearGemAt(index);

                        if (attempts == MAX_FILL_ATTEMPTS){
                            throw new Exception("Max attempts to fill the board reached");
                        }
                    }

                    FillRandomGemAt(index);
                    attempts++;
                } while (HasMatchOnFill(index));
            }
        }
    }

    private GameGemBehaviour FillRandomGemAt(BoardIndex index)
    {
        return PlaceGem(
            gem: Instantiate(
                     GetRandomGemPrefab(),
                     Vector3.zero, Quaternion.identity, parent: GemContainer)
                .GetComponent<GameGemBehaviour>()
                .Init(this),
            x: index.BoardX,
            y: index.BoardY
        );
    }

    private bool HasMatchOnFill(BoardIndex index)
    {
        return FindMatches(index, Vector2.left).Count() >= 3
            || FindMatches(index, Vector2.down).Count() >= 3;
    }

    #endregion


    #region Gems Manipulation

    private GameObject GetRandomGemPrefab()
    {
        return GemPrefabs[
            Random.Range(0, MaxGemTypes)
        ];
    }

    public GameGemBehaviour PlaceGem(GameGemBehaviour gem, int x, int y)
    {
        gem.gameObject
           .ResetTransformation()
           .SetPosition(x, y);

        SetGemPosition(gem, x, y);

        return gem;
    }

    private void SetGemPosition(GameGemBehaviour gem, int x, int y)
    {
        allGameGems[x, y] = gem;
        gem.SetCord(x, y);
    }

    private bool IsInBounds(BoardIndex index)
    {
        return index.BoardX >= 0 && index.BoardX < Width
                                 && index.BoardY >= 0 && index.BoardY < Height;
    }

    private void ClearGemAt(BoardIndex index)
    {
        var gem = GetGem(index);
        if (gem != null){
            allGameGems[index.BoardX, index.BoardY] = null;
            Destroy(gem.gameObject);
        }
    }

    private void ClearGems(IEnumerable<GameGemBehaviour> matches)
    {
        foreach (var gem in matches){
            ClearGemAt(gem.Index);
        }
    }

    private void ClearBoard()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                ClearGemAt(
                    new BoardIndex(i, j)
                );
            }
        }
    }

    private List<GameGemBehaviour> CollapseColumns(IEnumerable<GameGemBehaviour> gems)
    {
        var movingGems = new List<GameGemBehaviour>();
        
        var columns = GetColumns(gems);
        foreach (var column in columns){
            movingGems.AddRange(
                CollapseColumn(column)
            );
        }

        return movingGems;
    }
    
    // TODO refactor!
    private List<GameGemBehaviour> CollapseColumn(int column)
    {
        var movingGems =  new List<GameGemBehaviour>();

        for (var i = 0; i < Height - 1; i++){
            if (GetGem(column, i) != null){
                continue;
            }

            for (var j = i + 1; j < Height; j++){
                var gem = GetGem(column, j);
                if (gem == null || movingGems.Contains(gem)){
                    continue;
                }

                gem.Move(new BoardIndex(column, i), GEM_MOVE_TIME);
                
                SetGemPosition(gem, column, i);
                movingGems.Add(gem);
                
                allGameGems[column, j] = null;
                break;
            }
        }

        return movingGems;
    }
    
    #endregion


    #region Gameplay

    public void ClickTile(TileBehaviour tile)
    {
        if (clickedTile == null){
            clickedTile = tile;
        }
    }

    public void DragToTile(TileBehaviour tile)
    {
        if (clickedTile != null){
            targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if (clickedTile != null && targetTile != null){
            SwapTiles(clickedTile, targetTile);
        }

        clickedTile = null;
        targetTile = null;
    }

    private void SwapTiles(TileBehaviour clicked, TileBehaviour target)
    {
        if (!IsDirecNeighbor(clicked.Index, target.Index)){
            return;
        }

        StartCoroutine(
            SwapTilesRoutine(
                GetGem(clicked.Index),
                GetGem(target.Index))
        );
    }

    private IEnumerator SwapTilesRoutine(GameGemBehaviour gemA, GameGemBehaviour gemB)
    {
        var indexA = gemA.Index.Clone();
        var indexB = gemB.Index.Clone();

        gemA.Move(indexB, GEM_MOVE_TIME);
        gemB.Move(indexA, GEM_MOVE_TIME);

        yield return new WaitForSeconds(GEM_MOVE_TIME);

        var matches = FindMatchesAt(indexA).Union(FindMatchesAt(indexB));

        if (matches.Any()){
            yield return new WaitForSeconds(MATCH_CHECK_DELAY);
            ClearGems(matches);
            CollapseColumns(matches);

        } else{
            gemA.Move(indexA, GEM_MOVE_TIME);
            gemB.Move(indexB, GEM_MOVE_TIME);
        }
    }


    private static bool IsDirecNeighbor(BoardIndex indexA, BoardIndex indexB)
    {
        return Mathf.Abs(indexA.BoardX - indexB.BoardX) == 1 && indexA.BoardY == indexB.BoardY
            || Mathf.Abs(indexA.BoardY - indexB.BoardY) == 1 && indexA.BoardX == indexB.BoardX;
    }

    #endregion


    #region Matching

    private IEnumerable<GameGemBehaviour> FindMatches(BoardIndex startIndex, Vector2 searchDirection)
    {
        var matches = new List<GameGemBehaviour>();

        var startGem = GetGem(startIndex);
        if (startGem == null){
            throw new NullReferenceException("Start gem can't be null");
        }

        matches.Add(startGem);

        var maxValue = Mathf.Max(Width, Height);

        for (var i = 1; i < maxValue - 1; i++){
            var index = new BoardIndex(
                startIndex.BoardX + (int) searchDirection.x * i,
                startIndex.BoardY + (int) searchDirection.y * i
            );

            if (!IsInBounds(index)){
                break;
            }

            var gem = GetGem(index);

            if (gem != null && gem.MatchType == startGem.MatchType){
                matches.Add(gem);
            } else{
                break;
            }
        }

        return matches;
    }


    private List<GameGemBehaviour> FindVerticalMatches(BoardIndex index)
    {
        return FindAndCombineMatches(index, direction1: Vector2.down, direction2: Vector2.up);
    }

    private List<GameGemBehaviour> FindHorizontalMatches(BoardIndex index)
    {
        return FindAndCombineMatches(index, direction1: Vector2.left, direction2: Vector2.right);
    }

    private List<GameGemBehaviour> FindAndCombineMatches(BoardIndex index, Vector2 direction1, Vector2 direction2)
    {
        return
            FindMatches(index, searchDirection: direction1)
               .Union(
                    FindMatches(index, searchDirection: direction2))
               .ToList();
    }

    private IEnumerable<GameGemBehaviour> FindMatchesAt(BoardIndex index)
    {
        var hMatches = FindHorizontalMatches(index);
        var vMatches = FindVerticalMatches(index);

        hMatches = hMatches.Count >= 3 ? hMatches : new List<GameGemBehaviour>();
        vMatches = vMatches.Count >= 3 ? vMatches : new List<GameGemBehaviour>();

        return hMatches.Count + vMatches.Count > 0
                   ? hMatches.Union(vMatches).ToList()
                   : new List<GameGemBehaviour>();
    }

    #endregion


    #region Diagnostics

    private void HighlightMatches()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                HighlightMatchesAt(
                    new BoardIndex(i, j)
                );
            }
        }
    }


    private void HighlightMatchesAt(BoardIndex index)
    {
        var matches = FindMatchesAt(index);

        foreach (var gem in matches){
            HighlightTile(gem.Index);
        }
    }

    private void HighlightTile(BoardIndex index)
    {
        GetTile(index)
           .GetComponent<SpriteRenderer>()
           .SetEnabled(true)
           .SetColor(
                GetGem(index)
                   .GetComponent<SpriteRenderer>().color
            );
    }

    #endregion


    #region Indexing

    private TileBehaviour GetTile(BoardIndex index)
    {
        return IsInBounds(index)
                   ? tiles[index.BoardX, index.BoardY]
                   : null;
    }

    private GameGemBehaviour GetGem(int xIndex, int yIndex)
    {
        return GetGem(new BoardIndex(xIndex, yIndex));
    }

    private GameGemBehaviour GetGem(BoardIndex index)
    {
        return IsInBounds(index)
                   ? allGameGems[index.BoardX, index.BoardY]
                   : null;
    }

    private HashSet<int> GetColumns(IEnumerable<GameGemBehaviour> gems)
    {
        var columns = new HashSet<int>();
        foreach (var gem in gems){
            columns.Add(gem.Index.BoardX);
        }
        
        return columns;
    }
    
    #endregion
}