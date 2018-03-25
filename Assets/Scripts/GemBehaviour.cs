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

    public GridIndex Index { get; private set; }
    public MatchValue MatchType;

    private bool isMoving;
    private BoardBehaviour board;
    

    public void SetCord(int x, int y)
    {
        Index = new GridIndex(x, y);
    }

    public GemBehaviour Init(BoardBehaviour board)
    {
        this.board = board;
        
        return this;
    }

    public void Move(GridIndex newIndex, float timeToMove)
    {
        if (isMoving) return;
        isMoving = true;

        StartCoroutine(
            MoveRoutine(new Vector3(newIndex.GridX, newIndex.GridY), timeToMove)
        );
    }

    IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        var startPosition = transform.position;
        var reachedDestination = false;
        var elapsedTime = 0f;

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

        board.PlaceGem(this, (int) destination.x, (int) destination.y);

        isMoving = false;
    }
}