using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        if (Object.HasInputAuthority) {
            Local = this;

            //Si c'est le joueur du client, on appel la fonction pour le rendre invisible
            Utilitaires.SetRenderLayerInChildren(modeleJoueur, LayerMask.NameToLayer("JoueurLocal"));

            //On d�sactive la mainCamera. Assurez-vous que la cam�ra de d�part poss�de bien le tag MainCamera
            Camera.main.gameObject.SetActive(false);

            Debug.Log("Un joueur local a �t� cr��");
        } else {
            //Si le joueur cr�� est contr�l� par un autre joueur, on d�sactive le component cam�ra de cet objet
            Camera camLocale = GetComponentInChildren<Camera>();
            camLocale.enabled = false;

            // On d�sactive aussi le component AudioListener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Un joueur r�seau a �t� cr��");
        }
    }

    public void PlayerLeft(PlayerRef player) //.4
    {
        if (player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
