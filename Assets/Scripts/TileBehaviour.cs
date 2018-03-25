using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{

	public int xIndex;
	public int yIndex;

	private BoardBehaviour board;
	
	// Use this for initialization
	void Start () {
		
	}

	
	#region User input

	void OnMouseDown()
	{
		board.ClickTile(this);
	}

	void OnMouseEnter()
	{
		board.DragToTile(this);	
	}

	void OnMouseUp()
	{
		board.ReleaseTile();
	}

	#endregion
	
	public TileBehaviour Init(int x, int y, BoardBehaviour board)
	{
		xIndex = x;
		yIndex = y;
		this.board = board;
		return this;
	}
}
