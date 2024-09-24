using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System;
public class GameManager : MonoBehaviour {
    public static GameManager instance; // R�f�rence � l'instance du GameManager
    public int objectifPoints = 2; // Nombre de point pour finir la partie
    public int nbBoulesRougesDepart = 20; // Nombre de boules rouges a spawner au d�but d'une partie.

    public static bool partieEnCours = true;  // Est que la partie est en cours (static)
    [SerializeField] GestionnaireReseau gestionnaireReseau; // Ref�rence au gestionnaire r�seau
    public static string nomJoueurLocal; // Le nom du joueur local
    public static Dictionary<JoueurReseau, int> joueursPointagesData = new Dictionary<JoueurReseau, int>();
    //Dictionnaire pour m�moriser chaque JoueurReseau et son pointage. Au moment de la cr�ation d'un joueur (fonction Spawned() du joueur)
    // il ajoutera lui m�me sa r�f�rence au dictionnaire du GameManager.


    public string nomDeLapartie; // Le nom de la partie entr�e par le joueur
    public int nombreDeJoueurMax; // Le nombre maximum de joueurs d�cid� par le joueur qui cr�e la partie

    public TextMeshProUGUI refTextNomPartieJoindre; //Texte entr� dans le champs pour rejoindre une partie
    public TextMeshProUGUI refTextNomPartieNouvelle; // Texte entr� dans le champs pour cr�er une nouvelle partie

    // Attention ici, bogue avec TextMesh Pro. Le type TextMeshProUGUI ne permet pas d'utiliser
    //la commande TryParse(). Il faut absolument utiliser le type TMP_InputField
    public TMP_InputField refTextNbJoueursNouvelle; // R�f�rence au nombre de joueurs maximum entr� par l'utilisateur
    public GameObject panelNom; // R�f�rence au panel qui demande le nom du joueur
    public GameObject panelChoix; // R�f�rence au panel qui permet au joueur de choisir de cr�er ou joindre une partie
    public GameObject panelConnexionRefusee; // R�f�rence au panel qui s'affiche si la connexion a une partie est refus�e

    // R�f�rence au Prefab GestionnaireReseau. Sera utilis� lorsqu'une connexion a une partie est refus�e parce que le nombre
    // de joueur max a �t� atteint. Dans ce cas, Fusion supprimer le GestionnaireReseau original. Il faudra donc en cr�er un autre...
    public GameObject gestionnaireReseauSource;


    [Header("�l�ments UI")]
    public GameObject refPanelGagnant; // R�f�rence au panel affichant le texte du gagnant.
    public TextMeshProUGUI refTxtGagnant; // R�f�rence � la zone de texte pour afficher le nom du gagnant.
    public GameObject refPanelAttente; // R�f�rence au panel affichant le d'attente entre deux partie.
    public GameObject refCanvasDepart; // R�f�rence au canvas de d�part
    public GameObject refCanvasJeu; // R�f�rence au canvas de jeu
    public TextMeshProUGUI refTxtNomJoueur; // R�f�rence � la zone texte contenant le nom du joueur (dans CanvasDepart)
    public TextMeshProUGUI refTxtPointage; // R�f�rence � la zone d'affichage de tous les pointages (dans CanvasJeu)
    public GameObject txtAttenteAutreJoueur; // Texte sous forme de bandeau rouge pour indiquer au joueur qu'il est en attente. � d�f�nir dans l'inspecteur de Unity.


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
    /* Fonction appel�e par les boutons pour joindre et cr�er une nouvelle partie. La param�tre re�u d�terminera si une
      nouvelle partie est cr�� (true) ou si on tente de rejoindre une partie en cours (false)
      1. R�cup�ration du nom du joueur (string)
      2. R�cup�ration du nom de la partie � cr�er ou � rejoindre
      3. R�cup�ration du nombre de joueurs maximum entr�. Pour convertir un string en int, on utilise la commande TryParse
      4. Appel de la fonction CreationPartie pour �tablir la connexion au serveur (dans script gestionnaireReseau)
      5. D�sactivation du canvas de d�part et activation du canvas de jeu
      */
    public void OnRejoindrePartie(bool nouvellePartie) {
        //.1
        nomJoueurLocal = refTxtNomJoueur.text;
        //.2
        if (nouvellePartie) {
            nomDeLapartie = refTextNomPartieNouvelle.text;
        } else {
            nomDeLapartie = refTextNomPartieJoindre.text;
        }
        //.3
        if (refTextNbJoueursNouvelle.text != "") {
            int leNombre;
            if (int.TryParse(refTextNbJoueursNouvelle.text, out leNombre)) {
                GameManager.instance.nombreDeJoueurMax = leNombre;
            }
        }
        //.4
        gestionnaireReseau.CreationPartie(GameMode.AutoHostOrClient);
        //.5
        refCanvasDepart.SetActive(false);
        refCanvasJeu.SetActive(true);
    }


