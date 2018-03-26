using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class GemMatchingHelper
{
    private readonly GridManager gridManager;


    public GemMatchingHelper(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public IEnumerable<GemBehaviour> FindMatches(GridIndex startIndex, Vector2 searchDirection)
    {
        var matches = new List<GemBehaviour>();

        var startGem = gridManager.GetGem(startIndex);
        if (startGem == null){
            return Enumerable.Empty<GemBehaviour>();
        }

        matches.Add(startGem);

        var maxValue = Mathf.Max(gridManager.columns, gridManager.rows);

        for (var i = 1; i < maxValue - 1; i++){
            var index = new GridIndex(
                startIndex.GridX + (int) searchDirection.x * i,
                startIndex.GridY + (int) searchDirection.y * i
            );

            if (!BoardUtils.IsInBounds(index, gridManager.columns, gridManager.rows)){
                break;
            }

            var gem = gridManager.GetGem(index);

            if (gem != null && gem.MatchType == startGem.MatchType){
                matches.Add(gem);
            } else{
                break;
            }
        }

        return matches;
    }


    public List<GemBehaviour> FindVerticalMatches(GridIndex index)
    {
        return FindAndCombineMatches(
            index, direction1:
            Vector2.down, direction2:
            Vector2.up);
    }


    public List<GemBehaviour> FindHorizontalMatches(GridIndex index)
    {
        return FindAndCombineMatches(
            index, direction1:
            Vector2.left,
            direction2: Vector2.right);
    }


    private List<GemBehaviour> FindAndCombineMatches(GridIndex index, Vector2 direction1, Vector2 direction2)
    {
        return
            FindMatches(index, searchDirection: direction1)
               .Union(
                    FindMatches(index, searchDirection: direction2))
               .ToList();
    }
}