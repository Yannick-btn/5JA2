using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

/* Script qui g�re les dommages et autre effets quand un joueur est touch�
* Variables :
*
* ##### Pour la gestion des points de vie ################################################
* - ptsVie : variable Networked de type byte (moins lourd qu'un int) pour les points de vie du joueur.
* - estMort : variable bool pour savoir si le joueur est mort ou pas.
* - detecteurDeChangements : variable de type ChangeDetector propre � Fusion. Permet de r�cup�rer les changements de variables networked
* - estInitialise : pour savoir si le joueur est initialis�.
* - ptsVieDepart : le nombre de points de vie au commencement ou apr�s un respawn
*
* ##### Pour les effets de changement de couleur quand le perso est touch� ###############
* - uiCouleurTouche:la couleur de l'image quand le perso est touch�
* - uiImageTouche : l'image qui s'affiche quand le perso est touch�
* - persoRenderer : r�f�rence au meshrenderer du perso. Servira � changer la couleur du mat�riel
* - couleurNormalPerso : la couleur normal du perso
*
* ##### Pour g�rer la mort du perso ###############
* - modelJoueur : r�f�rence au gameObject avec la partie visuelle du perso
* - particulesMort_Prefab : r�f�rence au Prefab des particules de mort � instancier � la mort du perso
* - particulesMateriel : r�f�rence au mat�riel utilis� par les particules de morts
* - hitboxRoot : r�f�rence au component photon HitBoxRoot servant � la d�tection de collision
* - gestionnaireMouvementPersonnage : r�f�rence au script gestionnaireMouvementPersonnage sur le perso
* - joueurReseau : r�f�rence au script joueurReseau sur le perso
*/

public class GestionnairePointsDeVie : NetworkBehaviour {
    [Networked] byte ptsVie { get; set; } //(byte : valeur possible entre 0 et 255, aucune valeur n�gative)
    [Networked] public bool estMort { get; set; }
    ChangeDetector detecteurDeChangements;
    bool estInitialise = false;
    const byte ptsVieDepart = 5;
    public Color uiCouleurTouche; //� d�finir dans l'inspecteur
    public Image uiImageTouche;//� d�finir dans l'inspecteur
    public MeshRenderer persoRenderer;//� d�finir dans l'inspecteur
    Color couleurNormalPerso;
    public GameObject modelJoueur;//� d�finir dans l'inspecteur
    public GameObject particulesMort_Prefab;//� d�finir dans l'inspecteur
    public Material particulesMateriel;//� d�finir dans l'inspecteur
    HitboxRoot hitboxRoot;
    GestionnaireMouvementPersonnage gestionnaireMouvementPersonnage;
    JoueurReseau joueurReseau;

    /*
     * On garde en m�moire la r�f�rence au component HitBoxRoot ainsi que les r�f�rences � deux
     * components (scripts) sur le perso : GestionnaireMouvementPersonnage et JoueurReseau
     */
    private void Awake() {
        hitboxRoot = GetComponent<HitboxRoot>();
        gestionnaireMouvementPersonnage = GetComponent<GestionnaireMouvementPersonnage>();
        joueurReseau = GetComponent<JoueurReseau>();
    }

