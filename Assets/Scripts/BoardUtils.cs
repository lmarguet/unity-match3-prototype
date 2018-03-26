using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public static class BoardUtils
    {
        public static bool IsInBounds(GridIndex index, int columns, int rows)
        {
            return index.GridX >= 0
                && index.GridX < columns
                && index.GridY >= 0
                && index.GridY < rows;
        }
        
        

        public static IEnumerable<int> GetColumns(IEnumerable<GemBehaviour> gems)
        {
            var columns = new HashSet<int>();
            foreach (var gem in gems){
                columns.Add(gem.Index.GridX);
            }

            return columns;
        }
        
        
        public static bool IsDirecNeighbor(GridIndex indexA, GridIndex indexB)
        {
            return Mathf.Abs(indexA.GridX - indexB.GridX) == 1 && indexA.GridY == indexB.GridY
                || Mathf.Abs(indexA.GridY - indexB.GridY) == 1 && indexA.GridX == indexB.GridX;
        }

        

        public static bool IsFallComplete(IEnumerable<GemBehaviour> gems)
        {
            return gems.All(gem => !gem.IsMoving);
        }
    }
}