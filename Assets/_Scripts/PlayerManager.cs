using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerManager : NetworkBehaviour {

    private class Coord{

        public int x;
        public int y;

        public Coord(int x_, int y_){
            x = x_;
            y = y_;
        }

    }

    // ===Settings===
    public static float sec_per_frame = 0.175F; //Recommended 0.175

    // ===Static Player Data===
    // Server will set up this ID.
    [SyncVar]
    public int myNum;

    // set in lobby hook
    [SyncVar]
    public Color myColor;

    // set in lobby hook
    [SyncVar]
    public string myName;


    // ===Dynamic Player Data===
    //Movement
    private char desiredDirection = 'e';
    private char currentDirection = 'e';
    public float cooldown = sec_per_frame;
    private bool dead = false;

    private Vector3 prevTransform;
    private Vector3 currTransform;

    //Body
    private Queue<Coord> body = new Queue<Coord>();
    private int length = 5;

    //Arena Manager
    private ArenaManager arenaManager = null;
    private ArenaManager getAM (){
        if (arenaManager == null){
            arenaManager = GameObject.Find("ArenaManager").GetComponent<ArenaManager>();
        }
        return arenaManager;
    }


	// Use this for initialization
	void Start () {
        
	}

    // Called on EVERY NetworkBehavior when it is activated on a client.
    // OnStartClient runs just before OnStartLocalPlayer
    // Invoked after clients have connected
    // Im ASSUMING this happens only on clients???
	public override void OnStartClient(){
        base.OnStartClient();
	}

    // Happens AFTER onStartClient. 
    // Runs only on a single client. (local player)
    // Runs AFTER OnStartClient.
	public override void OnStartLocalPlayer(){

        //isLocalPlayer is true.
        base.OnStartLocalPlayer();
        Cmd_Register();
	}

    [Command]
    public void Cmd_Register(){
        ArenaManager am = getAM();
        am.AddPlayer(gameObject);


    }

	private void decompose(){
        
    }

	// Update is called once per frame
	void Update () {

        //Potentially move player object.
        if (!isLocalPlayer){
            return;
        }

        if(dead){
            decompose();
        }

        GrabInput();
        cooldown -= Time.deltaTime;
        if (cooldown <= 0){
            cooldown = sec_per_frame;
            MovePlayerUnit();
        }

        if(MovedLastFrame()){
            MoveSnakeForward();
        }

	}



	private bool MovedLastFrame(){
        currTransform = gameObject.GetComponent<Transform>().position;
        bool result = currTransform != prevTransform;
        prevTransform = currTransform;
        return result;
    }

    private void GrabInput(){
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)){
            if(GoingHor()){
                desiredDirection = 'n';
            }   
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)){
            if (!GoingHor()){
                desiredDirection = 'w';
            }
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)){
            if (GoingHor()){
                desiredDirection = 's';
            }
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)){
            if (!GoingHor()){
                desiredDirection = 'e';
            }
        }



    }
    private bool GoingHor(){
        return currentDirection == 'e' || currentDirection == 'w';

    }
	
    private void MovePlayerUnit(){
        
        Vector3 differential = Vector3.zero;
        switch(desiredDirection){
            case 'e': 
                differential.x = 1;
                break;
            case 's': 
                differential.y = -1;
                break;
            case 'w':
                differential.x = -1;
                break;
            case 'n':
                differential.y = 1;
                break;
        }

        currentDirection = desiredDirection;
        gameObject.transform.Translate(differential);
      

    }

    private void MoveSnakeForward(){

        // Get position data.
        Vector3 newPos = gameObject.transform.position;
        Coord coordToClaim = new Coord((int)(newPos.x), (int)(newPos.y));

        // Claim
        body.Enqueue(coordToClaim);
        arenaManager.Cmd_RequestClaim(myNum, coordToClaim.x, coordToClaim.y);

        // Free
        while (body.Count > length){
            Coord coordToFree = body.Dequeue();
            arenaManager.Cmd_RequestFree(coordToFree.x, coordToFree.y);
        }

    }

    private void die(){
        dead = true;
    }
}
