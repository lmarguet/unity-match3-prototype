using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Extensions;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class BoardBehaviour : MonoBehaviour
{
    public int Width;
    public int Height;
    public int BorderSize;
    public GameObject TilePrefab;
    public GameObject[] GemPrefabs;
    public Transform TileContainer;
    public Transform GemContainer;

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

        FillRandom();
        HighlightMatches();
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

    private void FillRandom()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                PlaceGem(
                    gem: Instantiate(
                            GetRandomGemPrefab(),
                            Vector3.zero, Quaternion.identity, parent: GemContainer)
                        .GetComponent<GameGemBehaviour>()
                        .Init(this),
                    x: i, y: j
                );
            }
        }
    }

    #endregion


    #region Gems Manipulation

    private GameObject GetRandomGemPrefab()
    {
        return GemPrefabs[
            Random.Range(0, GemPrefabs.Length)
        ];
    }

    public void PlaceGem(GameGemBehaviour gem, int x, int y)
    {
        gem.gameObject
            .ResetTransformation()
            .SetPosition(x, y);

        allGameGems[x, y] = gem;
        gem.SetCord(x, y);
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width
            && y >= 0 && y < Height;
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
        if (!IsDirecNeighbor(clicked, target)) return;

        var gemA = GetGem(clicked.xIndex, clicked.yIndex);
        var gemB = GetGem(target.xIndex, target.yIndex);

        gemA.Move(gemB.xIndex, gemB.yIndex, 0.1f);
        gemB.Move(gemA.xIndex, gemA.yIndex, 0.1f);
    }


    private bool IsDirecNeighbor(TileBehaviour tileA, TileBehaviour tileB)
    {
        return (Mathf.Abs(tileA.xIndex - tileB.xIndex) == 1 && tileA.yIndex == tileB.yIndex)
            || (Mathf.Abs(tileA.yIndex - tileB.yIndex) == 1 && tileA.xIndex == tileB.xIndex);
    }

    #endregion


    #region Matching

    List<GameGemBehaviour> FindMatches(int xIndex, int yIndex, Vector2 searchDirection)
    {
        var matches = new List<GameGemBehaviour>();

        var startGem = GetGem(xIndex, yIndex);
        if (startGem == null){
            throw new NullReferenceException("Start gem is null");
        }

        matches.Add(startGem);

        var maxValue = Mathf.Max(Width, Height);

        for (var i = 1; i < maxValue - 1; i++){
            xIndex += (int) searchDirection.x;
            yIndex += (int) searchDirection.y;

            if (!IsInBounds(xIndex, yIndex)){
                break;
            }

            var gem = GetGem(xIndex, yIndex);

            if (gem.MatchType == startGem.MatchType){
                matches.Add(gem);
            }
            else{
                break;
            }
        }

        return matches;
    }


    private List<GameGemBehaviour> FindVerticalMatches(int xIndex, int yIndex)
    {
        return FindAndCombineMatches(xIndex, yIndex, direction1:Vector2.down, direction2:Vector2.up);
    }

    private List<GameGemBehaviour> FindHorizontalMatches(int xIndex, int yIndex)
    {
        return FindAndCombineMatches(xIndex, yIndex, direction1:Vector2.left, direction2:Vector2.right);
    }

    private List<GameGemBehaviour> FindAndCombineMatches(int xIndex, int yIndex, Vector2 direction1, Vector2 direction2)
    {
        return
            FindMatches(xIndex, yIndex, searchDirection: direction1)
                .Union(
                    FindMatches(xIndex, yIndex, searchDirection: direction2))
                .ToList();
    }

    #endregion

    #region Diagnostics

    private void HighlightMatches()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                var hMatches = FindHorizontalMatches(i, j);
                var vMatches = FindVerticalMatches(i, j);

                if (hMatches.Count < 3){
                    hMatches = new List<GameGemBehaviour>();
                }

                if (vMatches.Count < 3){
                    vMatches = new List<GameGemBehaviour>();
                }

                if (hMatches.Count == 0 && vMatches.Count == 0) continue;
                ;

                var combined = hMatches.Union(vMatches).ToList();

                foreach (var gem in combined){
                    var tileRender =
                        GetTile(gem.xIndex, gem.yIndex)
                            .GetComponent<SpriteRenderer>();

                    tileRender.enabled = true;
                    tileRender.color = gem.GetComponent<SpriteRenderer>().color;
                }
            }
        }
    }

    #region Indexing

    private TileBehaviour GetTile(int xIndex, int yIndex)
    {
        return IsInBounds(xIndex, yIndex)
            ? tiles[xIndex, yIndex]
            : null;
    }

    private GameGemBehaviour GetGem(int xIndex, int yIndex)
    {
        return IsInBounds(xIndex, yIndex)
            ? allGameGems[xIndex, yIndex]
            : null;
    }

    #endregion

    #endregion
}