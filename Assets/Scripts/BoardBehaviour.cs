using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Extensions;
using DefaultNamespace;
using UnityEngine;
using Random = UnityEngine.Random;


#region EditorParameters

[Serializable]
public class GridParameters
{
    public int Columns;
    public int Rows;
    public int Padding;
    public int MaxGemTypes;
}

[Serializable]
public class ViewParameters
{
    public GameObject TilePrefab;
    public GameObject[] GemPrefabs;
    public Transform TileContainer;
    public Transform GemContainer;
}

[Serializable]
public class SequenceParameters
{
    public float GemMoveTime = 0.2f;
    public float GemFallTime = 0.25f;
    public float PlayerMatchCheckDelay = 0.1f;
    public float RefreshMatchCheckDelay = 0.35f;
}

#endregion


public class BoardBehaviour : MonoBehaviour
{
    [SerializeField] GridParameters GridParameters;
    [SerializeField] ViewParameters ViewParameters;
    [SerializeField] SequenceParameters SequenceParameters;


    public GridManager GridManager { get; private set; }
    public TilesManager TilesManager { get; private set; }

    private bool playerControlsEnabled = true;


    void Start()
    {
        GridParameters.MaxGemTypes = Mathf.Clamp(GridParameters.MaxGemTypes, 3, ViewParameters.GemPrefabs.Length);


        TilesManager = new TilesManager(
            this,
            GridParameters.Columns,
            GridParameters.Rows
        );


        GridManager = new GridManager(
            this, GridParameters.Columns,
            GridParameters.Rows,
            SequenceParameters.GemFallTime
        );

        SetupCamera();
    }


    private void SetupCamera()
    {
        var camera = Camera.main;

        camera.gameObject.SetPosition(
            (float) (GridParameters.Columns - 1) / 2,
            (float) (GridParameters.Rows - 1) / 2,
            -10f);

        var aspectRatio = (float) Screen.width / Screen.height;
        var verticalSize = (float) GridParameters.Rows / 2 + GridParameters.Padding;
        var horizontalSize = (float) GridParameters.Columns / 2 + GridParameters.Padding / aspectRatio;

        camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }


    private IEnumerator RefillRoutine()
    {
        GridManager.FillBoard(true);
        yield return null;
    }


    public void SwapTiles(GridTileBehaviour clicked, GridTileBehaviour target)
    {
        if (!playerControlsEnabled
         || !BoardUtils.IsDirecNeighbor(clicked.Index, target.Index)){
            return;
        }

        StartCoroutine(
            SwapTilesRoutine(
                GridManager.GetGem(clicked.Index),
                GridManager.GetGem(target.Index))
        );
    }


    private IEnumerator SwapTilesRoutine(GemBehaviour gemA, GemBehaviour gemB)
    {
        var indexA = gemA.Index.Clone();
        var indexB = gemB.Index.Clone();

        gemA.Move(indexB, SequenceParameters.GemMoveTime);
        gemB.Move(indexA, SequenceParameters.GemMoveTime);

        yield return new WaitForSeconds(SequenceParameters.GemMoveTime);

        var matches =
            GridManager.FindMatchesAt(indexA)
                       .Union(GridManager.FindMatchesAt(indexB));


        if (matches.Any()){
            yield return new WaitForSeconds(SequenceParameters.PlayerMatchCheckDelay);
            ClearAndRefillBoard(matches);
        } else{
            gemA.Move(indexA, SequenceParameters.GemMoveTime);
            gemB.Move(indexB, SequenceParameters.GemMoveTime);
        }
    }


    private void ClearAndRefillBoard(IEnumerable<GemBehaviour> gems)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gems));
    }


    private IEnumerator ClearAndRefillBoardRoutine(IEnumerable<GemBehaviour> gems)
    {
        playerControlsEnabled = false;

        yield return new WaitForSeconds(SequenceParameters.PlayerMatchCheckDelay);

        yield return StartCoroutine(ClearAndCollapseRoutine(gems));
        yield return null;

        yield return StartCoroutine(RefillRoutine());

        playerControlsEnabled = true;
    }


    private IEnumerator ClearAndCollapseRoutine(IEnumerable<GemBehaviour> matches)
    {
        var complete = false;
        while (!complete){
            GridManager.ClearGems(matches);

            yield return new WaitForSeconds(0.25f);

            var movedGems = GridManager.CollapseColumns(matches);

            while (!BoardUtils.IsFallComplete(movedGems)){
                yield return null;
            }

            yield return new WaitForSeconds(SequenceParameters.RefreshMatchCheckDelay);

            matches = GridManager.FindMatchesAt(movedGems);

            if (!matches.Any()){
                complete = true;
            } else{
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }
    }


    #region Gems life cycle

    public GameObject InsantiateTile(int column, int row)
    {
        return Instantiate(ViewParameters.TilePrefab,
                           new Vector3(column, row),
                           Quaternion.identity,
                           parent: ViewParameters.TileContainer);
    }


    public GameObject InsantiateGem()
    {
        return Instantiate(
            GetRandomGemPrefab(),
            Vector3.zero, Quaternion.identity, parent: ViewParameters.GemContainer);
    }


    private GameObject GetRandomGemPrefab()
    {
        return ViewParameters.GemPrefabs[
            Random.Range(0, GridParameters.MaxGemTypes)
        ];
    }


    public static void DestroyGem(GemBehaviour gem)
    {
        Destroy(gem.gameObject);
    }

    #endregion
}