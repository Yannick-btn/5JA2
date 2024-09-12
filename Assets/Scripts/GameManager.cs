using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
public class GameManager : MonoBehaviour {
    public static GameManager instance; // R�f�rence � l'instance du GameManager
    public int objectifPoints = 2; // Nombre de point pour finir la partie

    public static bool partieEnCours = true;  // Est que la partie est en cours (static)
    [SerializeField] GestionnaireReseau gestionnaireReseau; // Ref�rence au gestionnaire r�seau
    public static string nomJoueurLocal; // Le nom du joueur local
    public static Dictionary<JoueurReseau, int> joueursPointagesData = new Dictionary<JoueurReseau, int>();
    //Dictionnaire pour m�moriser chaque JoueurReseau et son pointage. Au moment de la cr�ation d'un joueur (fonction Spawned() du joueur)
    // il ajoutera lui m�me sa r�f�rence au dictionnaire du GameManager.

    // Liste static vide de type JoueurReseau qui servira � garder en m�moire tous les
    // joueurs connect�s. Sera utilis� entre 2 parties pour g�rer la reprise.
    public static List<JoueurReseau> lstJoueurReseau = new List<JoueurReseau>();

    [Header("�l�ments UI")]
    public GameObject refPanelGagnant; // R�f�rence au panel affichant le texte du gagnant.
    public TextMeshProUGUI refTxtGagnant; // R�f�rence � la zone de texte pour afficher le nom du gagnant.
    public GameObject refPanelAttente; // R�f�rence au panel affichant le d'attente entre deux partie.
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
        print("OnRejoindrePartie : nomJoueurLocal");
        //.2
        gestionnaireReseau.CreationPartie(GameMode.AutoHostOrClient);
        //.3
        refCanvasDepart.SetActive(false);
        refCanvasJeu.SetActive(true);
    }

    /* Fonction appel� par le script JoueurReseau du joueur gagnant (qui a ramass� 10 boules)
      1. Variable partieEnCours mise � false
      2. Activation du panel UI pour afficher le gagnant
      3. On met le nom du gagnant dans la zone de texte (re�u en param�tre)
      4. Construction d'une liste contenant la r�f�rence � tous les joueurs pr�sents. Sera
      utilis�e pour reprendre une partie en permettant d'attendre que tous les joueurs soient pr�ts
      */
    public void FinPartie(string nomGagnant) {
        partieEnCours = false;
        refPanelGagnant.SetActive(true);
        refTxtGagnant.text = nomGagnant;
        foreach (JoueurReseau leJoueur in joueursPointagesData.Keys) {
            lstJoueurReseau.Add(leJoueur);
        }
    }
    /* Fonction appel�e par le GestionnaireMouvementPersonnage qui v�rifie si la touche "R" a �t�
   enfonc�e pour reprendre une nouvelle partie. Cette fonction sera ex�cut� seulement sur le
   serveur.
   1. On retire de la liste lstJoueurReseau la r�f�rence au joueur qui est pr�t � reprendre.
   2. Si la liste lstJoueurReseau est rendu vide (== 0), c'est que tous les joueurs sont pr�t
   a reprendre. Si c'est le cas, on appelle la fonction Recommence pr�sente dans le script
   JoueurReseau. Tous les joueurs ex�cuteront cette fonction.
   */
    public void JoueurPretReprise(JoueurReseau joueurReseau) {
        lstJoueurReseau.Remove(joueurReseau);

        if (lstJoueurReseau.Count == 0) {
            foreach (JoueurReseau leJoueur in joueursPointagesData.Keys) {
                leJoueur.Recommence();
            }
        }
    }

}
