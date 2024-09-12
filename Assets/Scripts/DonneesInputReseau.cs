using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
/*
 * Ce script n'est pas une classe, mais bien une structure de donn�es (struct)
 * D�rive de INetworkInput qui est une interface de Fusion
 * Permet de m�moriser des valeurs avec des variables
 * mouvementInput : un vector2 qui servira au d�placement
 * vecteurDevant : un vecteur de direction repr�sentant le devant (l'axe des Z) du personnage dans le monde
 * saute : une bool�enne pour savoir le si personnage saute
 * Notez l'utilisation du type NetworkBool qui est une variable r�seau qui sera automatiquement synchronis�e
 * pour tous les clients
 */
public struct DonneesInputReseau : INetworkInput {
    public Vector2 mouvementInput;
    public Vector3 vecteurDevant;
    public NetworkBool saute;

    //Varialbe NetworkBool qui permet de savoir si un joueur est pr�t � recommencer. La variable
    // sera chang�e dans un autre script, quand le joueur appuiera sur la touche "R".
    public NetworkBool pretARejouer;
}
