using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Extensions;
using UnityEngine;

public class BoardBehaviour : MonoBehaviour
{
    public int Width;
    public int Height;
    public int BorderSize;
    public GameObject TilePrefab;

    private TileBehaviour[,] tiles;

    // Use this for initialization
    void Start()
    {
        tiles = new TileBehaviour[Width, Height];
        SetupCamera();
        SetupTiles();
    }

    private void SetupTiles()
    {
        for (var i = 0; i < Width; i++){
            for (var j = 0; j < Height; j++){
                tiles[i, j] =
                    Instantiate(TilePrefab, new Vector3(i, j), Quaternion.identity, transform)
                        .SetName(string.Format("Tile [{0}:{1}]", i, j))
                        .GetComponent<TileBehaviour>()
                        .Init(i, j, this);
            }
        }
    }
    

    private void SetupCamera()
    {
        var camera = Camera.main;

        camera.gameObject.SetPosition(
            (float) (Width - 1) / 2,
            (float) (Height - 1) / 2,
            -10f);

        var aspectRatio = (float) Screen.width / Screen.height;
        var verticalSize = (float) Height / 2 + BorderSize;
        var horizontalSize = (float) Width / 2 + BorderSize / aspectRatio;

        camera.orthographicSize = verticalSize > horizontalSize ? verticalSize : horizontalSize;
    }
}