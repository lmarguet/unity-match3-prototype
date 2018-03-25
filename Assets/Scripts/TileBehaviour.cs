using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{

	public int indeX;
	public int indeY;

	private BoardBehaviour board;
	
	// Use this for initialization
	void Start () {
		
	}

	public TileBehaviour Init(int x, int y, BoardBehaviour board)
	{
		indeX = x;
		indeY = y;
		this.board = board;
		return this;
	}
}
