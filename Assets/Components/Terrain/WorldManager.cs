using Antymology.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Terrain
{
    public class WorldManager : Singleton<WorldManager>
    {

        #region Fields

        /// <summary>
        /// The prefab containing the ant.
        /// </summary>
        public GameObject antPrefab;

        /// <summary>
        /// The prefab containing the queen ant.
        /// </summary>
        public GameObject queenAntPrefab;

        /// <summary>
        /// The material used for eech block.
        /// </summary>
        public Material blockMaterial;

        /// <summary>
        /// The raw data of the underlying world structure.
        /// </summary>
        private AbstractBlock[,,] Blocks;

        /// <summary>
        /// Reference to the geometry data of the chunks.
        /// </summary>
        private Chunk[,,] Chunks;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private System.Random RNG;


        /// <summary>
        /// Checks if the given world coordinates are within the bounds of the world.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <returns>true if the position is within bounds, otherwise, false.</returns>
        public bool IsValidPosition(int x, int y, int z)
        {
            return x >= 0 && x < ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter &&
                   y >= 0 && y < ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter &&
                   z >= 0 && z < ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
        }

        /// <summary>
        /// Random number generator.
        /// </summary>
        private SimplexNoise SimplexNoise;

        #endregion

        #region Initialization

        /// <summary>
        /// Awake is called before any start method is called.
        /// </summary>
        void Awake()
        {
            antPrefab = Resources.Load<GameObject>("Ant_Icon");
            queenAntPrefab = Resources.Load<GameObject>("Queen_Blue");

            // Generate new random number generator
            RNG = new System.Random(ConfigurationManager.Instance.Seed);

            // Generate new simplex noise generator
            SimplexNoise = new SimplexNoise(ConfigurationManager.Instance.Seed);

            // Initialize a new 3D array of blocks with size of the number of chunks times the size of each chunk
            Blocks = new AbstractBlock[
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

            // Initialize a new 3D array of chunks with size of the number of chunks
            Chunks = new Chunk[
                ConfigurationManager.Instance.World_Diameter,
                ConfigurationManager.Instance.World_Height,
                ConfigurationManager.Instance.World_Diameter];
        }

        /// <summary>
        /// Called after every awake has been called.
        /// </summary>
        private void Start()
        {
            GenerateData();
            GenerateChunks();

            Camera.main.transform.position = new Vector3(0 / 2, Blocks.GetLength(1), 0);
            Camera.main.transform.LookAt(new Vector3(Blocks.GetLength(0), 0, Blocks.GetLength(2)));

            GenerateAnts();
            GenerateQueenAnt();
        }

        /// <summary>
        /// TO BE IMPLEMENTED BY YOU
        /// </summary>


        private void GenerateAnts()
        {
            int numberOfAntsToSpawn = 100; // Define the number of ants to spawn.

            for (int i = 0; i < numberOfAntsToSpawn; i++)
            {
                bool validPositionFound = false;
                Vector3 spawnPosition = Vector3.zero;

                // Loop until a valid spawn position is found.
                while (!validPositionFound)
                {
                    int x = RNG.Next(0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter);
                    int z = RNG.Next(0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter);
                    int y = 0;

                    // Iterate through the blocks vertically to find a spawn height.
                    for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                    {
                        AbstractBlock block = Blocks[x, j, z];
                        // Check if the block is solid and not a container block.
                        if (block != null && !(block is AirBlock) && !(block is ContainerBlock)) // Avoid spawning on AirBlock or ContainerBlock
                        {
                            y = j + 1; 
                            validPositionFound = true;
                            spawnPosition = new Vector3(x, y, z);
                            break;
                        }
                    }
                }

                if (validPositionFound)
                {
                    GameObject ant = Instantiate(antPrefab, spawnPosition, Quaternion.identity);
                    ant.transform.parent = this.transform;
                }
            }
        }


        private void GenerateQueenAnt()
        {
            bool validPositionFound = false;
            Vector3 spawnPosition = Vector3.zero;

            // Used for spawning not close to container blocks.
            int safeMargin = 10;

            // Calculate the middle section of the map.
            int minX = safeMargin;
            int maxX = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - safeMargin;
            int minZ = safeMargin;
            int maxZ = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - safeMargin;

            int attempts = 0;
            int maxAttempts = 1000;

            while (!validPositionFound && attempts < maxAttempts)
            {
                attempts++;
                // Generate coordinates within the middle section.
                int x = RNG.Next(minX, maxX);
                int z = RNG.Next(minZ, maxZ);

                for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                {
                    AbstractBlock block = Blocks[x, j, z];
                    if (block != null && !(block is ContainerBlock) && IsPositionSafeFromContainers(x, j, z))
                    {
                        spawnPosition = new Vector3(x, j + 1, z); // Spawn on top of the block.
                        validPositionFound = true;
                        break; 
                    }
                }
            }

            if (validPositionFound)
            {
                GameObject queenAnt = Instantiate(queenAntPrefab, spawnPosition, Quaternion.identity);
                queenAnt.transform.parent = this.transform; 
            }
            else
            {
                Debug.LogError("Failed to find a valid spawn position for the queen ant.");
            }
        }

        private bool IsPositionSafeFromContainers(int x, int y, int z)
        {
            int maxX = Blocks.GetLength(0) - 1;
            int maxY = Blocks.GetLength(1) - 1;
            int maxZ = Blocks.GetLength(2) - 1;

            // Define positions to check, ensuring they are within bounds.
            (int, int, int)[] positionsToCheck = {
                (Math.Max(0, x - 1), y, z),
                (Math.Min(maxX, x + 1), y, z),
                (x, Math.Max(0, y - 1), z),
                (x, Math.Min(maxY, y + 1), z),
                (x, y, Math.Max(0, z - 1)),
                (x, y, Math.Min(maxZ, z + 1))
            };

            foreach (var (checkX, checkY, checkZ) in positionsToCheck)
            {
                // Check if the position is within bounds and if the block is a container block.
                if (IsValidPosition(checkX, checkY, checkZ))
                {
                    var block = Blocks[checkX, checkY, checkZ];
                    if (block is ContainerBlock)
                    {
                        return false; 
                    }
                }
            }

            return true; // No adjacent container blocks, position is safe.
        }

        // Used for UI text count
        public int CountNestBlocks()
        {
            int count = 0;
            for (int x = 0; x < Blocks.GetLength(0); x++)
            {
                for (int y = 0; y < Blocks.GetLength(1); y++)
                {
                    for (int z = 0; z < Blocks.GetLength(2); z++)
                    {
                        if (Blocks[x, y, z] != null && Blocks[x, y, z] is NestBlock && Blocks[x, y, z].isVisible())
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves an abstract block type at the desired world coordinates.
        /// </summary>
        public AbstractBlock GetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate >= Blocks.GetLength(0) ||
                WorldYCoordinate >= Blocks.GetLength(1) ||
                WorldZCoordinate >= Blocks.GetLength(2)
            )
                return new AirBlock();

            return Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate];
        }

        /// <summary>
        /// Retrieves an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public AbstractBlock GetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate >= Blocks.GetLength(0) ||
                LocalYCoordinate >= Blocks.GetLength(1) ||
                LocalZCoordinate >= Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate >= Blocks.GetLength(0) ||
                ChunkYCoordinate >= Blocks.GetLength(1) ||
                ChunkZCoordinate >= Blocks.GetLength(2) 
            )
                return new AirBlock();

            return Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ];
        }

        /// <summary>
        /// sets an abstract block type at the desired world coordinates.
        /// </summary>
        public void SetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate, AbstractBlock toSet)
        {
            if (IsValidPosition(WorldXCoordinate, WorldYCoordinate, WorldZCoordinate))
            {
                Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate] = toSet;
                SetChunkContainingBlockToUpdate(WorldXCoordinate, WorldYCoordinate, WorldZCoordinate);
            }
            else
            {
                Debug.LogError("Attempted to set a block out of bounds.");
            }
        }

        /// <summary>
        /// sets an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public void SetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate,
            AbstractBlock toSet)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate > Blocks.GetLength(0) ||
                LocalYCoordinate > Blocks.GetLength(1) ||
                LocalZCoordinate > Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate > Blocks.GetLength(0) ||
                ChunkYCoordinate > Blocks.GetLength(1) ||
                ChunkZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }
            Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ] = toSet;

            SetChunkContainingBlockToUpdate
            (
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            );
        }

        #endregion

        #region Helpers

        #region Blocks

        /// <summary>
        /// Is responsible for generating the base, acid, and spheres.
        /// </summary>
        private void GenerateData()
        {
            GeneratePreliminaryWorld();
            GenerateAcidicRegions();
            GenerateSphericalContainers();
        }

        /// <summary>
        /// Generates the preliminary world data based on perlin noise.
        /// </summary>
        private void GeneratePreliminaryWorld()
        {
            for (int x = 0; x < Blocks.GetLength(0); x++)
                for (int z = 0; z < Blocks.GetLength(2); z++)
                {
                    /**
                     * These numbers have been fine-tuned and tweaked through trial and error.
                     * Altering these numbers may produce weird looking worlds.
                     **/
                    int stoneCeiling = SimplexNoise.GetPerlinNoise(x, 0, z, 10, 3, 1.2) +
                                       SimplexNoise.GetPerlinNoise(x, 300, z, 20, 4, 0) +
                                       10;
                    int grassHeight = SimplexNoise.GetPerlinNoise(x, 100, z, 30, 10, 0);
                    int foodHeight = SimplexNoise.GetPerlinNoise(x, 200, z, 20, 5, 1.5);

                    for (int y = 0; y < Blocks.GetLength(1); y++)
                    {
                        if (y <= stoneCeiling)
                        {
                            Blocks[x, y, z] = new StoneBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight)
                        {
                            Blocks[x, y, z] = new GrassBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight + foodHeight)
                        {
                            Blocks[x, y, z] = new MulchBlock();
                        }
                        else
                        {
                            Blocks[x, y, z] = new AirBlock();
                        }
                        if
                        (
                            x == 0 ||
                            x >= Blocks.GetLength(0) - 1 ||
                            z == 0 ||
                            z >= Blocks.GetLength(2) - 1 ||
                            y == 0
                        )
                            Blocks[x, y, z] = new ContainerBlock();
                    }
                }
        }

        /// <summary>
        /// Alters a pre-generated map so that acid blocks exist.
        /// </summary>
        private void GenerateAcidicRegions()
        {
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Acidic_Regions; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = -1;
                for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                {
                    if (Blocks[xCoord, j, zCoord] as AirBlock == null)
                    {
                        yCoord = j;
                        break;
                    }
                }

                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HX < xCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HZ < zCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HY < yCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Acidic_Region_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                if (Blocks[CX, CY, CZ] as AirBlock != null)
                                    Blocks[CX, CY, CZ] = new AcidicBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Alters a pre-generated map so that obstructions exist within the map.
        /// </summary>
        private void GenerateSphericalContainers()
        {

            //Generate hazards
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Conatiner_Spheres; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = RNG.Next(0, Blocks.GetLength(1));


                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX < xCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ < zCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY < yCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Conatiner_Sphere_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                Blocks[CX, CY, CZ] = new ContainerBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a world coordinate, tells the chunk holding that coordinate to update.
        /// Also tells all 4 neighbours to update (as an altered block might exist on the
        /// edge of a chunk).
        /// </summary>
        /// <param name="worldXCoordinate"></param>
        /// <param name="worldYCoordinate"></param>
        /// <param name="worldZCoordinate"></param>

        private void SetChunkContainingBlockToUpdate(int worldXCoordinate, int worldYCoordinate, int worldZCoordinate) {
            if (!IsValidPosition(worldXCoordinate, worldYCoordinate, worldZCoordinate)) {
                Debug.LogError($"Attempted to update chunk out of bounds at {worldXCoordinate}, {worldYCoordinate}, {worldZCoordinate}.");
                return;
            }

            // Calculate chunk indices based on world coordinates.
            int updateX = worldXCoordinate / ConfigurationManager.Instance.Chunk_Diameter;
            int updateY = worldYCoordinate / ConfigurationManager.Instance.Chunk_Diameter;
            int updateZ = worldZCoordinate / ConfigurationManager.Instance.Chunk_Diameter;

            // Ensure the calculated chunk indices are within the bounds of the Chunks array.
            if (updateX >= 0 && updateX < Chunks.GetLength(0) &&
                updateY >= 0 && updateY < Chunks.GetLength(1) &&
                updateZ >= 0 && updateZ < Chunks.GetLength(2)) {

                // Mark the corresponding chunk as needing an update.
                Chunks[updateX, updateY, updateZ].updateNeeded = true;
                
                // Flag all 6 neighbors for update as well, with bounds checking.
                FlagNeighborForUpdate(updateX - 1, updateY, updateZ);
                FlagNeighborForUpdate(updateX + 1, updateY, updateZ);
                FlagNeighborForUpdate(updateX, updateY - 1, updateZ);
                FlagNeighborForUpdate(updateX, updateY + 1, updateZ);
                FlagNeighborForUpdate(updateX, updateY, updateZ - 1);
                FlagNeighborForUpdate(updateX, updateY, updateZ + 1);
            } else {
                Debug.LogError($"Calculated chunk indices out of bounds: X={updateX}, Y={updateY}, Z={updateZ}");
            }
        }

        // Helper method to flag neighbor chunks for update with bounds checking.
        private void FlagNeighborForUpdate(int x, int y, int z) {
            if (x >= 0 && x < Chunks.GetLength(0) &&
                y >= 0 && y < Chunks.GetLength(1) &&
                z >= 0 && z < Chunks.GetLength(2)) {
                Chunks[x, y, z].updateNeeded = true;
            }
        }


        #endregion

        #region Chunks

        /// <summary>
        /// Takes the world data and generates the associated chunk objects.
        /// </summary>
        private void GenerateChunks()
        {
            GameObject chunkObg = new GameObject("Chunks");

            for (int x = 0; x < Chunks.GetLength(0); x++)
                for (int z = 0; z < Chunks.GetLength(2); z++)
                    for (int y = 0; y < Chunks.GetLength(1); y++)
                    {
                        GameObject temp = new GameObject();
                        temp.transform.parent = chunkObg.transform;
                        temp.transform.position = new Vector3
                        (
                            x * ConfigurationManager.Instance.Chunk_Diameter - 0.5f,
                            y * ConfigurationManager.Instance.Chunk_Diameter + 0.5f,
                            z * ConfigurationManager.Instance.Chunk_Diameter - 0.5f
                        );
                        Chunk chunkScript = temp.AddComponent<Chunk>();
                        chunkScript.x = x * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.y = y * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.z = z * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.Init(blockMaterial);
                        chunkScript.GenerateMesh();
                        Chunks[x, y, z] = chunkScript;
                    }
        }

        #endregion

        #endregion
    }
}
