using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ArenaManager : NetworkBehaviour {

    // Purpose is really just to catch up a client when they connect and keep track of square objects.
    // These should only be commands that got through AFTER claim logic.
    // In other words, they actually got sent out to square managers.
    // state and lastCommandIssued shouldn't ever update on client.
    public class SquareData {
        //The square game object
        public GameObject square;
        // Neither of the two below reflect ACTUAL state. They reflect requested state.
        // Last state issued
        public int state;
        // Last command id issued
        // If this is -1, its never been changed.
        public int lastCommandID;
    }

    private int commandsIssued = 0;

    public List<GameObject> players = new List<GameObject>();

    //prefab
    //TODO from editor. 
    //For spawning purposes only.
    public GameObject squarePrefab;

    // num cols
    public int width = 52;

    // num rows
    public int height = 36; 

    //(0, 0) in the world is in the bottom right corner.
    //indexed by [col][row]
    //Real objects with sprites.
    public SquareData[,] squareData;

    //Hierarchy manager. All of these will be in the same position. 
    public GameObject[] cols;

    //hierarchy manager.
    public GameObject all;

    public bool finishedSetup = false;

    // Use this for initialization
    void Awake(){

        setupBoard();
    }

    // ==== LOW LEVEL LOCAL METHODS ====
    private void setupBoard(){
        //set up all object container.
        all = new GameObject("All Squares");
        all.transform.position = Vector2.zero;

        //set up row managers.
        cols = new GameObject[width];
        for (int colsDone = 0; colsDone < width; ++colsDone){
            cols[colsDone] = new GameObject("Col" + colsDone.ToString());
            cols[colsDone].transform.SetParent(all.transform);
            cols[colsDone].transform.position = Vector2.zero;
        }

        //set up individuals
        squareData = new SquareData[width, height];
        for (int colsDone = 0; colsDone < width; ++colsDone){
            for (int rowsDone = 0; rowsDone < height; ++rowsDone){
                // Debug.Log("Setting up tile");
                //create with a parent.
                GameObject currObject = Instantiate(squarePrefab, cols[colsDone].transform) as GameObject;
                //position.
                currObject.transform.position = new Vector2(colsDone, rowsDone);

                //rename
                currObject.name = "c" + colsDone.ToString() + "r" + rowsDone;

                // Init server data
                squareData[colsDone, rowsDone] = new SquareData();
                SquareData currSD = squareData[colsDone, rowsDone];
                currSD.square = currObject;
                currSD.lastCommandID = -1;

                //state
                currObject.GetComponent<SquareManager>().initOpen();
                currSD.state = SquareManager.OPEN;

                //init server data
                //border colors
                if (colsDone == 0 || colsDone == width - 1 ||
                    rowsDone == 0 || rowsDone == height - 1){

                    // state
                    currObject.GetComponent<SquareManager>().initWall();
                    currSD.state = SquareManager.WALL;
                }


            }

        }
        //todo SEND OUT FOOD.
        finishedSetup = true;
    }

    // ==== SERVER SETUP & CATCH UP METHODS ====
    [Server]
    public void AddPlayer(GameObject player){
        // players.Add
        players.Add(player);
        player.GetComponent<PlayerManager>().myNum = players.Count - 1;
        catchUp();
         
    }

    // Called when a new client connects.
    // Sends out all change data to all clients. 
    // Wish I could send it to just the one client,
    // but that is pretty involved. So just doing this for now.
    [Server]
    public void catchUp(){
        // Go through every square data and send claims/frees/foods...
        for (int colsDone = 0; colsDone < width; ++colsDone){
            for (int rowsDone = 0; rowsDone < height; ++rowsDone){
                SquareData sd = squareData[colsDone, rowsDone];
                if(sd.state == SquareManager.OPEN){
                    continue;
                }
                // Debug.Log("Catching up...");
                if(sd.state >= 0){
                    Color c = players[sd.state].GetComponent<PlayerManager>().myColor;
                    Rpc_ReceiveClaim(sd.state, c, colsDone, rowsDone, sd.lastCommandID);
                }
                else if(sd.state == SquareManager.FOOD){
                    Rpc_ReceiveFood(colsDone, rowsDone, sd.lastCommandID);
                } 

            }
        }        
    }

    // ======= Server proccess request ========
    // Actually handle logic of requests.
    [Command]
    public void Cmd_RequestClaim(int player, int x, int y){
        Debug.Log("Player attempting claim " + x.ToString() + " " + y.ToString() + " for player " + player.ToString());


        SquareManager sm = get_SM(x, y);
        PlayerManager pm = players[player].GetComponent<PlayerManager>();
        if(sm.isDeath()){
            Debug.Log("Killing player" + player.ToString());
            Debug.Log("Death due to coordinate: " + x.ToString() + " " + y.ToString());
            Debug.Log("Finished setup is: " + finishedSetup.ToString());
            pm.Rpc_Die();
            return;
        }
        if(sm.isFood()){
            Debug.Log("Growing");
            pm.Rpc_Grow();            
            Cmd_RequestFood();
        }
        SendClaim(player, x, y);

    }

    [Command]
    public void Cmd_RequestFree(int x, int y){
        SendFree(x, y);        
    }

    [Command]
    public void Cmd_RequestFood(){
        int x = 1;
        int y = 1;
        bool valid = false;
        while(!valid){
            x = Random.Range(1, width - 1);
            y = Random.Range(1, height - 1); 
            SquareManager sm = get_SM(x, y);
            valid = sm.isOpen();
            valid = x != 3;
        }
        SendFood(x, y);
    }

    // ========== Server actually sends an RPC ==========
    // Not to be used while catching up.
    [Server]
    private void SendClaim(int player, int x, int y){
        Debug.Log("Claiming " + x.ToString() + " " + y.ToString() + " for player " + player.ToString());
        squareData[x, y].state = player;
        squareData[x, y].lastCommandID = commandsIssued;

        PlayerManager pm = players[player].GetComponent<PlayerManager>();
        Color c = pm.myColor;
        Rpc_ReceiveClaim(player, c, x, y, commandsIssued);
        commandsIssued += 1;

    }
    [Server]
    private void SendFree(int x, int y){
        squareData[x, y].state = SquareManager.OPEN;
        squareData[x, y].lastCommandID = commandsIssued;

        Rpc_ReceiveFree(x, y, commandsIssued);
        commandsIssued += 1;

    }
    [Server]
    private void SendFood(int x, int y){
        squareData[x, y].state = SquareManager.FOOD;
        squareData[x, y].lastCommandID = commandsIssued;
        
        Rpc_ReceiveFood(x, y, commandsIssued);
        commandsIssued += 1;
    }
    
    // ==== Client receives RPC ====================
    [ClientRpc]
    public void Rpc_ReceiveClaim(int player, Color c, int x, int y, int commandID){
        SquareManager squareManager = get_SM(x, y);
        squareManager.makeClaim(player, c, commandID);

    }
    [ClientRpc]
    public void Rpc_ReceiveFree(int x, int y, int commandID){
        SquareManager squareManager = get_SM(x, y);
        squareManager.makeFree(commandID);
    }
    [ClientRpc]
    public void Rpc_ReceiveFood(int x, int y, int commandID){
        SquareManager squareManager = get_SM(x, y);
        squareManager.makeFood(commandID);
    }

    // ==== HELPERS ===========
    private SquareManager get_SM(int x, int y){
       SquareManager sm = squareData[x, y].square.GetComponent<SquareManager>();
       return sm;
    }
}
