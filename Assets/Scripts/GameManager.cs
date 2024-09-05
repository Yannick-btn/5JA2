using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
public class GameManager : MonoBehaviour {
    public static GameManager instance; // R�f�rence � l'instance du GameManager
    public static bool partieEnCours = true;  // Est que la partie est en cours (static)
    [SerializeField] GestionnaireReseau gestionnaireReseau; // Ref�rence au gestionnaire r�seau
    public static string nomJoueurLocal; // Le nom du joueur local
    public static Dictionary<JoueurReseau, int> joueursPointagesData = new Dictionary<JoueurReseau, int>();
    //Dictionnaire pour m�moriser chaque JoueurReseau et son pointage. Au moment de la cr�ation d'un joueur (fonction Spawned() du joueur)
    // il ajoutera lui m�me sa r�f�rence au dictionnaire du GameManager.

    [Header("�l�ments UI")]
    public GameObject refCanvasDepart; // R�f�rence au canvas de d�part
    public GameObject refCanvasJeu; // R�f�rence au canvas de jeu
    public TextMeshProUGUI refTxtNomJoueur; // R�f�rence � la zone texte contenant le nom du joueur (dans CanvasDepart)
    public TextMeshProUGUI refTxtPointage; // R�f�rence � la zone d'affichage de tous les pointages (dans CanvasJeu)


    // Au d�part, on d�finit la variable "instance" qui permettra au autre script de communiquer avec l'instance du GameManager.
    void Awake() {
        instance = this;
    }

    /* Affichage du pointage des diff�rents joueurs connect�s � la partie.
   1. Si la partie est en cours...
   2. Cr�ation d'une variable locale de type string "lesPointages"
   3. Boucle qui passera tous les �l�ments du dictionnaire contenant la r�f�rence � chaque joueur et � son pointage.
   On va chercher le nom du joueur ainsi que son pointage et on l'ajoute � la variable locale "lesPointages". � la fin
   la chaine de caract�re contientra tous les noms et tous les pointages.
   4. Affichage des noms et des pointages (var lesPointages ) dans la zone de texte situ�e en haut de l'�cran.
   */
    void Update() {
        if (partieEnCours) {
            string lesPointages = "";
            foreach (JoueurReseau joueurReseau in joueursPointagesData.Keys) {
                lesPointages += $"{joueurReseau.monNom} : {joueurReseau.nbBoulesRouges}   ";
            }
            refTxtPointage.text = lesPointages;
        }
    }

    /* Fonction appel�e par le bouton pour commencer une partie
    1. R�cup�ration du nom du joueur (string)
    2. Appel de la fonction CreationPartie pour �tablir la connexion au serveur (dans script gestionnaireReseau)
    3. D�sactivation du canvas de d�part et activation du canvas de jeu
    */
    public void OnRejoindrePartie() {
        //.1
        nomJoueurLocal = refTxtNomJoueur.text;
        //.2
        gestionnaireReseau.CreationPartie(GameMode.AutoHostOrClient);
        //.3
        refCanvasDepart.SetActive(false);
        refCanvasJeu.SetActive(true);
    }
}
