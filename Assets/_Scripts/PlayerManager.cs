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

    public bool alive = true;
    [ClientRpc]
    public void Rpc_Die(){
        alive = false;
        length = 1;
    }

    // ===Dynamic Player Data===
    //Movement
    private char desiredDirection = 'e';
    private char currentDirection = 'e';
    public float cooldown = sec_per_frame;
    private Vector3 prevTransform;
    private Vector3 currTransform;

    //Body
    private Queue<Coord> body = new Queue<Coord>();
    public int length = 5;
    [ClientRpc]
    public void Rpc_Grow(){
        length += 1;
    }


    // todo remove
    public float countdown = 5.0f;

    //====Arena Manager & Registration===========
    private bool startedSearch = false;
    private bool managerFound = false;
    private bool sentRegistration = false;
    [SyncVar]
    public bool registered = false;
    private ArenaManager arenaManager = null;

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
        // Cmd_Register();
	}

    [Command]
    public void Cmd_Register(){
        if(arenaManager == null){
            // hack for now. component is never found on server becuase of local player filter in update.
            arenaManager = GameObject.Find("ArenaManager").GetComponent<ArenaManager>();
        }
        arenaManager.AddPlayer(gameObject);
        registered = true;
        Cmd_RequestFood();
    }
    // [Command]
    // public void Cmd_SpawnAM(){
    //     GameObject go = Instantiate(AMPrefab, Vector3.zero, Quaternion.identity) as GameObject;
    //     NetworkServer.SpawnWithClientAuthority(go, connectionToClient);
    // }

    IEnumerator FindArenaManager(){
        // Cmd_SpawnAM();
        GameObject AMObject = null;
        ArenaManager AM = null;
        while(AM == null){
            AMObject = GameObject.Find("ArenaManager");
            if(AMObject == null){
                yield return new WaitForSeconds(0.5f);
            }
            else{
                AM = AMObject.GetComponent<ArenaManager>();
                // AM.GetComponent<NetworkIdentity>().AssignClientAuthority(this.GetComponent<NetworkIdentity>().connectionToClient);
            }
        }
        arenaManager = AM;
        managerFound = true;
    }

	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer){
            return;
        }
        // Start Search For Manager
        if(!startedSearch){
            startedSearch = true;
            StartCoroutine("FindArenaManager");
            return;
        }
        if(managerFound && !sentRegistration){
            sentRegistration = true;
            Cmd_Register();
            return;
        }
        if(!registered){
            // Debug.Log("Not Registered");
            return;
        }

        // Debug.Log("I'm registered and the local player");


        if(countdown > 0){
            countdown -= Time.deltaTime;
            return;
        }


        // Handle cool down
        cooldown -= Time.deltaTime;
        bool isCoolFrame = false;
        if(cooldown <= 0){
            cooldown = sec_per_frame;
            isCoolFrame = true;

        }

        // Get input
        GrabInput();

        // If cool, move or decompose
        // LOOK. UNCOMMENT.
        if(isCoolFrame){
            if(alive){
                MovePlayerUnit();
                Cmd_MoveSnakeForward();
            }
            else{
                Cmd_Decompose();
            }
        }
        
        
        if(MovedLastFrame()){
            // Cmd_MoveSnakeForward();
        }

	}

	private bool MovedLastFrame(){
        if(countdown > 0){
            return false;
        }
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

    [Command]
    private void Cmd_MoveSnakeForward(){
        //TODO
        // Problems with authorized length...

        // Get position data.
        Vector3 newPos = gameObject.transform.position;
        Coord coordToClaim = new Coord((int)(newPos.x), (int)(newPos.y));

        // Attempt Claim
        body.Enqueue(coordToClaim);
        Debug.Log("Requesting claim when my name is:" + myName.ToString());
        arenaManager.Cmd_RequestClaim(myNum, coordToClaim.x, coordToClaim.y);

        // Free
        // If statement allows for growth
        if(body.Count > length){
            Coord coordToFree = body.Dequeue();
            arenaManager.Cmd_RequestFree(coordToFree.x, coordToFree.y);

        }

    }
    [Command]
	private void Cmd_Decompose(){
        // When you die, you keep one tile. 
        // You died, so the last coord you tried to claim is invalid,
        // so don't try to unclaim it.
        if(body.Count > 2){
            Coord coordToFree = body.Dequeue();
            arenaManager.Cmd_RequestFree(coordToFree.x, coordToFree.y);
        }
    }

    [Command]
    private void Cmd_RequestFood(){
        arenaManager.Cmd_RequestFood();
    }


}
