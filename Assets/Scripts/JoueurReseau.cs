using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // namespace pour utiliser les classes de Fusion
/* 
 * 1.Les objets réseau ne doivent pas dériver de MonoBehavior, mais bien de NetworkBehavior
 * Importation de l'interface IPlayerLeft
 * 2.Variable pour mémoriser l'instance du joueur
 * 3.Fonction Spawned() : Semblable au Start(), mais pour les objets réseaux
 * Sera exécuté lorsque le personnage sera créé (spawn)
* Test si le personnage créé est le personnage contrôlé par l'utilisateur local.
 * HasInputAuthority permet de vérifier cela.
 * Retourne true si on est sur le client qui a généré la création du joueur
 * Retourne false pour les autres clients
 * 4. Lorsqu'un joueur se déconnecte du réseau, on élimine (Despawn) son joueur.
 */
public class JoueurReseau : NetworkBehaviour, IPlayerLeft //1.
{
    public static JoueurReseau Local; //.2

    public override void Spawned() //3.
    {
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("Un joueur local a été créé");
        }
        else
        {
            Debug.Log("Un joueur réseau a été créé");
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
