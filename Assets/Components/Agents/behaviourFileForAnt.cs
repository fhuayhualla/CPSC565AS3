using System.Collections;
using System.Collections.Generic;
using Antymology.Terrain;
using UnityEngine;
using System.Collections;
using UnityEngine;



public class AntBehaviour : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float turnSpeed = 90f; 
    public float climbSpeed = 2f;
    private Quaternion targetRotation;
    private Vector3 startPosition;
    private bool canMove = true;
    private bool isClimbing = false;
    private Stack<Vector3> lastPositions = new Stack<Vector3>(); // To backtrack
    private int noPathFoundCount = 0;

    void Start()
    {
        targetRotation = transform.rotation;
        startPosition = transform.position;
    }

    void Update()
    {
        if (canMove && !isClimbing)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // Check for ContainerBlock in front of the ant
            Vector3 positionInFront = transform.position + transform.forward;
            var blockInFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y), Mathf.FloorToInt(positionInFront.z));

            if (blockInFront is ContainerBlock)
            {
                HandleContainerBlockEncounter();
            }
            else if (blockInFront is AcidicBlock)
            {
                HandleAcidicBlockEncounter();
            }
            else if (PathIsClear())
            {
                MoveAnt();
                lastPositions.Push(transform.position); // Remember last position for potential backtracking
                noPathFoundCount = 0; // Reset counter
            }
            else if (noPathFoundCount < 4)
            {
                FindClearPath();
            }
            else
            {
                Backtrack();
            }
        }
    }

    void HandleContainerBlockEncounter()
    {
        TurnAround();
    }

    void HandleAcidicBlockEncounter()
    {
        // Example action: turn the ant around to avoid the acidic block
        TurnAround();
    }

    void MoveAnt()
    {
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    bool IsGrounded()
    {
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f;
        var blockBelow = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionBelow.x), Mathf.FloorToInt(positionBelow.y), Mathf.FloorToInt(positionBelow.z));
        return !(blockBelow is AirBlock);
    }

    bool PathIsClear()
    {
        Vector3 positionInFront = transform.position + transform.forward;
        var blockInFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y), Mathf.FloorToInt(positionInFront.z));
        return blockInFront is AirBlock;
    }

    void FindClearPath()
    {
        StartCoroutine(ResolveCollision());
        noPathFoundCount++;
    }

    void Backtrack()
    {
        if (lastPositions.Count > 0)
        {
            Vector3 lastPosition = lastPositions.Pop(); // Go back to the last position
            StartCoroutine(MoveToPosition(lastPosition));
            noPathFoundCount = 0; // Reset no path found count after backtracking
        }
        else
        {
            Debug.Log("No previous positions to backtrack to.");
            // Implement additional logic for when the ant cannot backtrack.
            SeekNewDirectionOrWait();
        }
    }

    void SeekNewDirectionOrWait()
    {
        // Randomly choose a new direction to explore.
        if (Random.value > 0.5f)
        {
            Debug.Log("Choosing a new direction randomly.");
            ChooseRandomDirection();
        }
        else
        {
            // Wait for a short period before trying to move again.
            // This can simulate the ant "thinking" or waiting for the environment to change.
            Debug.Log("Waiting before trying to move again.");
            StartCoroutine(WaitBeforeMoving());
        }
    }

    IEnumerator WaitBeforeMoving()
    {
        yield return new WaitForSeconds(2f); // Wait for 2 seconds.
        // After waiting, try to move again.
        if (PathIsClear())
        {
            MoveAnt();
        }
        else
        {
            // If the path is still not clear, choose a new direction randomly.
            ChooseRandomDirection();
        }
    }

    void ChooseRandomDirection()
    {
        // Randomly choose between turning left or right by 90 degrees.
        int randomTurn = Random.Range(0, 2) * 180 - 90; // Results in either -90 or 90.
        targetRotation *= Quaternion.Euler(0, randomTurn, 0);
        StartCoroutine(MoveForwardAfterRandomTurn());
    }

    IEnumerator MoveForwardAfterRandomTurn()
    {
        // Wait for the rotation to complete.
        yield return new WaitForSeconds(0.5f); // Adjust time as needed.
        // Check if the path is clear, then move forward.
        if (PathIsClear())
        {
            MoveAnt();
        }
        else
        {
            // If still blocked, try turning again or implement additional logic.
            Debug.Log("Still blocked after turning. Implement further logic as needed.");
        }
    }

    IEnumerator MoveToPosition(Vector3 position)
    {
        float time = 0;
        Vector3 startPosition = transform.position;

        while (time < 1)
        {
            transform.position = Vector3.Lerp(startPosition, position, time);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = position;
    }

    void TurnRight()
    {
        targetRotation *= Quaternion.Euler(0, 90, 0);
    }

    IEnumerator ResolveCollision()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.5f)); //random delay
        TurnRight(); 
    }


    void TurnAround()
    {
        // Rotates the ant 180 degrees to walk in the opposite direction
        targetRotation *= Quaternion.Euler(0, 180, 0);
    }

    IEnumerator Climb(Vector3 direction, float height)
    {
        isClimbing = true;
        Vector3 start = transform.position;
        Vector3 end = start + direction * height;
        float time = 0;

        while (time < 1)
        {
            transform.position = Vector3.Lerp(start, end, time);
            time += Time.deltaTime * climbSpeed;
            yield return null;
        }

        transform.position = end;
        isClimbing = false;
    }

    private void AdjustAntHeight()
    {
        RaycastHit hit;
        // Cast ray downwards to adjust ant's height based on the block below.
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            // Adjust the ant's height to be on top of the block.
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.5f, transform.position.z);
        }
    }
}









