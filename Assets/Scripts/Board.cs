using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum GameState
{
    WAIT,
    MOVE
}

public class Board : MonoBehaviour
{
    
    public GameState currentState = GameState.MOVE;
    public int width;
    public int height;
    public int offSet;
    public GameObject tilePrefab;
    public GameObject destroyEffect;
    public GameObject[] dots;
    public GameObject[,] allDots;
    
    private BackgroundTile[,] allTiles;
    private FindMatches findMatches;
        
    void Start()
    {
        findMatches = FindObjectOfType<FindMatches>();
        allTiles = new BackgroundTile[width, height];
        allDots = new GameObject[width, height];
        SetUp();
    }

    private void SetUp() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                Vector2 tempPosition = new Vector2(i, j + offSet);
                GameObject backgroundTile = Instantiate(tilePrefab, tempPosition, Quaternion.identity) as GameObject;
                backgroundTile.transform.parent = this.transform;
                backgroundTile.name = "( " + i + ", " + j + " )";
                
                int dotToUse = Random.Range(0, dots.Length);
                int maxIterations = 0;
                
                while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100) {
                    dotToUse = Random.Range(0, dots.Length);
                    maxIterations++;
                    Debug.Log(maxIterations);
                }

                maxIterations = 0;
                
                GameObject dot = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                dot.GetComponent<Dot>().row = j;
                dot.GetComponent<Dot>().column = i;
                
                dot.transform.parent = this.transform;
                dot.name = "( " + i + ", " + j + " )";
                allDots[i, j] = dot;
            }
        }   
    }

    private bool MatchesAt(int column,  int row, GameObject piece) {
        if (column > 1 && row > 1) {
            if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag) {
                return true;
            } 
            if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag) {
                return true;
            } 
        } else if (column <= 1 || row <= 1) {
            if (row > 1) {
                if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag) {
                    return true;
                }
            }
            if (column > 1) {
                if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag) {
                    return true;
                }
            }
        }
        
        return false;
    }

    private void DestroyMatchesAt(int column, int row) {
        if (allDots[column, row].GetComponent<Dot>().isMatched) {
            findMatches.currentMatches.Remove(allDots[column, row]);
            GameObject particle = Instantiate(destroyEffect, allDots[column, row].transform.position, Quaternion.identity);
            Destroy(particle, .5f);
            Destroy(allDots[column, row]);
            allDots[column, row] = null;
        }
    }

    public void DestroyMatches() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    DestroyMatchesAt(i, j);
                }
            }   
        }
        StartCoroutine(DecreaseRowCo());
    }

    private IEnumerator DecreaseRowCo() {
        int nullCount = 0;
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] == null) {
                    nullCount++;
                } else if (nullCount > 0) {
                    allDots[i, j].GetComponent<Dot>().row -= nullCount;
                    allDots[i, j] = null;
                }
            }
            nullCount = 0;
        }
        yield return new WaitForSeconds(.4f);
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] == null) {
                    Vector2 tempPosition = new Vector2(i, j + offSet);
                    int dotToUse = Random.Range(0, dots.Length);
                    GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                    allDots[i, j] = piece;
                    piece.GetComponent<Dot>().row = j;
                    piece.GetComponent<Dot>().column = i;
                }
            }
        }
    }

    private bool MatchesOnBoard() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (allDots[i, j].GetComponent<Dot>().isMatched) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator FillBoardCo() {
        RefillBoard();
        yield return new WaitForSeconds(.5f);

        while (MatchesOnBoard()) {
            yield return new WaitForSeconds(.5f);
            DestroyMatches();
        }

        findMatches.currentMatches.Clear();
        yield return new WaitForSeconds(.5f);

        if (IsDeadLocked()) {
            ShuffleBoard();
            Debug.Log("Deadlocked!!");
        }
        currentState = GameState.MOVE;
    }

    
    private void SwitchPieces(int column, int row, Vector2 direction) {
        GameObject holder = allDots[column + (int)direction.x, row + (int)direction.y] as GameObject;
        allDots[column + (int)direction.x, row + (int)direction.y] = allDots[column, row];
        allDots[column, row] = holder;
    }
    

    private bool CheckForMatches() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (i < width - 2) {
                        if (allDots[i + 1, j] != null && allDots[i + 2, j] != null) {
                            if (allDots[i + 1, j].tag == allDots[i, j].tag 
                                && allDots[i + 2, j].tag == allDots[i, j].tag) {
                                return true;
                            }
                        }
                    }
                    
                    if (j < height - 2) {
                        if (allDots[i, j + 1] != null && allDots[i, j + 2] != null) {
                            if (allDots[i, j + 1].tag == allDots[i, j].tag 
                                && allDots[i, j + 2].tag == allDots[i, j].tag) {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool SwitchAndCheck(int column, int row, Vector2 direction) {
        SwitchPieces(column, row, direction);
        if (CheckForMatches()) {
            SwitchPieces(column, row, direction);
            return true;
        }
        SwitchPieces(column, row, direction);
        return false;
    }

    private bool IsDeadLocked() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    if (i < width - 1) {
                        if (SwitchAndCheck(i, j, Vector2.right)) {
                            return false;
                        }
                    }
                    if (j < height - 1) {
                        if (SwitchAndCheck(i, j, Vector2.up)) {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    private void ShuffleBoard() {
        List<GameObject> newBoard = new List<GameObject>();
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (allDots[i, j] != null) {
                    newBoard.Add(allDots[i, j]);
                }
            }
        }

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                int pieceToUse = Random.Range(0, newBoard.Count);

                int maxIterations = 0;
                while (MatchesAt(i, j, newBoard[pieceToUse]) && maxIterations < 100) {
                    pieceToUse = Random.Range(0, newBoard.Count);
                    maxIterations++;
                    Debug.Log(maxIterations);
                }
                Dot piece = newBoard[pieceToUse].GetComponent<Dot>();

                maxIterations = 0;

                piece.column = i;
                piece.row = j;
                allDots[i, j] = newBoard[pieceToUse];
                newBoard.Remove(newBoard[pieceToUse]);
            }
        }

        if (IsDeadLocked()) {
            ShuffleBoard();
        }
    }
}
