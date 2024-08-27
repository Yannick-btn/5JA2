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
public class JoueurReseau : NetworkBehaviour, IPlayerLeft //1.
{
    public static JoueurReseau Local; //.2

    public override void Spawned() //3.
    {
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("Un joueur local a �t� cr��");
        }
        else
        {
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
