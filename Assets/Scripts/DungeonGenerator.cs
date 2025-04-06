using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class DungeonGenerator : MonoBehaviour
{
    public int x,y;
    public int initialHeight, initialWidth;

    public float speed = 0.1f;
    public int minArea = 100;
    public int minWidth = 10;
    public int minHeight = 10;

    public int seed = 000;
    public bool randomizeSeed;
    private int splitNum, splitDiv, splitMod;

    private int arraySize = 50;
    private RectInt[] rooms;
	public int roomCount = 1;





	private void Start()
    {
        roomCount = 1;
        rooms = new RectInt[arraySize];


        rooms[0] = new(x, y, initialWidth, initialHeight);


        if (randomizeSeed){
            seed = Random.Range(0,999);
        }
        splitNum = seed / 100 % 10;
        splitDiv = seed / 10 % 10; if (splitDiv <= 2) splitDiv = 3;
        splitMod = seed % 10; if (splitMod <= 2) splitMod = 3;


        StartCoroutine(Split(0));
    }



    private void Update()
    {


        for (int i = 0; i < roomCount; i++)
        {
            AlgorithmsUtils.DebugRectInt(rooms[i], Color.blue, 0.1f);
        }



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

        
        //RectInt room1 = SpawnRoom(rooms[roomIndex].x + 5, rooms[roomIndex].y + 5, rooms[roomIndex].width / 2 - 10, rooms[roomIndex].height - 10);
        //RectInt room2 = SpawnRoom(rooms[roomIndex].x + rooms[roomIndex].width / 2 + 5, rooms[roomIndex].y + 5, rooms[roomIndex].width / 2 - 10, rooms[roomIndex].height - 10);

		RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width / 2, rooms[roomIndex].height);
        RectInt room2 = SpawnRoom(rooms[roomIndex].x + rooms[roomIndex].width / 2, rooms[roomIndex].y, rooms[roomIndex].width / 2, rooms[roomIndex].height);

        if (room1 == room2) return;

        RemoveRoomAtIndex(roomIndex);
    }
    private void HorizontalSplit(int roomIndex)
    {

        //RectInt room1 = SpawnRoom(rooms[roomIndex].x + 5, rooms[roomIndex].y + 5, rooms[roomIndex].width - 10, rooms[roomIndex].height / 2 - 10);
        //RectInt room2 = SpawnRoom(rooms[roomIndex].x + 5, rooms[roomIndex].y + rooms[roomIndex].height / 2 + 5, rooms[roomIndex].width - 10, rooms[roomIndex].height / 2 - 10);

		RectInt room1 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y, rooms[roomIndex].width, rooms[roomIndex].height / 2);
        RectInt room2 = SpawnRoom(rooms[roomIndex].x, rooms[roomIndex].y + rooms[roomIndex].height / 2, rooms[roomIndex].width, rooms[roomIndex].height / 2);

		if (room1 == room2) return;

		RemoveRoomAtIndex(roomIndex);
    }

    private RectInt SpawnRoom(int x, int y, int width, int height)
    {
        if (width * height <= minArea) return new RectInt(); 
        if (height < minHeight) return new RectInt();
        if (width < minWidth) return new RectInt();

        RectInt newRoom = new(x, y, width, height);

        


		rooms[roomCount] = newRoom;
		roomCount++;

        if (roomCount == arraySize){
            IncreaseArraySize();
        }

        return newRoom;
    }

    private void RemoveRoomAtIndex(int indexToRemove)
    {
        Debug.Log(indexToRemove);
        //Check if the index is valid
        if (indexToRemove < 0 || indexToRemove >= roomCount)
        {
            throw new IndexOutOfRangeException("Index out of bounds");
        }


        //Shift all elements to the left starting from the index to remove to the end of the array and decrement the count

        for (int i = indexToRemove + 1; i < roomCount; i++)
        {
            rooms[i - 1] = rooms[i];
        }
        rooms[roomCount - 1] = new RectInt();

        roomCount--;

    }

    private void IncreaseArraySize()
    {
        RectInt[] tempArray = rooms;
        arraySize *= 2;

        rooms = new RectInt[arraySize];

        for (int i = 0; i < tempArray.Length; i++)
        {
            rooms[i] = tempArray[i];
        }

    }




    IEnumerator Split(int roomIndex = 0)
	{
		if (roomIndex >= roomCount) roomIndex = 0;

		if (ChooseSplit()){
            VerticalSplit(roomIndex);
        }else{
            HorizontalSplit(roomIndex);
		}
		
        yield return new WaitForSeconds(speed);
        //Debug.Log("waited");

        StartCoroutine(Split(roomIndex+1));

	}

}
