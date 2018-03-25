using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Extensions;
using UnityEngine;

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

        camera.orthographicSize = verticalSize > horizontalSize ? verticalSize : horizontalSize;
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
        return x >= 0 && y < Width
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
        if(!IsDirecNeighbor(clicked, target)) return;
        
        var gemA = allGameGems[clicked.xIndex, clicked.yIndex];
        var gemB = allGameGems[target.xIndex, target.yIndex];

        gemA.Move(gemB.xIndex, gemB.yIndex, 0.1f);
        gemB.Move(gemA.xIndex, gemA.yIndex, 0.1f);
    }
    
    

    private bool IsDirecNeighbor(TileBehaviour tileA, TileBehaviour tileB)
    {
        return (Mathf.Abs(tileA.xIndex - tileB.xIndex) == 1 && tileA.yIndex == tileB.yIndex)
            || (Mathf.Abs(tileA.yIndex - tileB.yIndex) == 1 && tileA.xIndex == tileB.xIndex);
    }

    #endregion
}