    /*On d�finit la variable detecteurDeChangements. On utilise une commande propre a Fusion qui nous permettra
    de v�rifier les changements des variables r�seau.
    */
    public override void Spawned() {
        detecteurDeChangements = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    /*
     * Initialisation des variables � l'apparition du personnage. On garde aussi en m�moire la couleur
     * du personnage.
     */
    void Start() {
        ptsVie = ptsVieDepart;
        estMort = false;
        estInitialise = true;
        couleurNormalPerso = persoRenderer.material.color;
    }

    /* Fonction publique appel�e uniquement par le serveur dans le script GestionnairesArmes du joueur qui
     * a tir�.
     * 1. On quitte la fonction imm�diatement si le joueur touch� est d�j� mort
     * 2. Soustraction d'un point de vie.
     * 3. Si le joueur touch� avait des points (boules rouges ramass�es) :
        - On soustrait un point � son total;
     * 4. Si les points de vie sont � 0 (ou moins), la variable estMort est mise � true et on appelle
     * la coroutine RessurectionServeur_CO qui g�rera un �ventuel respawn du joueur
     * Important : souvenez-vous que les variables ptsVie et estMort sont de type [Networked] et qu'une
     * fonction sera automatiquement appel�e lorsque leur valeur change (voir fonction Render() plus bas)
    */
    public void PersoEstTouche(JoueurReseau dommageFaitParQui, byte dommage) {
        //1.
        if (estMort)
            return;
        //2.
        if (dommage > ptsVie)
            dommage = ptsVie;

        ptsVie -= dommage;

        // Perte d'un point
        if (joueurReseau.nbBoulesRouges > 0) {
            joueurReseau.nbBoulesRouges--;
            GameManager.instance.AjoutBoulesRouges(1); //Cr�ation d'une nouvelle boule rouge
        }
        //4.
        if (ptsVie <= 0) {
            StartCoroutine(RessurectionServeur_CO());
            estMort = true;
            GameManager.instance.AjoutBoulesRouges(joueurReseau.nbBoulesRouges); //Cr�ation de nouvelles boules rouges
            joueurReseau.nbBoulesRouges = 0;
        }
    }

    /* Enumarator qui attend 2 secondes et qui appelle ensuite la fonction DemandeRespawn
     * du script gestionnaireMouvementPersonnage.
    */
    IEnumerator RessurectionServeur_CO() {
        yield return new WaitForSeconds(2);
        gestionnaireMouvementPersonnage.DemandeRespawn();
    }

    /* Fonction Render (semblable au update) dans laquelle on utilise la variable de type ChangeDetector
    "detecteurDeChangements" pour g�rer la modification aux variables ptsVie et estMort.
    - Le foreach permet de r�cup�rer tout les changements qu'il y a eu dans les variables networked. La d�tection de changement
    permet de r�cup�rer la nouvelle valeur de la variable, mais aussi la valeur qu'elle avait auparavant.
    -� l'aide d'un switch, on v�rifie si le changement concerne la variable ptsVie ou estMort. Dans les 2 cas, on r�cup�re
    la nouvelle et l'ancienne valeur de la variable et on appelle une fonction (OnPtsVieChange ou OnChangeEtat) en passant les
    deux valeurs de la variable en param�tres(la nouvelle et l'ancienne).
    */
    public override void Render() {
        foreach (var change in detecteurDeChangements.DetectChanges(this, out var previousBuffer, out var currentBuffer)) {
            switch (change) {
                case nameof(ptsVie):
                    var byteReader = GetPropertyReader<byte>(nameof(ptsVie));
                    var (previousByte, currentByte) = byteReader.Read(previousBuffer, currentBuffer);
                    OnPtsVieChange(previousByte, currentByte);
                    break;

                case nameof(estMort):
                    var boolReader = GetPropertyReader<bool>(nameof(estMort));
                    var (previousBool, currentBool) = boolReader.Read(previousBuffer, currentBuffer);
                    OnChangeEtat(previousBool, currentBool);
                    break;
            }
        }
    }

    /* Fonction appel�e par la fonction Render quand la variable ptsVie est modifi�es.
    - On quitte la fonction si le joueur n'est pas initialis� encore;
    - Si la nouvelle valeut de ptsVie est plus petite que l'ancienne valeur :
        - On appelle la coroutine EffetTouche_CO qui s'occupera des effets visuels lorsqu'un personnage est touch�.
     */
    void OnPtsVieChange(byte ancienPtsVie, byte nouveauPtsvie) {
        if (!estInitialise) return;

        if (nouveauPtsvie < ancienPtsVie) {
            StartCoroutine(EffetTouche_CO());
        }
    }

    /* Coroutine qui g�re les effets visuels lorsqu'un joueur est touch�.
     * 1. Changement de la couleur du joueur pour blanc
     * 2. Changement de la couleur de l'image servant � indiquer au joueur qu'il est touch�.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur touch�
     * 3. Apr�s un d�lai de 0.2 secondes, on remet la couleur normale au joueur touch�
     * 4. On change la couleur de l'image servant � indiquer au joueur qu'il est touch�. L'important dans
     *    cette commande est qu'on met la valeur alpha � 0 (compl�tement transparente) pour la faire dispara�tre.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur touch� et que le joueur
     *    touch� n'est pas mort.
    */
    IEnumerator EffetTouche_CO() {
        //.1
        persoRenderer.material.color = Color.white; // pour tous les clients
                                                    //2.
        if (Object.HasInputAuthority) // seulement pour le joueur qui controle et qui s'est fait touch�
            uiImageTouche.color = uiCouleurTouche;
        //3.
        yield return new WaitForSeconds(0.2f);
        persoRenderer.material.color = couleurNormalPerso;
        //4.
        if (Object.HasInputAuthority && !estMort)
            uiImageTouche.color = new Color(0, 0, 0, 0);
    }

    /* Fonction appel�e automatiquement lorsque que la variable [Networked] estMort est modifi�e (voir fonction Render)
     * Appel de la fonction Mort() seulement quand la valeur actuelle de la variable estMort est true
     * Appel de la fonction Resurection() quand la valeur actuelle de la variable estMort est false
     * et que l'ancienne valeur de la variable estMort est true. Donc, quand le joueur �tait mort et qu'on
     * change la variable estMort pour la mettre � false.
     */
    void OnChangeEtat(bool estMortAncien, bool estMortNouveau) {
        if (estMortNouveau) {
            Mort();
        } else if (!estMortNouveau && estMortAncien) {
            Resurrection();
        }
    }

    /* Fonction appel�e � la mort du personnage par la fonction OnChangeEtat
     * 1. D�sactivation du joueur et de son hitboxroot qui sert � la d�tection de collision
     * 2. Appelle de la fonction ActivationCharacterController(false) dans le scriptgestionnaireMouvementPersonnage
     * pour d�sactiver le CharacterConroller.
     * 3. Instanciation d'un syst�me de particules (particulesMort_Prefab) � la place du joueur. On modifie
     * la couleur du mat�riel des particules en lui donnant la couleur du joueur qui meurt. Les particules
     * sont d�truites apr�s un d�lai de 3 secondes.
     */
    private void Mort() {
        //1.
        modelJoueur.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;
        //2.
        gestionnaireMouvementPersonnage.ActivationCharacterController(false);
        //3.
        GameObject nouvelleParticule = Instantiate(particulesMort_Prefab, transform.position, Quaternion.identity);
        particulesMateriel.color = joueurReseau.maCouleur;
        Destroy(nouvelleParticule, 3);
    }

    /* Fonction appel�e apr�s la mort du personnage, lorsque la variable estMort est remise � false
     * 1. On change la couleur de l'image servant � indiquer au joueur qu'il est touch�. L'important dans
     *    cette commande est qu'on met la valeur alpha � 0 (compl�tement transparente) pour la faire dispara�tre.
     *    Cette commande est effectu�e seulement sur le client qui contr�le le joueur
     * 2. On active le hitboxroot pour r�activer la d�tection de collisions
     * 3. Appelle de la fonction ActivationCharacterController(true) dans le scriptgestionnaireMouvementPersonnage
     *    pour activer le CharacterConroller.
     * 4. Appel de la coroutine (JoueurVisible) qui r�activera le joueur
     */
    private void Resurrection() {
        //1.
        if (Object.HasInputAuthority)
            uiImageTouche.color = new Color(0, 0, 0, 0);
        //2.
        hitboxRoot.HitboxRootActive = true;
        //3.
        gestionnaireMouvementPersonnage.ActivationCharacterController(true);
        //4.
        StartCoroutine(JoueurVisible());
    }

    /* Coroutine qui r�active le joueur apr�s un d�lai de 0.1 seconde */
    IEnumerator JoueurVisible() {
        yield return new WaitForSeconds(0.1f);
        modelJoueur.gameObject.SetActive(true);
    }

    /* Fonction publique appel�e par le script GestionnaireMouvementPersonnage
     * R�initialise les points de vie
     * Change l'�tat (la variable) estMort pour false
     */
    public void Respawn() {
        ptsVie = ptsVieDepart;
        estMort = false;
    }
}