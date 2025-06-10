using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    //initial conditions
    public int x,y;
    public int initialHeight, initialWidth;
	public int minArea = 5000;
	public int minWidth = 50;
	public int minHeight = 50;

	//operational parameters
	public float speed = 0.1f;

    //RNJesus
    public int seed = 00000;
    public bool randomizeSeed;
    public int splitSize;
    private bool stuck = false;
	private bool stuckAgain = false;
    private System.Random random;

	//General Array stuff
	private int arraySize = 50;

	//Room generation array
	private RectInt[] rooms;
	public int roomCount = 1;
    public bool showRooms = true;

    //Rooms Set Aside
    private RectInt[] finishedRooms;
    public int finishedRoomCount = 0;

    //Doors
    private RectInt[] doors;
    public int doorCount = 0;

	//Graph
	private Dictionary<Vector3, (int locationID, Vector3[] neighbors, bool isDoor)> graph; //when reading, will need to break loop when Vector3 = (0,0,0)

	//DFS
	private Vector3[] checkedLocations;
	private Vector3 graphSearchStart = new Vector3();
	public int checkedLocationsCount = 0;

	//spawning
	private int[,] tilemap;
	public GameObject floorPrefab;
	public GameObject wallPrefab;
	public GameObject doorPrefab;


	private void Start()
    {
        roomCount = 1; finishedRoomCount = 0; doorCount = 0; checkedLocationsCount = 0;
        rooms = new RectInt[arraySize];
        finishedRooms = new RectInt[arraySize];
        doors = new RectInt[arraySize];
		graph = new Dictionary<Vector3, (int locationID, Vector3[] neighbors, bool isDoor)> { };
		checkedLocations = new Vector3[arraySize];
		tilemap = new int[initialHeight, initialWidth];


        rooms[0] = new(x, y, initialWidth, initialHeight);

        random = new(seed);
        if (randomizeSeed){
            seed = (int)DateTime.Now.Ticks;
            random = new(seed);
        }

        StartCoroutine(Split(0));
    }



    private void Update()
    {

        if (showRooms)
		{
			for (int i = 0; i < roomCount; i++)
			{
				AlgorithmsUtils.DebugRectInt(rooms[i], Color.blue, 0.01f);
			}
            for (int i = 0; i < finishedRoomCount; i++)
            {
                AlgorithmsUtils.DebugRectInt(finishedRooms[i], Color.green, 0.01f);
            }
			for (int i = 0; i < doorCount; i++)
			{
				AlgorithmsUtils.DebugRectInt(doors[i], Color.yellow, 0.04f);
			}
		}
    }

    private void OnDrawGizmos()
    {
		Gizmos.color = Color.white;
		if (graph != null)
        {
			foreach (Vector3 key in graph.Keys)
            {
				if (graph[key].isDoor) Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(key, 0.5f);
				Gizmos.color = Color.white;
				foreach (Vector3 neighbor in graph[key].neighbors) 
				{
					if (neighbor == new Vector3(0, 0, 0)) break;
					Gizmos.DrawLine(key,neighbor);
				}
            }

        }
	}


    //
    //Doing Splits
    //

    private void VerticalSplit(int roomIndex)
    {
        int splitDifference = random.Next(-splitSize,splitSize);

		//check that rooms aren't too small
		if ((rooms[roomIndex].width / 2 + splitDifference) * rooms[roomIndex].height < minArea) return;
		if ((rooms[roomIndex].width / 2 - splitDifference) * rooms[roomIndex].height < minArea) return;
        if (rooms[roomIndex].width / 2 + splitDifference< minWidth) return;
        if (rooms[roomIndex].width / 2 - splitDifference< minWidth) return;

		stuck = false;

        if (rooms[roomIndex].width % 2 == 1)
		{
			RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width / 2 + splitDifference, rooms[roomIndex].height);
			RectInt room2 = SpawnRoom(rooms[roomIndex].x + rooms[roomIndex].width / 2 + splitDifference - 1, rooms[roomIndex].y, rooms[roomIndex].width / 2 - splitDifference + 2, rooms[roomIndex].height);
		}else
        {
			RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width / 2 + splitDifference, rooms[roomIndex].height);
			RectInt room2 = SpawnRoom(rooms[roomIndex].x + rooms[roomIndex].width / 2 + splitDifference - 1, rooms[roomIndex].y, rooms[roomIndex].width / 2 - splitDifference + 1, rooms[roomIndex].height);
		}




        RemoveRoomAtIndex(roomIndex, rooms);
    }

    private void HorizontalSplit(int roomIndex)
	{
		int splitDifference = random.Next(-splitSize, splitSize);
        

        //check that rooms aren't too small
		if (rooms[roomIndex].width * (rooms[roomIndex].height / 2 + splitDifference) < minArea) return;
        if (rooms[roomIndex].width * (rooms[roomIndex].height / 2 - splitDifference) < minArea) return;
        if (rooms[roomIndex].height / 2 + splitDifference< minHeight) return;
		if (rooms[roomIndex].height / 2 - splitDifference< minHeight) return;


		stuck = false;
        if (rooms[roomIndex].height % 2 == 1)
		{
			RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height / 2 + splitDifference);
			RectInt room2 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y + rooms[roomIndex].height / 2 + splitDifference - 1, rooms[roomIndex].width, rooms[roomIndex].height / 2 - splitDifference + 2);
		}else
		{
			RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height / 2 + splitDifference);
			RectInt room2 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y + rooms[roomIndex].height / 2 + splitDifference - 1, rooms[roomIndex].width, rooms[roomIndex].height / 2 - splitDifference + 1);
		}


            RemoveRoomAtIndex(roomIndex, rooms);
    }

	IEnumerator Split(int roomIndex = 0)
	{

		//Debug.Log(roomIndex + " " + roomCount);

        //make sure it doesn't go out of bounds
        if (roomIndex >= roomCount)
		{
            //if (stuck) SetRoomAside(0);
			roomIndex = 0;
			if (stuck) stuckAgain = true;
			stuck = true;

		}
		//Debug.Log(stuck);

		if (rooms[roomIndex].width * rooms[roomIndex].height  <= 2 * minArea)
        {
            SetRoomAside(roomIndex);
        }
		else if (random.Next(0,2) == 1)
		{
			VerticalSplit(roomIndex);
		}
		else
		{
			HorizontalSplit(roomIndex);
		}

        if (stuckAgain && roomCount != 0)
        {
			stuckAgain = false;
            SetRoomAside(roomIndex);
        }

		yield return new WaitForSeconds(speed);

        if (roomCount != 0) StartCoroutine(Split(roomIndex + 1));
        else
        {
            Debug.Log("Splitting done");
            StartCoroutine(MakeDoors());
        }

	}

	private RectInt SpawnRoom(int x, int y, int width, int height)
	{
		//Verify room size, redundant
		//if (width * height <= minArea) return new RectInt(); 
		//if (height < minHeight) return new RectInt();
		//if (width < minWidth) return new RectInt();

		//Debug.Log(width *  height);

		RectInt newRoom = new(x, y, width, height);




		rooms[roomCount] = newRoom;
		roomCount++;

		if (roomCount == arraySize)
		{
			IncreaseArraySize();
		}

		return newRoom;
	}

	private void RemoveRoomAtIndex(int indexToRemove, RectInt[] array)
	{
		//Debug.Log(indexToRemove);
		//Check if the index is valid
		if (indexToRemove < 0 || indexToRemove >= roomCount)
		{
			Debug.Log(indexToRemove);
			throw new IndexOutOfRangeException("Index out of bounds");
			//Debug.Log("Index out of bounds");
			//return;
		}

		//Show the rool being deleted
		AlgorithmsUtils.DebugRectInt(array[indexToRemove], Color.red, 1f);


		//Shift all elements to the left starting from the index to remove to the end of the array and decrement the count

		for (int i = indexToRemove + 1; i < roomCount; i++)
		{
			array[i - 1] = array[i];
		}
		array[roomCount - 1] = new RectInt();

		roomCount--;

	}

		//
		//Array Finangling
		//



	private void IncreaseArraySize()
    {
        RectInt[] tempArray;
        arraySize *= 2;

		//rooms
		tempArray = rooms;
		rooms = new RectInt[arraySize];

        for (int i = 0; i < tempArray.Length; i++)
        {
            rooms[i] = tempArray[i];
        }

        //finishedRooms
        tempArray = finishedRooms;
        finishedRooms = new RectInt[arraySize];

		for (int i = 0; i < tempArray.Length; i++)
		{
			finishedRooms[i] = tempArray[i];
		}

        //doors
        tempArray = doors;
        doors = new RectInt[arraySize];

        for (int i = 0;i < tempArray.Length; i++)
        {
            doors[i] = tempArray[i];
        }

		//locations general
		Vector3[] differentTempArray = new Vector3[arraySize];
		differentTempArray = checkedLocations;
		checkedLocations = new Vector3[arraySize];

		for (int i = 0;i < tempArray.Length; i++)
		{
			checkedLocations[i] = differentTempArray[i];
		}

	}


		//
		//setting rooms[i] into finishedRooms[]
		//

    private void SetRoomAside(int roomIndex)
	{
        //Debug.Log(roomIndex);

		if (finishedRoomCount == arraySize)
		{
			IncreaseArraySize();
		}

		//room
		finishedRooms[finishedRoomCount] = new RectInt(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height);

		//graph
		Vector3 center = new Vector3(rooms[roomIndex].x + rooms[roomIndex].width / 2 + 0.5f, 0, rooms[roomIndex].y + rooms[roomIndex].height / 2 + 0.5f);
		graph.Add(center, (finishedRoomCount, new Vector3[20], false));
        

        if (graphSearchStart == new Vector3()) graphSearchStart = center;


		//Bach to regularly scheduled construction

        finishedRoomCount++;

        RemoveRoomAtIndex(roomIndex, rooms);



	}

		//
		//generating doors
		//

    IEnumerator MakeDoors(int roomIndex = 0)
    {
        for (int i = roomIndex + 1; i < finishedRoomCount; i++)
        {
			RectInt door = SpawnDoor(roomIndex, i);
			if (door != new RectInt())
            {
				if (doorCount == arraySize)
				{
					IncreaseArraySize();
				}
                doors[doorCount] = door;

                //graph
                AddDoorToGraph(door, roomIndex, i);


                doorCount++;
            }
        }

        yield return new WaitForSeconds(speed);
		if (roomIndex < finishedRoomCount - 1) StartCoroutine(MakeDoors(roomIndex + 1));
		else
		{
			Debug.Log("Doors Done");
			StartDFS();
		} 
        
    }

	private RectInt SpawnDoor(int checkedRoom, int comparedRoom)
	{
		RectInt door = AlgorithmsUtils.Intersect(finishedRooms[checkedRoom], finishedRooms[comparedRoom]);
		if (door == new RectInt()) return door;
		if (door.width < 3 && door.height < 3) return new RectInt();


		if (door.height == 1)
		{
			door = new(random.Next(door.x + 1,door.x + door.width - 1), door.y, 1, 1);
		}
		else
		{
			door = new(door.x, random.Next(door.y + 1, door.y + door.height - 1), 1, 1);
		}


		return door;
	}

	private void AddDoorToGraph(RectInt door, int room1, int room2)
	{
        Vector3 center = new Vector3(door.x + door.width / 2 + 0.5f, 0, door.y + door.height / 2 + 0.5f);
        Vector3 room1Center = new Vector3(finishedRooms[room1].x + finishedRooms[room1].width / 2 + 0.5f, 0, finishedRooms[room1].y + finishedRooms[room1].height / 2 + 0.5f);
        Vector3 room2Center = new Vector3(finishedRooms[room2].x + finishedRooms[room2].width / 2 + 0.5f, 0, finishedRooms[room2].y + finishedRooms[room2].height / 2 + 0.5f);

        //graph.Remove;
        //graph.ContainsKey;


        graph.Add(center, (doorCount, new Vector3[] { room1Center, room2Center }, true));

        if (graph.ContainsKey(room1Center))
		{
			int id = graph[room1Center].locationID;
			
			Vector3[] neighbors = new Vector3[20];
			neighbors = graph[room1Center].neighbors;

			for (int i = 0; i < 20; i++)
			{
				if (neighbors[i] == new Vector3())
				{
					neighbors[i] = center;
					break;
				}
			}
			graph.Remove(room1Center);
			graph.Add(room1Center, (room1, neighbors, false));

        }
		else graph.Add(room1Center, (room1, new Vector3[] { center }, false));

        if (graph.ContainsKey(room2Center))
		{
			int id = graph[room2Center].locationID;

            Vector3[] neighbors = new Vector3[graph[room2Center].neighbors.Length + 1];
            neighbors = graph[room2Center].neighbors;

            for (int i = 0; i < 20; i++)
            {
                if (neighbors[i] == new Vector3())
                {
                    neighbors[i] = center;
                    break;
                }
            }
            graph.Remove(room2Center);
			graph.Add(room2Center, (room1, neighbors, false));
		}
		else graph.Add(room2Center, (room2, new Vector3[] { center }, false));

		//Debug.Log(graph[center].neighbors);

    }

		//
		//checking connectivity
		//

	private void StartDFS()
	{
		checkedLocations[checkedLocationsCount] = graphSearchStart;
		checkedLocationsCount++;
		CheckConnectivity(graphSearchStart);
		if (checkedLocationsCount != finishedRoomCount + doorCount) Debug.Log("All rooms not connected");
		else
		{
			Debug.Log("Everything connected");
			StartCoroutine(SpawnDungeon(0, 0));
		}
	}

	private void CheckConnectivity(Vector3 location)
	{
		foreach (var neighbor in graph[location].neighbors)
		{
			if (neighbor == new Vector3(0, 0, 0)) break;
			if (checkedLocations.Contains(neighbor)) continue;

			if (checkedLocationsCount == arraySize)
			{
				IncreaseArraySize();
			}

			if (graph[location].isDoor) LocationToTilemap(doors[graph[location].locationID]);
			else LocationToTilemap(finishedRooms[graph[location].locationID]);

			checkedLocations[checkedLocationsCount] = neighbor;
			checkedLocationsCount++;

			CheckConnectivity(neighbor);
		}
	}

		//
		//Tilemap + dungeon spawning
		//

	private void LocationToTilemap(RectInt location)
	{
		if (location.width == 1 && location.height == 1)
		{
			tilemap[location.x, location.y] = -1;
		}else
		{
			for (int i = location.x;i < location.x + location.width; i++)
			{
				if (tilemap[i, location.y] == 0)
				{
					tilemap[i,location.y] = 1;
				}
				if (tilemap[i, location.y + location.height - 1] == 0)
				{
					tilemap[i, location.y + location.height - 1] = 1;
				}
			}

			for (int i = location.y + 1;i < location.y + location.height - 1;i++)
			{
				if (tilemap[location.x, i] == 0)
				{
					tilemap[location.x, i] = 1;
				}
				if (tilemap[location.x + location.width - 1, i] == 0)
				{
					tilemap[location.x + location.width - 1, i] = 1;
				}
			}
		}
	}

	IEnumerator SpawnDungeon(int i, int j)
	{
		if (tilemap[i, j] == 1)
		{
			Instantiate(wallPrefab, new Vector3(i, 0, j), Quaternion.identity);
		} else if (tilemap[i,j] == -1)
		{
			Instantiate(doorPrefab, new Vector3(i ,0 ,j), Quaternion.identity); 
		}else
		{
			Instantiate(floorPrefab, new Vector3(i, 0, j), Quaternion.identity);
		}




		yield return new WaitForSeconds(speed);
		if (j == initialHeight - 1 && i == initialWidth - 1) Debug.Log("Spawning Complete");
		else if (i == initialWidth - 1)
		{
			StartCoroutine(SpawnDungeon(0, j + 1));
		}
		else
		{
			StartCoroutine(SpawnDungeon(i + 1, j));
		}
	}


		
}
