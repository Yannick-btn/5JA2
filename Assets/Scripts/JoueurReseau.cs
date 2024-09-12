using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion; // namespace pour utiliser les classes de Fusion
/* 
 * 1.Les objets r�seau ne doivent pas d�river de MonoBehavior, mais bien de NetworkBehavior
 * Importation de l'interface IPlayerLeft
 * 2.Variable pour m�moriser l'instance du joueur
 * 3.Fonction Spawned() : Semblable au Start(), mais pour les objets r�seaux
 * Sera ex�cut� lorsque le personnage sera cr�� (spawn)
* Test si le personnage cr�� est le personnage contr�l� par l'utilisateur local.
 * HasInputAuthority permet de v�rifier cela.
 * Retourne true si on est sur le client qui a g�n�r� la cr�ation du joueur
 * Retourne false pour les autres clients
 * 4. Lorsqu'un joueur se d�connecte du r�seau, on �limine (Despawn) son joueur.
 */

//Ajout d'une variable public Transform. Dans Unity, glisser l'objet "visuel" du prefab du joueur

public class JoueurReseau : NetworkBehaviour, IPlayerLeft //1.
{
    //Variable qui sera automatiquement synchronis�e par le serveur sur tous les clients
    [Networked] public Color maCouleur { get; set; }

    // Variable est mise � true lorsque tous les joueurs sont pr�ts � reprendre une nouvelle partie
    // Il s'agit d'une variable synchronis�e sur toues les clients. Lorsqu'un changement est d�tect�
    // la fonctionne OnNouvellePartie() sera ex�cut�e.
    [Networked, OnChangedRender(nameof(OnNouvellePartie))] public bool recommence { get; set; }


    // Variable pour le pointage (nombre de boules rouge) du joueur qui sera automatiquement synchronis� par le serveur sur tous les clients
    // Lorsqu'un chanegement est d�tect�, la fonction OnChangementPointage sera automatiquement appel�e pour faire
    // une mise � jour de l'affichage du texte.
    [Networked, OnChangedRender(nameof(OnChangementPointage))] public int nbBoulesRouges { get; set; }

    //Variable r�seau (Networked) contenant le nom du joueur (sera synchronis�e)
    [Networked] public string monNom { get; set; }

    // Variable pour m�moriser la zone de texte au dessus de la t�te du joueur et qui afficher le pointage
    // Cette variable doit �tre d�finie dans l'inspecteur de Unity
    public TextMeshProUGUI affichagePointageJoueur;

    public static JoueurReseau Local; //.2

    //Ajout d'une variable public Transform. Dans Unity, glisser l'objet "visuel" du prefab du joueur
    public Transform modeleJoueur;

    /*
 * Au d�part, on change la couleur du joueur. La variable maCouleur sera d�finie
 * par le serveur dans le script GestionnaireReseau.La fonction Start() sera appel�e apr�s la fonction Spawned().
 */
    private void Start() {
        GetComponentInChildren<MeshRenderer>().material.color = maCouleur;
    }

    public override void Spawned() //3.
       {
        // � sa cr�ation, le joueur ajoute sa r�f�rence (son script JoueurReseau) et son pointage (var nbBoulesRouges) au dictionnaire
        // du GameManager.
        GameManager.joueursPointagesData.Add(this, nbBoulesRouges);

        if (Object.HasInputAuthority) {
            Local = this;
            Debug.Log("Un joueur local a �t� cr��");

            /*� la cr�ation du joueur et s'il est le joueur local (HasInputAuthority), ont doit d�f�nir son nom en allant
            chercher la variable nomJoueurLocal du GameManager.
            Pour que le nom soit synchronis� sur tous les clients, appelle d'une fonction RPC (RemoteProcedureCall) qui permet
            de dire � tous les clients d'ex�cuter la fonction  "RPC_ChangementdeNom"
            */
            monNom = GameManager.nomJoueurLocal;
            RPC_ChangementdeNom(monNom);

            //Si c'est le joueur du client, on appel la fonction pour le rendre invisible
            Utilitaires.SetRenderLayerInChildren(modeleJoueur, LayerMask.NameToLayer("JoueurLocal"));

            //On d�sactive la mainCamera. Assurez-vous que la cam�ra de d�part poss�de bien le tag MainCamera
            Camera.main.gameObject.SetActive(false);
        } else {
            //Si le joueur cr�� est contr�l� par un autre joueur, on d�sactive le component cam�ra de cet objet
            Camera camLocale = GetComponentInChildren<Camera>();
            camLocale.enabled = false;

            // On d�sactive aussi le component AudioListener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Un joueur r�seau a �t� cr��");
        }
        // on affiche le nom du joueur cr�� et son pointage
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";
    }

    /* Fonction RPC (RemoteProcedureCall) d�clench� par un joueur local qui permet la mise � jour du nom du joueur
    sur tous les autres clients. La source (l'�metteur) est le joueur local (RpcSources.InputAuthority). La cible est tous les joueurs
    connect�s (RpcTargets.All). Le param�tre re�u contient le nom du joueur � d�f�nir.
    Pour bien comprendre : Mathieu se connecte au serveur en inscrivant son nom. Il envoir un message � tous les autres clients. Sur
    chaque client, le joueur contr�l� par Mathieu ex�cutera cette fonction ce qui permettra une mise � jour du nom.
    1. On d�finit la variable nomNom
    2. On affiche le nom et le poitage au dessus de la t�te du joueur.
    */
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ChangementdeNom(string leNom, RpcInfo infos = default) {
        //1.
        monNom = leNom;
        //2.
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";
    }



    public void PlayerLeft(PlayerRef player) //.4
    {
        if (player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    /* Fonction appel�e automatiquement lorsqu'un changement est d�tect� dans la variable nbBoulesRouges du joueur (variable Networked)
    Mise � jour du pointage du joueur qui sera �gal au nombre de boules rouges ramass�es
    */
    public void OnChangementPointage() {
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";

        // On v�rifie si le nombre de boules rouge == l'objectif de points � atteindre
        // Si oui, on appelle la fonction FinPartie en passant le nom du joueur gagnant.
        // Cette fonction sera appel�e dans le script du gagnant, sur tous les clients connect�s
        if (nbBoulesRouges >= GameManager.instance.objectifPoints) {
            GameManager.instance.FinPartie(monNom);
        }
    }

    /* Fonction appel�e par le GameManager lorsque tous les joueurs sont pr�ts et qu'il faut relancer
   une nouvelle partie.
  */
    public void Recommence() {
        recommence = true;
    }

    /* Fonction appel�e lorsque la variable r�seau recommence = true.
    1. Si c'est le joueur local (hasInputAuthority), on d�sactive les panneux de victoire et d'attente
    2. Si la variable recommence est bien �gale � true, on remet diff�rentes variales � leur valeur
    de base, c'est-�-dire celle qu'elles doivent avoir en d�but de partie, comme le nbBoulesRouges = 0;
   */
    public void OnNouvellePartie() {
        if (Object.HasInputAuthority) {
            GameManager.instance.refPanelAttente.SetActive(false);
            GameManager.instance.refPanelGagnant.SetActive(false);
        }
        if (recommence) {
            GetComponent<GestionnaireInputs>().pretARecommencer = false;
            nbBoulesRouges = 0;
            GameManager.partieEnCours = true;
            recommence = false;
        }
    }

}
