using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;

public class GemBehaviour : MonoBehaviour
{
    public enum MatchValue
    {
        Yellow,
        Blue,
        Magenta,
        Indigo,
        Green,
        Teal,
        Red,
        Cyan,
        Wild
    }

    public Action<GemBehaviour, GridIndex> OnMoveComplete;

    public GridIndex Index { get; private set; }
    public bool IsMoving { get; private set; }

    public MatchValue MatchType;

    private BoardBehaviour board;


    public GemBehaviour Init(BoardBehaviour board)
    {
        this.board = board;
        return this;
    }


    public GemBehaviour SetCord(GridIndex index)
    {
        Index = index;
        return this;
    }


    public GemBehaviour ApplyOffsetY(int yOffset, float timeToFall)
    {
        if (yOffset > 0){
            transform.position = new Vector3(Index.GridX, yOffset);
            Move(new GridIndex(Index.GridX, Index.GridY), timeToFall);
        }

        return this;
    }


    public void Move(GridIndex newIndex, float timeToMove)
    {
        if (IsMoving){
            return;
        }

        IsMoving = true;

        StartCoroutine(
            MoveRoutine(newIndex, timeToMove)
        );
    }


    private IEnumerator MoveRoutine(GridIndex newIndex, float timeToMove)
    {
        var startPosition = transform.position;
        var reachedDestination = false;
        var elapsedTime = 0f;
        var destination = new Vector3(newIndex.GridX, newIndex.GridY);

        while (!reachedDestination){
            elapsedTime += Time.deltaTime;

            var lerpT = Mathf.Clamp(elapsedTime / timeToMove, 0, 1);
            lerpT = Mathf.Sin(lerpT * Mathf.PI / 2f);

            transform.position = Vector3.Lerp(
                a: startPosition, b: destination, t: lerpT
            );

            reachedDestination = Vector3.Distance(transform.position, destination) < 0.01f;

            yield return null;
        }

        if (OnMoveComplete != null){
            OnMoveComplete(this, newIndex);
        }

        IsMoving = false;
    }
}