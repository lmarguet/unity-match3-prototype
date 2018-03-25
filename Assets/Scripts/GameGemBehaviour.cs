using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGemBehaviour : MonoBehaviour
{

	public int xIndex;
	public int yIndex;

	private bool isMoving;
	private BoardBehaviour board;
	
	void Start () {
		
	}


	public void SetCord(int x, int y)
	{
		xIndex = x;
		yIndex = y;
	}

	public GameGemBehaviour Init(BoardBehaviour board)
	{
		this.board = board;
		return this;
	}

	public void Move(int destX, int destY, float timeToMove)
	{
		if(isMoving) return;
		isMoving = true;
		
		StartCoroutine(
			MoveRoutine(new Vector3(destX,destY), timeToMove)
		);
	}

	// TODO replace with tween
	IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
	{
		var startPosition = transform.position;
		var reachedDestination = false;
		var elapsedTime = 0f;

		while (!reachedDestination){
			elapsedTime +=  Time.deltaTime;

			var lerpT = Mathf.Clamp(elapsedTime / timeToMove, 0, 1);
			lerpT = Mathf.Sin(lerpT * Mathf.PI / 2f);
			
			transform.position = Vector3.Lerp(
				a:startPosition, b:destination, t:lerpT
			);
			
			reachedDestination = Vector3.Distance(transform.position, destination) < 0.01f;
			
			yield return null;
		}
		
		board.PlaceGem(this, (int)destination.x, (int)destination.y);
		
		isMoving = false;
	}
	
}
