using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NO NETWORKING!
public class SquareManager : MonoBehaviour {

    //Players are (0, 1, 2...)
    public static int WALL = -1;
    public static int OPEN = -2;
    public static int FOOD = -3;

    private int state = OPEN;

    // Make it accept anything first.
    private int lastCommandID = -1;

    private SpriteRenderer sr;

    void Awake(){
        sr = gameObject.GetComponent<SpriteRenderer>();
    }
	void Start () {

	}

    // Informational
    public bool isDeath(){
        return isClaimed() || isWall(); 
    }
    public bool isClaimed(){
        return state >= 0;
    }
    public bool isWall(){
        return state == WALL;
    }
    public bool isFood(){
        return state == FOOD;
    }
    public bool isOpen(){
        return state == OPEN;
    }

    // Init

    public void initOpen(){
        state = OPEN;
        sr.color = Color.grey;
    }
    public void initWall(){
        state = WALL;
        sr.color = Color.black;
    }

    // Modify
    // Only changes if this commandID is larger than the last commandID.
    //playerNum = 0 -> first player.
    public void makeClaim(int playerNum, Color color, int commandID){
        if(commandID <= lastCommandID){
            return;
        }
        lastCommandID = commandID;
        state = playerNum;
        sr.color = color;
    }

    public void makeFree(int commandID){
        if (commandID <= lastCommandID){
            return;
        }
        lastCommandID = commandID;
        state = OPEN;
        sr.color = Color.grey;
    }

    public void makeFood(int commandID){
        if (commandID <= lastCommandID){
            return;
        }
        lastCommandID = commandID;
        state = FOOD;
        sr.color = Color.white;
    }
}
