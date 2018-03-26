using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Extensions;
using UnityEngine;

namespace DefaultNamespace
{
    public class GridManager
    {
        private const int MAX_FILL_ATTEMPTS = 200;
        private const int REFILL_Y_OFFSET = 5;

        public readonly int columns;
        public readonly int rows;
        private readonly float gemFallSpeed;
        private readonly BoardBehaviour board;
        private readonly GemBehaviour[,] allGems;
        private readonly GemMatchingHelper matchingHelper;


        public GridManager(BoardBehaviour board, int columns, int rows, float gemFallSpeed)
        {
            this.columns = columns;
            this.rows = rows;
            this.gemFallSpeed = gemFallSpeed;
            this.board = board;

            allGems = new GemBehaviour[columns, rows];
            matchingHelper = new GemMatchingHelper(this);

            FillBoard();
        }


        public void FillBoard(bool isRefill = false)
        {
            for (var i = 0; i < columns; i++){
                for (var j = 0; j < rows; j++){
                    var index = new GridIndex(i, j);
                    if (GetGem(index) != null){
                        continue;
                    }

                    var gem = FillIndexWithGem(isRefill, index);
                    gem.OnMoveComplete += HandleGemMoveComplete;
                }
            }
        }


        private GemBehaviour FillIndexWithGem(bool isRefill, GridIndex index)
        {
            GemBehaviour gem;

            var yOffest = isRefill ? index.GridY + REFILL_Y_OFFSET : 0;
            var attempts = 0;

            do{
                if (attempts > 0){
                    ClearGemAt(index);

                    if (attempts == MAX_FILL_ATTEMPTS){
                        throw new Exception("Max attempts to fill the board reached");
                    }
                }

                gem = FillRandomGemAt(index, yOffest);
                attempts++;
            } while (HasMatchOnFill(index));

            return gem;
        }


        private void SetGemPosition(GemBehaviour gem, GridIndex index)
        {
            allGems[index.GridX, index.GridY] = gem;
            gem.SetCord(index);
        }


        private GemBehaviour FillRandomGemAt(GridIndex index, int yOffset = 0)
        {
            return
                PlaceGem(
                        gem: board.InsantiateGem()
                                  .GetComponent<GemBehaviour>()
                                  .Init(board),
                        index: index)
                   .ApplyOffsetY(yOffset, gemFallSpeed);
        }


        private bool HasMatchOnFill(GridIndex index)
        {
            return matchingHelper.FindMatches(index, Vector2.left).Count() >= 3
                || matchingHelper.FindMatches(index, Vector2.down).Count() >= 3;
        }


        private GemBehaviour PlaceGem(GemBehaviour gem, GridIndex index)
        {
            gem.gameObject
               .ResetTransformation()
               .SetPosition(index.GridX, index.GridY);

            SetGemPosition(gem, index);
            return gem;
        }

        private void HandleGemMoveComplete(GemBehaviour gem, GridIndex newIndex)
        {
            PlaceGem(gem, newIndex);
        }


        public void ClearGems(IEnumerable<GemBehaviour> matches)
        {
            foreach (var gem in matches){
                ClearGemAt(gem.Index);
            }
        }

        private void ClearBoard()
        {
            for (var i = 0; i < columns; i++){
                for (var j = 0; j < rows; j++){
                    ClearGemAt(
                        new GridIndex(i, j)
                    );
                }
            }
        }

        private void ClearGemAt(GridIndex index)
        {
            var gem = GetGem(index);
            if (gem != null){
                allGems[index.GridX, index.GridY] = null;
                gem.OnMoveComplete -= HandleGemMoveComplete;
                BoardBehaviour.DestroyGem(gem);
            }
        }

        public IEnumerable<GemBehaviour> CollapseColumns(IEnumerable<GemBehaviour> gems)
        {
            var movingGems = new List<GemBehaviour>();

            var columns = BoardUtils.GetColumns(gems);
            foreach (var column in columns){
                movingGems.AddRange(
                    CollapseColumn(column)
                );
            }

            return movingGems;
        }


        private IEnumerable<GemBehaviour> CollapseColumn(int column)
        {
            var movingGems = new List<GemBehaviour>();

            for (var i = 0; i < rows - 1; i++){
                if (GetGem(column, i) != null){
                    continue;
                }

                for (var j = i + 1; j < rows; j++){
                    var gem = GetGem(column, j);

                    if (gem != null && !movingGems.Contains(gem)){
                        movingGems.Add(
                            MakeGemFall(gem, xIndex: column, initY: j, newY: i)
                        );

                        break;
                    }
                }
            }

            return movingGems;
        }

        private GemBehaviour MakeGemFall(GemBehaviour gem, int xIndex, int initY, int newY)
        {
            var index = new GridIndex(xIndex, newY);
            gem.Move(index, gemFallSpeed * (initY - newY));

            SetGemPosition(gem, index);

            allGems[xIndex, initY] = null;
            return gem;
        }


        #region Indexing

        private GemBehaviour GetGem(int xIndex, int yIndex)
        {
            return GetGem(new GridIndex(xIndex, yIndex));
        }


        public GemBehaviour GetGem(GridIndex index)
        {
            return BoardUtils.IsInBounds(index, columns, rows)
                       ? allGems[index.GridX, index.GridY]
                       : null;
        }

        #endregion


        #region Matching 

        public IEnumerable<GemBehaviour> FindMatchesAt(GridIndex index)
        {
            var hMatches = matchingHelper.FindHorizontalMatches(index);
            var vMatches = matchingHelper.FindVerticalMatches(index);

            hMatches = hMatches.Count >= 3 ? hMatches : new List<GemBehaviour>();
            vMatches = vMatches.Count >= 3 ? vMatches : new List<GemBehaviour>();

            return hMatches.Any() || vMatches.Any()
                       ? hMatches.Union(vMatches)
                       : Enumerable.Empty<GemBehaviour>();
        }


        public IEnumerable<GemBehaviour> FindMatchesAt(IEnumerable<GemBehaviour> gems)
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
    }
}