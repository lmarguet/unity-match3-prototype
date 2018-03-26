using System;
using DefaultNamespace;
using UnityEngine;

public class GridTileBehaviour : MonoBehaviour
{
    public Action<GridTileBehaviour> OnPress;
    public Action<GridTileBehaviour> OnDragOver;
    public Action<GridTileBehaviour> OnRelease;

    public GridIndex Index { get; private set; }

    private BoardBehaviour board;


    #region User input

    void OnMouseDown()
    {
        if (OnPress != null){
            OnPress(this);
        }
    }

    void OnMouseEnter()
    {
        if (OnDragOver != null){
            OnDragOver(this);
        }
    }

    void OnMouseUp()
    {
        if (OnRelease != null){
            OnRelease(this);
        }
    }

    #endregion


    public GridTileBehaviour Init(int x, int y, BoardBehaviour board)
    {
        Index = new GridIndex(x, y);
        this.board = board;

        return this;
    }
}