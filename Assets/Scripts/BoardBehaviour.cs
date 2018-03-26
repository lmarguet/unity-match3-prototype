﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using Assets.Scripts.Extensions;
using UnityEngine;
using System.Linq;
using System.Runtime.Remoting.Channels;
using DefaultNamespace;
using Random = UnityEngine.Random;

public class BoardBehaviour : MonoBehaviour
{
    // TODO move to serialized
    public int Columns;
    public int Rows;
    public int Padding;
    public int MaxGemTypes;

    // TODO move to serialized
    public GameObject TilePrefab;
    public GameObject[] GemPrefabs;
    public Transform TileContainer;
    public Transform GemContainer;

    // TODO move to serialized
    public float GemMoveTime = 0.2f;
    public float GemFallTime = 0.25f;
    public float PlayerMatchCheckDelay = 0.1f;
    public float RefreshMatchCheckDelay = 0.35f;

    private const int MAX_FILL_ATTEMPTS = 200;
    private const int REFILL_Y_OFFSET = 5;

    private GridTileBehaviour[,] tiles;
    private GemBehaviour[,] allGems;
    private GridTileBehaviour clickedTile;
    private GridTileBehaviour targetTile;
    private bool playerControlsEnabled = true;

    // Use this for initialization
    void Start()
    {
        tiles = new GridTileBehaviour[Columns, Rows];
        allGems = new GemBehaviour[Columns, Rows];
        MaxGemTypes = Mathf.Clamp(MaxGemTypes, 3, GemPrefabs.Length);


        SetupCamera();
        SetupTiles();

        FillBoard();
    }

    #region Setup

    private void SetupTiles()
    {
        for (var i = 0; i < Columns; i++){
            for (var j = 0; j < Rows; j++){
                tiles[i, j] =
                    Instantiate(TilePrefab,
                                new Vector3(i, j), Quaternion.identity, parent: TileContainer)
                       .SetName(string.Format("Tile [{0}:{1}]", i, j))
                       .GetComponent<GridTileBehaviour>()
                       .Init(i, j, this);
            }
        }
    }


