using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // Ne pas oublier le namespace Fusion. Permet d'utiliser les commandes de Fusion

/*Script appliqu� sur le boules rouge et qui d�tecte les collisions avec les joueurReseau uniquement. Notez qu'on s'assure ici
que seul le serveur ex�cute le code lorsqu'il y a une collision. Dans la simulation locale, il ne se passera rien. Ceci permet d'�viter
des probl�mes, par exemple si deux joueurs touchent � une boule rouge presque en m�me temps. Dans ce cas, c'est le serveur qui tranchera
et d�terminera celui qui lui a touch� en premier.

1. Utilisation de la fonction OnTriggerEnter comme � l'habitude.
2. On v�rifie qu'on est sur le serveur et que l'objet touch� contient le component JoueurReseau(script). Si la condition est vraie,
la variable joueurReseau contiendra la r�f�rence au script JoueurReseau du joueur qui a touch� � la boule.
3. On augmente le pointage du joueur qui a touch� une boule
4. On Despawn l'objet touch� (la boule rouge). Seul le serveur ex�cute cette commande, mais l'objet disparaitra sur tous les clients.
*/
public class SphereCollision : NetworkBehaviour {
    private void OnTriggerEnter(Collider other) //1.
{
        if (Runner.IsServer && other.gameObject.TryGetComponent(out JoueurReseau joueurReseau)) //2.
        {
            joueurReseau.nbBoulesRouges++; //3.
            Runner.Despawn(Object);//4.
        }
    }
}
