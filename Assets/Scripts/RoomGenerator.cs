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
    private int splitNum, splitMod, sizeInc, sizeMod, sizeNum;

    //General Array stuff
	private int arraySize = 50;

	//Room generation array
	private RectInt[] rooms;
	public int roomCount = 1;
    public bool showRooms = true;

    //Rooms Set Aside
    private RectInt[] finishedRooms;
    public int finishedRoomCount = 0;



	private void Start()
    {
        roomCount = 1; finishedRoomCount = 0;
        rooms = new RectInt[arraySize];
        finishedRooms = new RectInt[arraySize];


        rooms[0] = new(x, y, initialWidth, initialHeight);


        if (randomizeSeed){
            seed = Random.Range(10000,99999);
        }
        
        sizeNum = seed / 10000 % 10;
        sizeMod = seed / 1000 % 10; if (sizeMod <= 1) sizeMod = 2; 
        sizeInc = seed / 100 % 10; if (sizeInc == sizeMod) sizeInc++;

        splitNum = seed / 10 % 10;
        splitMod = seed % 10; if (splitMod <= 2) splitMod = 3;


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
            for (int i = 0;i < finishedRoomCount; i++)
            {
                AlgorithmsUtils.DebugRectInt(finishedRooms[i], Color.green, 0.01f);
            }
		}



    }


            //
            //Doing Splits
            //

    private int GetSplitRatio()
    {
        if (sizeNum == sizeMod) sizeNum--;

        int result = (sizeNum % sizeMod);

        sizeNum += sizeInc;

        return result;
    }

    private bool ChooseSplit()
    {
        int result = splitNum % splitMod / 2 % 2;
		splitNum++;

		if (result == 0) return false;
        else return true;
    }
    private void VerticalSplit(int roomIndex)
    {
        int splitRatio = GetSplitRatio();


        //check that rooms aren't too small
        if ((rooms[roomIndex].width / 10 * splitRatio + 5) * rooms[roomIndex].height < minArea) return;
        if ((rooms[roomIndex].width / 10 * (10 - splitRatio) + 5) * rooms[roomIndex].height < minArea) return;
        if (rooms[roomIndex].width / 10 * splitRatio < minWidth || rooms[roomIndex].width / 10 * (10 - splitRatio) < minWidth) return;


		RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width / 10 * splitRatio + 5, rooms[roomIndex].height);
        RectInt room2 = SpawnRoom(rooms[roomIndex].x + rooms[roomIndex].width / 10 * splitRatio - 5, rooms[roomIndex].y, rooms[roomIndex].width / 10 * (10 - splitRatio) + 10, rooms[roomIndex].height);


        RemoveRoomAtIndex(roomIndex, rooms);
    }
    private void HorizontalSplit(int roomIndex)
	{
		int splitRatio = GetSplitRatio();

		
        //check that rooms fit the requirements
		if (rooms[roomIndex].width * (rooms[roomIndex].height / 10 * splitRatio + 5) < minArea) return;
        if (rooms[roomIndex].width * (rooms[roomIndex].height / 10 * (10 - splitRatio) + 5) < minArea) return;
        if (rooms[roomIndex].height / 10 * splitRatio < minHeight || rooms[roomIndex].height / 10 * (10 - splitRatio) < minHeight) return;


		RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height / 10 * splitRatio + 5);
        RectInt room2 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y + rooms[roomIndex].height / 10 * splitRatio - 5, rooms[roomIndex].width, rooms[roomIndex].height / 10 * (10 - splitRatio) + 10);


		RemoveRoomAtIndex(roomIndex, rooms);
    }

	IEnumerator Split(int roomIndex = 0)
	{
        //make sure it doesn't go out of bounds
		if (roomIndex >= roomCount) roomIndex = 0;
		Debug.Log(roomIndex);

		if (rooms[roomIndex].width * rooms[roomIndex].height / (sizeMod - 1) <= minArea )
        {
            SetRoomAside(roomIndex);
        }
		else if (ChooseSplit())
		{
			VerticalSplit(roomIndex);
		}
		else
		{
			HorizontalSplit(roomIndex);
		}

		yield return new WaitForSeconds(speed);

        if (roomCount != 0) StartCoroutine(Split(roomIndex + 1));
        else Debug.Log("Splitting done");


	}


	        //
	        //Array Finangling
	        //

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

        if (roomCount == arraySize){
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
            throw new IndexOutOfRangeException("Index out of bounds");
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
}
