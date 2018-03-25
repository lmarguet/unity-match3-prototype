using DefaultNamespace;
using UnityEngine;

public class GridTileBehaviour : MonoBehaviour
{

	public GridIndex Index { get; private set; }

	private BoardBehaviour board;


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
	
	public GridTileBehaviour Init(int x, int y, BoardBehaviour board)
	{
		Index = new GridIndex(x, y); 
		this.board = board;
		
		return this;
	}
}
