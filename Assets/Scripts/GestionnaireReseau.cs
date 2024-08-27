using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

public class GestionnaireReseau : MonoBehaviour, INetworkRunnerCallbacks {

    //Contient une r�f�rence au component NetworkRunner
    NetworkRunner _runner;

    // pour m�moriser le component GestionnaireMouvementPersonnage du joueur
    GestionnaireInputs gestionnaireInputs;

    //Index de la sc�ne du jeu
    public int IndexSceneJeu;

    public JoueurReseau joueurPrefab;

    void Start() {
        // Cr�ation d'une partie d�s le d�part
        CreationPartie(GameMode.AutoHostOrClient);
    }

    // Fonction asynchrone pour d�marrer Fusion et cr�er une partie
    async void CreationPartie(GameMode mode) 
        {
        /*  1.M�morisation du component NetworkRunner . On garde en m�moire
            la r�f�rence � ce component dans la variable _runner.
            2.Indique au NetworkRunner qu'il doit fournir les entr�es (inputs) au 
            simulateur (Fusion)
        */
        _runner = gameObject.GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        /*M�thode du NetworkRunner qui permet d'initialiser une partie
         * GameMode : re�u en argument. Valeur possible : Client, Host, Server,
           AutoHostOrClient, etc.)
         * SessionName : Nom de la chambre (room) pour cette partie
         * Scene : la sc�ne qui doit �tre utilis�e pour la simulation
         * SceneManager : r�f�rence au component script
          NetworkSceneManagerDefault qui est ajout� au m�me moment
         */
        await _runner.StartGame(new StartGameArgs() {
            GameMode = mode,
            SessionName = "ttt",
            Scene = SceneRef.FromIndex(IndexSceneJeu),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
        
    public void OnConnectedToServer(NetworkRunner runner) {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
       
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
       
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {
       
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
       
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {
  
    }

    /* Fonction du Runner pour d�finir les inputs du client dans la simulation
    * 1. On r�cup�re le component GestionnaireInputs du joueur local
    * 2. On d�finit (set) le param�tre input en lui donnant la structure de donn�es (struc) qu'on r�cup�re
    * en appelant la fonction GestInputReseau du script GestionnaireInputs. Les valeurs seront m�moris�es
    * et nous pourrons les utilis�es pour le d�placement du joueur dans un autre script. Ouf...*/
    public void OnInput(NetworkRunner runner, NetworkInput input) {
        //1.
        if (gestionnaireInputs == null && JoueurReseau.Local != null) {

            gestionnaireInputs = JoueurReseau.Local.GetComponent<GestionnaireInputs>();
        }

        //2.
        if (gestionnaireInputs != null) {
            input.Set(gestionnaireInputs.GetInputReseau());
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        print("playerjoin");
        if (_runner.IsServer) {
            Debug.Log("Un joueur s'est connect� comme serveur. Spawn d'un joueur");
            _runner.Spawn(joueurPrefab, Utilitaires.GetPositionSpawnAleatoire(), Quaternion.identity, player);
        } else {
            Debug.Log("Un joueur s'est connect� comme client. Spawn d'un joueur");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {

    }

    public void OnSceneLoadDone(NetworkRunner runner) {

    }

    public void OnSceneLoadStart(NetworkRunner runner) {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {

    }
}
