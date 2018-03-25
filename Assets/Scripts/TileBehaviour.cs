using DefaultNamespace;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{

	public BoardIndex Index { get; private set; }

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
	
	public TileBehaviour Init(int x, int y, BoardBehaviour board)
	{
		Index = new BoardIndex(x, y); 
		this.board = board;
		
		return this;
	}
}
