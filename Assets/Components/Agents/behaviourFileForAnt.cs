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
    private Stack<Vector3> lastPositions = new Stack<Vector3>(); // For backtrack.
    private int noPathFoundCount = 0;

    public float health = 100f; // The current health of the ant.
    public float healthDecayRate = 10f; // The rate at which health decreases over time.
    public float healthRefillAmount = 50f; // The amount of health restored when consuming a Mulch block.

    void Start()
    {
        targetRotation = transform.rotation;
        startPosition = transform.position;
    }

    void Update()
    {   
        // Check if the object is allowed to move and is not currently climbing.
        if (canMove && !isClimbing)
        {
            TryDigging();

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            Vector3 positionInFront = transform.position + transform.forward;
   
            // Get block type in front.
            AbstractBlock blockInFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y), Mathf.FloorToInt(positionInFront.z));

            HandleSpecialBlockEncounter(blockInFront);

            // Check if the object should climb.
            if (ShouldClimb())
            {
                Vector3 climbDirection = CalculateClimbDirection();
                float climbHeight = 1;
                if (climbHeight <= 2 && climbHeight > 0) // Maximum climbable height of 2.
                {
                    StartCoroutine(Climb(climbDirection, climbHeight));
                    return;
                }
            }

            // If the path is clear, move the object and update
            if (PathIsClear())
            {
                MoveAnt();
                lastPositions.Push(transform.position);
                noPathFoundCount = 0;
            }
            // If no clear path is found, attempt to find one.
            else if (noPathFoundCount < 4)
            {
                FindClearPath();
            }
            else
            {
                Backtrack();
            }

            ExchangeHealthWithNearbyAnts();

            ReduceHealth();

            if (IsOnMulchBlock())
            {
                ConsumeMulchBlock();
            }
        }
    }

    // Determines whether the ant should attempt to climb. 
    bool ShouldClimb()
    {
        Vector3 positionInFront = transform.position + transform.forward;
        AbstractBlock blockInFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y), Mathf.FloorToInt(positionInFront.z));
        AbstractBlock blockAboveFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y) + 1, Mathf.FloorToInt(positionInFront.z));

        return !(blockInFront is AirBlock) && (blockAboveFront is AirBlock);
    }

    Vector3 CalculateClimbDirection()
    {
        // Directly forward as it is climbing straight ahead.
        return transform.forward;
    }

    // Handle different block types in front.
    void HandleSpecialBlockEncounter(AbstractBlock blockInFront)
    {
        if (blockInFront is ContainerBlock)
        {
            HandleContainerBlockEncounter();
        }
        else if (blockInFront is AcidicBlock)
        {
            HandleAcidicBlockEncounter();
        }
    }

    // Reduce the object's health over time.
    void ReduceHealth()
    {
        // Check if the ant is standing on an AcidicBlock.
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f;
        AbstractBlock blockBelow = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionBelow.x), Mathf.FloorToInt(positionBelow.y), Mathf.FloorToInt(positionBelow.z));

        float currentHealthDecayRate = healthDecayRate;

        // If standing on an AcidicBlock, double the decay rate of ant.
        if (blockBelow is AcidicBlock)
        {
            currentHealthDecayRate *= 2;
        }

        health -= currentHealthDecayRate * Time.deltaTime;
        if (health <= 0)
        {
            Die();
        }
    }

    bool IsOnMulchBlock()
    {
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f;
        AbstractBlock blockBelow = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionBelow.x), Mathf.FloorToInt(positionBelow.y), Mathf.FloorToInt(positionBelow.z));
        return blockBelow is MulchBlock;
    }

    // Logic for consuming Mulch Blocks.
    void ConsumeMulchBlock()
    {
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f;
        int x = Mathf.FloorToInt(positionBelow.x);
        int y = Mathf.FloorToInt(positionBelow.y);
        int z = Mathf.FloorToInt(positionBelow.z);

        if (IsValidPosition(positionBelow))
        {
            // Ants cannot consume mulch if another ant is also on the same mulch block.
            Collider[] hitColliders = Physics.OverlapSphere(positionBelow, 0.5f);
            bool otherAntOnBlock = false;

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != gameObject && hitCollider.gameObject.CompareTag("Ant"))
                {
                    otherAntOnBlock = true;
                    break;
                }
            }

            if (!otherAntOnBlock)
            {
                // Refill health only if no other ant is on the block.
                health += healthRefillAmount;
                health = Mathf.Min(health, 100f);

                // Remove the Mulch block from the world.
                WorldManager.Instance.SetBlock(x, y, z, new AirBlock());
            }
            else
            {
                Debug.Log("Another ant is on the same Mulch block, cannot consume.");
            }
        }
        else
        {
            Debug.LogError("Attempted to consume a block on out of bounds position.");
        }
    }


    void Die()
    {
        Destroy(gameObject);
    }

    // Turn around as that is the end of the world.
    void HandleContainerBlockEncounter()
    {
        TurnAround();
    }

    // Turn the ant around to avoid the acidic block.
    void HandleAcidicBlockEncounter()
    {
        TurnAround();
    }

    void MoveAnt() 
    {
        if (PathIsClear()) {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        } else {
            // If the path is not clear due to height difference, etc, find a new path.
            Debug.Log("Blocked path or too steep. Finding alternative...");
            FindClearPath();
        }
    }

    //Keeps ants grounded at all times (their feet should on top of blocks).
    bool IsGrounded()
    {
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f;
        AbstractBlock blockBelow = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionBelow.x), Mathf.FloorToInt(positionBelow.y), Mathf.FloorToInt(positionBelow.z));
        return !(blockBelow is AirBlock);
    }

    bool PathIsClear() 
    {
        Vector3 positionInFront = transform.position + transform.forward;
        Vector3 currentPositionBelow = transform.position - Vector3.up;

        // Getting the block directly in front at the ant's current level.
        AbstractBlock blockInFront = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionInFront.x), Mathf.FloorToInt(positionInFront.y), Mathf.FloorToInt(positionInFront.z));
        // Getting the block directly below the ant to determine the current ground level.
        AbstractBlock currentGroundBlock = WorldManager.Instance.GetBlock(Mathf.FloorToInt(currentPositionBelow.x), Mathf.FloorToInt(currentPositionBelow.y), Mathf.FloorToInt(currentPositionBelow.z));

        // Early return if the block in front is not an air block.
        if (!(blockInFront is AirBlock)) {
            return false;
        }

        // Calculating the height difference between the current position and the position in front.
        int heightDifference = Mathf.Abs(Mathf.FloorToInt(positionInFront.y) - Mathf.FloorToInt(currentPositionBelow.y));

        // Path is considered clear, as per assignment instructions.
        return heightDifference <= 2;

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
            Vector3 lastPosition = lastPositions.Pop(); // Go back to the last position.
            StartCoroutine(MoveToPosition(lastPosition));
            noPathFoundCount = 0; // Reset no path found count after backtracking.
        }
        else
        {
            Debug.Log("No previous positions to backtrack.");
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
            // Employing waiting as a means to help ant get unstuck.
            Debug.Log("Waiting before trying to move again.");
            StartCoroutine(WaitBeforeMoving());
        }
    }

    IEnumerator WaitBeforeMoving()
    {
        yield return new WaitForSeconds(2f); // Wait for 2 seconds.
        if (PathIsClear())
        {
            MoveAnt();
        }
        else
        {
            // Choose a new direction randomly.
            ChooseRandomDirection();
        }
    }

    // Randomly choose between turning left or right by 90 degrees.
    void ChooseRandomDirection()
    {
        int randomTurn = Random.Range(0, 2) * 180 - 90;
        targetRotation *= Quaternion.Euler(0, randomTurn, 0);
        StartCoroutine(MoveForwardAfterRandomTurn());
    }

    IEnumerator MoveForwardAfterRandomTurn()
    {
        // Wait for the rotation to complete.
        yield return new WaitForSeconds(0.5f); 
        if (PathIsClear())
        {
            MoveAnt();
        }
        else
        {
            Debug.Log("Still blocked after turning.");
        }
    }

    // To move the object to a specified position.
    IEnumerator MoveToPosition(Vector3 position)
    {
        float time = 0;
        Vector3 startPosition = transform.position;

        // Until it reaches the target position.
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
        yield return new WaitForSeconds(Random.Range(0.1f, 0.5f)); // Random delay.
        TurnRight(); 
    }


    void TurnAround()
    {
        targetRotation *= Quaternion.Euler(0, 180, 0);
    }

    IEnumerator Climb(Vector3 direction, float height)
    {
        // Ensure the climb does not exceed the maximum height difference.
        if (height > 2)
        {
            Debug.LogError("Attempted to climb a height greater than allowed.");
            yield break;
        }

        isClimbing = true;
        Vector3 start = transform.position;
        Vector3 end = start + direction * height; // Calculate the end position based on the height to climb.

        float time = 0;
        while (time < 1)
        {
            transform.position = Vector3.Lerp(start, end, time);
            time += Time.deltaTime * climbSpeed;
            yield return null; 
        }

        transform.position = end; // Ensure the ant reaches the end position.
        AdjustAntHeight();
        isClimbing = false;
    }

    // Cast ray downwards to adjust ant's height based on the block below.
    private void AdjustAntHeight()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.5f, transform.position.z);
        }
    }

    // Checking to see if position (coordinates) is in valid range of world.
    bool IsValidPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        return x >= 0 && x < ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter &&
            y >= 0 && y < ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter &&
            z >= 0 && z < ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
    }

    void TryDigging()
    {
        Vector3 positionBelow = transform.position - Vector3.up * 0.5f; // Block to dig is directly below the ant.
        AbstractBlock blockBelow = WorldManager.Instance.GetBlock(Mathf.FloorToInt(positionBelow.x), Mathf.FloorToInt(positionBelow.y), Mathf.FloorToInt(positionBelow.z));

        // Check if the block below is either a GrassBlock or a StoneBlock, only these are diggable in this world.
        if (blockBelow is GrassBlock || blockBelow is StoneBlock)
        {
            // Random chance to dig.
            float digChance = 0.2f; 
            if (Random.value < digChance)
            {
                int x = Mathf.FloorToInt(positionBelow.x);
                int y = Mathf.FloorToInt(positionBelow.y);
                int z = Mathf.FloorToInt(positionBelow.z);
                
                WorldManager.Instance.SetBlock(x, y, z, new AirBlock());
                Debug.Log("Dug up a block at " + positionBelow);
            }
        }
    }


    void ExchangeHealthWithNearbyAnts()
    {
        // Define the radius within which to look for other ants.
        float detectionRadius = 1f;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject != gameObject && hitCollider.gameObject.CompareTag("Ant"))
            {
                AntBehaviour otherAnt = hitCollider.gameObject.GetComponent<AntBehaviour>();
                if (otherAnt != null)
                {
                    PerformHealthExchange(otherAnt);
                }
            }
        }
    }

    void PerformHealthExchange(AntBehaviour otherAnt)
    {
        // Assuming 5 for health exchange.
        float healthExchangeAmount = 5f;

        // Check if this ant has enough health to give
        if (this.health > healthExchangeAmount)
        {
            this.health -= healthExchangeAmount;
            otherAnt.health += healthExchangeAmount;

            otherAnt.health = Mathf.Min(otherAnt.health, 100f);
        }
    }

}









