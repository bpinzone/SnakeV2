using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Prototype.NetworkLobby;

public class NetworkLobbyHook : LobbyHook {
	// only runs on server.
	// RUns when we transition from the lobby scene to the game scene
	public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer){
		LobbyPlayer lobbyPlayerScript= lobbyPlayer.GetComponent<LobbyPlayer>();
		PlayerManager playerManagerScript = gamePlayer.GetComponent<PlayerManager>();

		playerManagerScript.myColor = lobbyPlayerScript.playerColor;
		playerManagerScript.myName = lobbyPlayerScript.playerName;
	}
}
