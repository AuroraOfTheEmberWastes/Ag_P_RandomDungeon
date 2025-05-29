using System;
using System.Collections;
using System.Collections.Generic;
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



	private void Start()
    {
        roomCount = 1; finishedRoomCount = 0; doorCount = 0;
        rooms = new RectInt[arraySize];
        finishedRooms = new RectInt[arraySize];
        doors = new RectInt[arraySize];


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

        if (stuck && roomCount != 0)
        {
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
			//throw new IndexOutOfRangeException("Index out of bounds");
			Debug.Log("Index out of bounds");
			return;
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


		finishedRooms[finishedRoomCount] = new RectInt(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height);
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
                if (!CheckDoubleDoor(door))
                {
					if (doorCount == arraySize)
					{
						IncreaseArraySize();
					}
                    doors[doorCount] = door;
                    doorCount++;
                }
			}
        }


        yield return new WaitForSeconds(speed);
		if (roomIndex < finishedRoomCount - 1) StartCoroutine(MakeDoors(roomIndex + 1));
		else
		{
			Debug.Log("Doors Done");
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

	private bool CheckDoubleDoor(RectInt door)
	{
		for (int i = 0; i < doorCount; i++)
		{
			if (AlgorithmsUtils.Intersects(door, doors[i])) return true;
		}
		return false;
	}
}
