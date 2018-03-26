using System;
using Assets.Scripts.Extensions;
using DefaultNamespace;
using UnityEngine;

public class TilesManager
{
    private readonly int columns;
    private readonly int rows;
    private readonly GridTileBehaviour[,] tiles;
    private readonly BoardBehaviour board;

    private GridTileBehaviour clickedTile;
    private GridTileBehaviour targetTile;

    public TilesManager(BoardBehaviour board, int columns, int rows)
    {
        this.columns = columns;
        this.rows = rows;
        this.board = board;

        tiles = new GridTileBehaviour[columns, rows];
        SetupTiles();
    }


    private void SetupTiles()
    {
        for (var i = 0; i < columns; i++){
            for (var j = 0; j < rows; j++){
                var tile =
                    board.InsantiateTile(i, j)
                         .SetName(string.Format("Tile [{0}:{1}]", i, j))
                         .GetComponent<GridTileBehaviour>()
                         .Init(i, j, board);

                tile.OnPress += HandleTileClicked;
                tile.OnDragOver += HandleDragOverTile;
                tile.OnRelease += HandleTileReleased;
                tiles[i, j] = tile;
            }
        }
    }


    private GridTileBehaviour GetTile(GridIndex index)
    {
        return BoardUtils.IsInBounds(index, columns, rows)
                   ? tiles[index.GridX, index.GridY]
                   : null;
    }


    private void HandleTileClicked(GridTileBehaviour tile)
    {
        if (clickedTile == null){
            clickedTile = tile;
        }
    }


    private void HandleDragOverTile(GridTileBehaviour tile)
    {
        if (clickedTile != null){
            targetTile = tile;
        }
    }


    private void HandleTileReleased(GridTileBehaviour tile)
    {
        if (clickedTile != null && targetTile != null){
            board.SwapTiles(clickedTile, targetTile);
        }

        clickedTile = null;
        targetTile = null;
    }


    #region Visual Debug Helpers

    public void HighlightMatches()
    {
        ClearAllHighlights();

        for (var i = 0; i < columns; i++){
            for (var j = 0; j < rows; j++){
                HighlightMatchesAt(
                    new GridIndex(i, j)
                );
            }
        }
    }

    public void ClearAllHighlights()
    {
        for (var i = 0; i < columns; i++){
            for (var j = 0; j < rows; j++){
                GetTile(new GridIndex(i, j))
                   .GetComponent<SpriteRenderer>()
                   .SetEnabled(false);
            }
        }
    }

    public void HighlightMatchesAt(GridIndex index)
    {
        var matches = board.GridManager.FindMatchesAt(index);

        foreach (var gem in matches){
            HighlightTile(gem.Index);
        }
    }

    public void HighlightTile(GridIndex index)
    {
        GetTile(index)
           .GetComponent<SpriteRenderer>()
           .SetEnabled(true)
           .SetColor(
                board.GridManager.GetGem(index)
                     .GetComponent<SpriteRenderer>().color
            );
    }

    #endregion
}