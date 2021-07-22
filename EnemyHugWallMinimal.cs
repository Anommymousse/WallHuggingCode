using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkTonic.PoolBoss;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyHugWallMinimal : MonoBehaviour
{
    [SerializeField] bool isClockwise = true;
    float _speed = 0.2f;
    Vector3 _initialWorldPosition;
    Vector2Int _initialDirection;

    Vector2 _currentDirection;
    Vector3Int _oldCellPos;
    Vector3Int _currentCellPos;

    Tilemap _nonHiddenRef;

    //:Talking point Thanks to Jason for this idea, implementmented badly but hey it works ^_^
    void Log(string stringToLog)
    {
        /*
        string path = "D:/downloads/EnemyHugWallMinimal_log.txt";
        if (!File.Exists(path)) {
            File.WriteAllText(path," ");
        }
        File.AppendAllText(path,stringToLog);        
        File.AppendAllText(path," \n");*/
    }
    
    void Start()
    {
        var player = GameObject.Find("Player").GetComponent<PlayerMovement>();
        player.OnPlayerReset += ResetEnemy;
        player.OnPlayerLevelChange += OnPlayerLevelChange;
        
        _initialWorldPosition = transform.position;
        _initialDirection = Vector2Int.down;
        _nonHiddenRef = GameObject.Find("NonHidden").GetComponent<Tilemap>();
        
        SetInitialState();
        
    }
    
    void SetInitialState()
    {
        transform.position = _initialWorldPosition;
        _currentDirection = _initialDirection;
        _currentCellPos = _nonHiddenRef.WorldToCell(_initialWorldPosition);
        _oldCellPos = _currentCellPos;
    }

    void OnPlayerLevelChange() => PoolBoss.Despawn(this.transform);

    void ResetEnemy()
    {
        SetInitialState();
    }

    bool IsCellSameAsOld()
    {
        if (_oldCellPos != _currentCellPos) return false;
        return true;
    }

    void Rotate90Clockwise()
    {
        Log("Rotate 90 clockwise");
        if(_currentDirection==Vector2.down) _currentDirection=Vector2Int.left;
        else
        if(_currentDirection==Vector2.left) _currentDirection=Vector2Int.up;
        else
        if(_currentDirection==Vector2.up) _currentDirection=Vector2Int.right;
        else
        if(_currentDirection==Vector2.right) _currentDirection=Vector2Int.down;
    }

    void RotateMinus90()
    {
        Log("Rotate -90 anticlockwise");
        if(_currentDirection==Vector2.down) _currentDirection=Vector2Int.right;
        else
        if(_currentDirection==Vector2.right) _currentDirection=Vector2Int.up;
        else
        if(_currentDirection==Vector2.up) _currentDirection=Vector2Int.left;
        else
        if(_currentDirection==Vector2.left) _currentDirection=Vector2Int.down;
    }
    
    void UpdateDirection()
    {
        if (IsCellSameAsOld() == false) ProcessNewCellDirectionChange(); 
        if (IsNextMoveIntoBrick()) CollisionAdjustDirection();
    }

    void CollisionAdjustDirection()
    {
        Log("Predicted collision : rotate in appropriate direction\n ");
        if(isClockwise)
            Rotate90Clockwise();
        else
            RotateMinus90();
    }

    bool IsNextMoveIntoBrick()
    {
        Vector3 destPosition =  transform.position;
        destPosition += (Vector3)(_currentDirection * _speed);
        
        if (_nonHiddenRef.HasTile(_nonHiddenRef.WorldToCell(destPosition))) 
        {
            return true;
        }

        return false;
    }

    Vector3Int GetClosestFilledCell(Vector3 worldPointOfCircle)
    {
        List<Vector3Int> cellList = new List<Vector3Int>();
        
        Vector3Int cellCirclePosition = _nonHiddenRef.WorldToCell(worldPointOfCircle);
        
        Log($"GetClosestFilledCell call with position {cellCirclePosition} \n");
        bool cellWasPresent = false;
        for(int x=-1; x<2 ; x++)
        for (int y = -1; y < 2; y++)
        {
            Vector3Int testPosition = cellCirclePosition;
            testPosition.x += x;
            testPosition.y += y;
            if (_nonHiddenRef.HasTile(testPosition))
            {
                cellList.Add(testPosition);
                Log($"cell {testPosition} at {_nonHiddenRef.CellToWorld(testPosition)}has tile \n");
                cellWasPresent = true;
            }
            else
            {
                Log($"{testPosition} has no tile present\n");
            }
        }

        //:Talking point ? : I prefer this to relying on cellList.Length == 0, as it's clearer what this means, though maybe slower,maybe not?
        if (cellWasPresent == false)
        {
            Log("No cells present");
            return Vector3Int.zero;
        }
        
        float mindist = Single.MaxValue;
        Vector3Int cellfound = Vector3Int.zero;
         
        foreach (var brickcell in cellList)
        {
            Vector3 worldBrickAdjustToCenter = _nonHiddenRef.CellToWorld(brickcell);
            worldBrickAdjustToCenter.x += 0.5f;
            worldBrickAdjustToCenter.y += 0.5f;
            var worldPos = worldBrickAdjustToCenter - worldPointOfCircle; 

            if (mindist > worldPos.magnitude)
            {
                cellfound = brickcell;
                mindist = worldPos.magnitude;
                Log($"new cell choosen {cellfound} distance = {mindist}\n");
            }
        }

        //Talking point : Magic numbers are generally bad, in this case this is to ignore anything that isn't relevant threshold is 2.0
        if (mindist < 1.8f)
        {
            Log($"Cell choosen : {cellfound} dist {mindist}\n");
            return cellfound;
        }
        else
        {
            Log($"min distance exceeds cell minimum distance cells not close enough {cellfound} {mindist} \n");
            return Vector3Int.zero;
        }
    }
    
    void ProcessNewCellDirectionChange()
    {
        _oldCellPos = _currentCellPos;
        Vector3Int closestFilledCell = GetClosestFilledCell(transform.position);

        if (closestFilledCell == Vector3Int.zero) return;
        
        var cellDiff =  closestFilledCell - _currentCellPos;
        
        if (IsCellAdjacent(cellDiff)) return;
        
        Log($" {_currentCellPos} {_currentDirection} {cellDiff} ");
        
        if (_currentDirection == Vector2.down)
        {
            if (cellDiff.y < 0) return;
            if(cellDiff.x <0) Rotate90Clockwise();
            if(cellDiff.x >0) RotateMinus90();
            return;
        }

        if (_currentDirection == Vector2.up)
        {
            if (cellDiff.y > 0) return;
            if(cellDiff.x > 0) Rotate90Clockwise();
            if(cellDiff.x < 0) RotateMinus90();
            return;
        }

        if (_currentDirection == Vector2.left)
        {
            if (cellDiff.x < 0) return;
            if(cellDiff.y >0) Rotate90Clockwise();
            if(cellDiff.y <0) RotateMinus90();
            return;
        }

        if (_currentDirection == Vector2.right)
        {
            if (cellDiff.x > 0) return;
            if(cellDiff.y <0) Rotate90Clockwise();
            if(cellDiff.y >0) RotateMinus90();
        }
    }

    bool IsCellAdjacent(Vector3Int cellDiff)
    {
        if (cellDiff.y == 0)
            if ((_currentDirection == Vector2.down) || (_currentDirection == Vector2.up)) 
                return true;
        if (cellDiff.x == 0)
            if ((_currentDirection == Vector2.left) || (_currentDirection == Vector2.right)) 
                return true;
        return false;
    }
    
    void FixedUpdate()
    {
        UpdateDirection();
        UpdateCellPosition();
    }
    
    //Talking point ? : Repeated reference here, is it worth stick something used twice into a variable for speed reasons, generally speaking?
    void UpdateCellPosition()
    {
        Vector3 destPosition =  transform.position;
        destPosition += (Vector3)(_currentDirection * _speed);
        _currentCellPos = _nonHiddenRef.WorldToCell(transform.position);
        transform.DOMove(destPosition, 0.0f);
    }
    
}