    /* Fonction qui d�clenchera une nouvelle partie si toutes les conditions sont r�unis.
   1. D�sactivation des panneaux de fin de partie
   2. On met la variable partieEnCours � true;
   3. Variable unSeulJoueur : pour g�rer le cas o� un seul joueur serait rest� connect�.
   4. Appel de la fonction Recommence pour chaque JoueurReseau. Si le joueur est seul, cette fonction
   renverra true, sinon false;
   5. S'il y a plus d'un joueur, on appelle la fonction NouvellesBoulesRouges pour spawner des boules
   */
    public void DebutNouvellePartie() {
        //.1
        refPanelAttente.SetActive(false);
        refPanelGagnant.SetActive(false);
        //2.
        partieEnCours = true;
        //3.
        bool unSeulJoueur = false;
        //4.
        foreach (JoueurReseau leJoueur in joueursPointagesData.Keys) {
            unSeulJoueur = leJoueur.Recommence();
        }
        if (!unSeulJoueur) NouvellesBoulesRouges();
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
        gestionnaireReseau.spheresDejaSpawn = false;
    }

    /* Fonction permettant d'afficher ou de masquer le texte d'attente d'un autre joueur (bandeau rouge) */
    public void AfficheAttenteAutreJoueur(bool etat) {
        txtAttenteAutreJoueur.SetActive(etat);
    }

    /* Fonction appel�e lors qu'il est temps d'instancier de nouvelles boules rouges en d�but de partie
   On appelle simplement une autre fonction CreationBoulleRouge dans le script gestionnaireReseau.
   */
    public void NouvellesBoulesRouges() {
        gestionnaireReseau.CreationBoulleRouge();
    }

    /* Fonction du GameManager qui recoit le nombre de boules rouges � cr�er. Appelle ensuite
   une fonction du m�me nom dans le GestionnaireR�seau.
   */
    public void AjoutBoulesRouges(int combien) {
        gestionnaireReseau.AjoutBoulesRouges(combien);
    }

    /* Fonction appel�e par le bouton "clic ici pour commencer" lorsque le joueur entre son nom. Dans ce cas,
   elle redevra "false" comme param�tre.
   Cette fonction est aussi appel�e lorsqu'une connexion � une partie est refus�e (max de joueurs atteints). Elle recevra
   alors true comme param�tre
   - Activation du panneau du choix de partie (rejoindre ou cr�er nouvelle)
   - D�sactivation du panneau de saisie de nom du joueur
   - Activation du canvas de d�part et d�sactivation du canvas de jeu
   - Dans le cas ou cette fonction est appel� suite � un refus de connexion, on cr�er un nouvel objet gestionnaire de r�seau et on
   m�morise sa r�f�rence. On affiche �galement le pannel qui indique au joueur la raison du refus de connexion.
   */
    public void NavigationPanel(bool nouveauGestionnaireReseau) {
        panelChoix.SetActive(true);
        panelNom.SetActive(false);
        refCanvasDepart.SetActive(true);
        refCanvasJeu.SetActive(false);

        if (nouveauGestionnaireReseau) {
            GameObject nouveauGestionnaire = Instantiate(gestionnaireReseauSource);
            gestionnaireReseau = nouveauGestionnaire.GetComponent<GestionnaireReseau>();
            panelConnexionRefusee.SetActive(true);
        }
    }
}