    private void SetupCamera()
    {
        var camera = Camera.main;

        camera.gameObject.SetPosition(
            (float) (Columns - 1) / 2,
            (float) (Rows - 1) / 2,
            -10f);

        var aspectRatio = (float) Screen.width / Screen.height;
        var verticalSize = (float) Rows / 2 + Padding;
        var horizontalSize = (float) Columns / 2 + Padding / aspectRatio;

        camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    private void FillBoard(bool isRefill = false)
    {
        for (var i = 0; i < Columns; i++){
            for (var j = 0; j < Rows; j++){
                var index = new GridIndex(i, j);
                if (GetGem(index) != null){
                    continue;
                }

                var yOffest = isRefill ? index.GridY + REFILL_Y_OFFSET : 0;
                var attempts = 0;
                do{
                    if (attempts > 0){
                        ClearGemAt(index);

                        if (attempts == MAX_FILL_ATTEMPTS){
                            throw new Exception("Max attempts to fill the board reached");
                        }
                    }

                    FillRandomGemAt(index, yOffest);
                    attempts++;
                } while (HasMatchOnFill(index));
            }
        }
    }

    private GemBehaviour FillRandomGemAt(GridIndex index, int yOffset = 0)
    {
        return
            PlaceGem(
                gem: Instantiate(
                         GetRandomGemPrefab(),
                         Vector3.zero, Quaternion.identity, parent: GemContainer)
                    .GetComponent<GemBehaviour>()
                    .Init(this),
                index: index
            ).ApplyOffsetY(yOffset, GemFallTime);
    }

    private bool HasMatchOnFill(GridIndex index)
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

    public GemBehaviour PlaceGem(GemBehaviour gem, GridIndex index)
    {
        gem.gameObject
           .ResetTransformation()
           .SetPosition(index.GridX, index.GridY);

        SetGemPosition(gem, index);
        return gem;
    }

    private void SetGemPosition(GemBehaviour gem, GridIndex index)
    {
        allGems[index.GridX, index.GridY] = gem;
        gem.SetCord(index);
    }

    private bool IsInBounds(GridIndex index)
    {
        return index.GridX >= 0 && index.GridX < Columns
                                && index.GridY >= 0 && index.GridY < Rows;
    }

    private void ClearGemAt(GridIndex index)
    {
        var gem = GetGem(index);
        if (gem != null){
            allGems[index.GridX, index.GridY] = null;
            Destroy(gem.gameObject);
        }
    }

    private void ClearGems(IEnumerable<GemBehaviour> matches)
    {
        foreach (var gem in matches){
            ClearGemAt(gem.Index);
        }
    }

    private void ClearBoard()
    {
        for (var i = 0; i < Columns; i++){
            for (var j = 0; j < Rows; j++){
                ClearGemAt(
                    new GridIndex(i, j)
                );
            }
        }
    }

    private IEnumerable<GemBehaviour> CollapseColumns(IEnumerable<GemBehaviour> gems)
    {
        var movingGems = new List<GemBehaviour>();

        var columns = GetColumns(gems);
        foreach (var column in columns){
            movingGems.AddRange(
                CollapseColumn(column)
            );
        }

        return movingGems;
    }

    // TODO refactor!
    private List<GemBehaviour> CollapseColumn(int column)
    {
        var movingGems = new List<GemBehaviour>();

        for (var i = 0; i < Rows - 1; i++){
            if (GetGem(column, i) != null){
                continue;
            }

            for (var j = i + 1; j < Rows; j++){
                var gem = GetGem(column, j);
                if (gem == null || movingGems.Contains(gem)){
                    continue;
                }

                var index = new GridIndex(column, i);
                gem.Move(index, GemFallTime * (j - i));

                SetGemPosition(gem, index);
                movingGems.Add(gem);

                allGems[column, j] = null;
                break;
            }
        }

        return movingGems;
    }


    private void ClearAndRefillBoard(IEnumerable<GemBehaviour> gems)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gems));
    }


    private IEnumerator ClearAndRefillBoardRoutine(IEnumerable<GemBehaviour> gems)
    {
        playerControlsEnabled = false;

        yield return new WaitForSeconds(PlayerMatchCheckDelay);

        yield return StartCoroutine(ClearAndCollapseRoutine(gems));
        yield return null;

        yield return StartCoroutine(RefillRoutine());

        playerControlsEnabled = true;
    }

    private IEnumerator ClearAndCollapseRoutine(IEnumerable<GemBehaviour> matches)
    {
        var complete = false;
        while (!complete){
            ClearGems(matches);

            yield return new WaitForSeconds(0.25f);

            var movedGems = CollapseColumns(matches);

            while (!IsFallComplete(movedGems)){
                yield return null;
            }

            yield return new WaitForSeconds(RefreshMatchCheckDelay);

            matches = FindMatchesAt(movedGems);

            if (!matches.Any()){
                complete = true;
                break;
            }


            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
        }
    }

    private IEnumerator RefillRoutine()
    {
        FillBoard(true);
        yield return null;
    }

    #endregion


    #region Gameplay

    public void ClickTile(GridTileBehaviour tile)
    {
        if (playerControlsEnabled && clickedTile == null){
            clickedTile = tile;
        }
    }

    public void DragToTile(GridTileBehaviour tile)
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

    private void SwapTiles(GridTileBehaviour clicked, GridTileBehaviour target)
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

    private IEnumerator SwapTilesRoutine(GemBehaviour gemA, GemBehaviour gemB)
    {
        var indexA = gemA.Index.Clone();
        var indexB = gemB.Index.Clone();

        gemA.Move(indexB, GemMoveTime);
        gemB.Move(indexA, GemMoveTime);

        yield return new WaitForSeconds(GemMoveTime);

        var matches =
            FindMatchesAt(indexA)
               .Union(FindMatchesAt(indexB));


        if (matches.Any()){
            yield return new WaitForSeconds(PlayerMatchCheckDelay);
            ClearAndRefillBoard(matches);
        } else{
            gemA.Move(indexA, GemMoveTime);
            gemB.Move(indexB, GemMoveTime);
        }
    }


    private static bool IsDirecNeighbor(GridIndex indexA, GridIndex indexB)
    {
        return Mathf.Abs(indexA.GridX - indexB.GridX) == 1 && indexA.GridY == indexB.GridY
            || Mathf.Abs(indexA.GridY - indexB.GridY) == 1 && indexA.GridX == indexB.GridX;
    }


    private bool IsFallComplete(IEnumerable<GemBehaviour> gems)
    {
        return gems.All(gem => !gem.IsMoving);
    }

    #endregion


    #region Matching

    private IEnumerable<GemBehaviour> FindMatches(GridIndex startIndex, Vector2 searchDirection)
    {
        var matches = new List<GemBehaviour>();

        var startGem = GetGem(startIndex);
        if (startGem == null){
            return Enumerable.Empty<GemBehaviour>();
        }

        matches.Add(startGem);

        var maxValue = Mathf.Max(Columns, Rows);

        for (var i = 1; i < maxValue - 1; i++){
            var index = new GridIndex(
                startIndex.GridX + (int) searchDirection.x * i,
                startIndex.GridY + (int) searchDirection.y * i
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


    private List<GemBehaviour> FindVerticalMatches(GridIndex index)
    {
        return FindAndCombineMatches(index, direction1: Vector2.down, direction2: Vector2.up);
    }

    private List<GemBehaviour> FindHorizontalMatches(GridIndex index)
    {
        return FindAndCombineMatches(index, direction1: Vector2.left, direction2: Vector2.right);
    }

    private List<GemBehaviour> FindAndCombineMatches(GridIndex index, Vector2 direction1, Vector2 direction2)
    {
        return
            FindMatches(index, searchDirection: direction1)
               .Union(
                    FindMatches(index, searchDirection: direction2))
               .ToList();
    }

    private IEnumerable<GemBehaviour> FindMatchesAt(GridIndex index)
    {
        var hMatches = FindHorizontalMatches(index);
        var vMatches = FindVerticalMatches(index);

        hMatches = hMatches.Count >= 3 ? hMatches : new List<GemBehaviour>();
        vMatches = vMatches.Count >= 3 ? vMatches : new List<GemBehaviour>();

        return hMatches.Any() || vMatches.Any()
                   ? hMatches.Union(vMatches)
                   : Enumerable.Empty<GemBehaviour>();
    }


    private IEnumerable<GemBehaviour> FindMatchesAt(IEnumerable<GemBehaviour> gems)
    {
        return gems.Aggregate(
            Enumerable.Empty<GemBehaviour>(),
            (result, gem) =>
                result.Union(
                    FindMatchesAt(gem.Index)
                )
        );
    }

    #endregion


    #region Diagnostics

    private void ClearAllHighlights()
    {
        for (var i = 0; i < Columns; i++){
            for (var j = 0; j < Rows; j++){
                GetTile(new GridIndex(i, j))
                   .GetComponent<SpriteRenderer>()
                   .SetEnabled(false);
            }
        }
    }

    private void HighlightMatches()
    {
        ClearAllHighlights();

        for (var i = 0; i < Columns; i++){
            for (var j = 0; j < Rows; j++){
                HighlightMatchesAt(
                    new GridIndex(i, j)
                );
            }
        }
    }


    private void HighlightMatchesAt(GridIndex index)
    {
        var matches = FindMatchesAt(index);

        foreach (var gem in matches){
            HighlightTile(gem.Index);
        }
    }

    private void HighlightTile(GridIndex index)
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

    private GridTileBehaviour GetTile(GridIndex index)
    {
        return IsInBounds(index)
                   ? tiles[index.GridX, index.GridY]
                   : null;
    }

    private GemBehaviour GetGem(int xIndex, int yIndex)
    {
        return GetGem(new GridIndex(xIndex, yIndex));
    }

    private GemBehaviour GetGem(GridIndex index)
    {
        return IsInBounds(index)
                   ? allGems[index.GridX, index.GridY]
                   : null;
    }

    private HashSet<int> GetColumns(IEnumerable<GemBehaviour> gems)
    {
        var columns = new HashSet<int>();
        foreach (var gem in gems){
            columns.Add(gem.Index.GridX);
        }

        return columns;
    }

    #endregion
}