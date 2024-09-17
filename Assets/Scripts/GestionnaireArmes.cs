using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // ne pas oublier ce namespace

/* Script qui g�re le tir du joueur qui d�rive de NetworkBehaviour
* Variables :
* - ilTir : variable r�seau [Networked] qui sera synchronis�e sur tous les clients
* - detecteurDeChangements : variable de type ChangeDetector (propre � Fusion) qui permet de r�cup�rer
les changements de variables r�seau.
* - tempsDernierTir : pour limiter la cadence de tir
* - delaiTirLocal : delai entre 2 tir (local)
* - delaiTirServeur:delai entre 2 tir (r�seau)
*
* - origineTir : point d'origine du rayon g�n�r� pour le tir (la cam�ra)
* - layersCollisionTir : layers � consid�rer pour la d�tection de collision.
*   En choisir deux dans l'inspecteur: Default et HitBoxReseau
* - distanceTir : la distance de port�e du tir
*
* - particulesTir : le syst�me de particules � activer � chaque tir. D�finir dans l'inspecteur en
* glissant le l'objet ParticulesTir qui est enfant du fusil
*/

public class GestionnaireArmes : NetworkBehaviour {
    [Networked] public bool ilTir { get; set; } // variable r�seau peuvent seulement �tre chang�e par le serveur (stateAuthority)
    ChangeDetector detecteurDeChangements;
    float tempsDernierTir = 0;
    float delaiTirLocal = 0.15f;
    float delaiTirServeur = 0.1f;

    // pour le raycast
    public Transform origineTir; // d�finir dans Unity avec la cam�ra
    public LayerMask layersCollisionTir; // d�finir dans Unity
    public float distanceTir = 100f;

    public ParticleSystem particulesTir;
    JoueurReseau joueurReseau; // r�f�rence au script JoueurReseau

    /*
     * On garde en m�moire le component (script) JoueurReseau pour pouvoir
     * communiquer avec lui.
     */
    void Awake() {
        joueurReseau = GetComponent<JoueurReseau>();
    }

    /*
     * On d�finit la variable detecteurDeChangements. On utilise une commande propre a Fusion qui nous permettra
     de v�rifier les changements des variables r�seau.
     */
    public override void Spawned() {
        detecteurDeChangements = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    /*
     * Fonction qui d�tecte le tir et d�clenche tout le processus
     * On r�cup�re les donn�es enregistr�es dans la structure de donn�es donneesInputReseau et on
     * v�rifie la variable appuieBoutonTir. Si elle est � true, on active la fonction TirLocal en passant
     * comme param�tre le vector indiquant le devant du personnage.
     */
    public override void FixedUpdateNetwork() {

        if (GetInput(out DonneesInputReseau donneesInputReseau)) {
            if (donneesInputReseau.appuieBoutonTir) {
                TirLocal(donneesInputReseau.vecteurDevant);
            }
        }
    }

    /* Gestion local du tir (sur le client seulement)
    * 1.On sort de la fonction si le tir ne respecte pas le d�lais entre 2 tir.
    * 2.Appel de la coroutine qui activera les particules et lancera le Tir pour le r�seau (autres clients)
    * 3.Raycast r�seau propre � Fusion avec une compensation de d�lai.
    * Param�tres:
    *   - origineTir.position (vector3) : position d'origine du rayon;
    *   - vecteurDevant (vector3) : direction du rayon;
    *   - distanceTir (float) : longueur du rayon
    *   - Object.InputAuthority : Indique au serveur le joueur � l'origine du tir
    *   - out var infosCollisions : variable pour r�cup�rer les informations si le rayon touche un objet
    *   - layersCollisionTir : indique les layers sensibles au rayon. Seuls les objets sur ces layers seront consid�r�s.
    *   - HitOptions.IncludePhysX : pr�cise quels type de collider sont sensibles au rayon.IncludePhysX permet
    *   de d�tecter les colliders normaux en plus des collider fusion de type Hitbox.
    * 4.V�rification du type d'objet touch� par le rayon.
    * - Si c'est un hitbox (objet r�seau), on change la variable toucheAutreJoueur
    * - Si c'est un collider normal, on affiche un message dans la console
    * 5.M�morisation du temps du tir. Servira pour emp�cher des tirs trop rapides.

    */
    void TirLocal(Vector3 vecteurDevant) {
        //1.
        if (Time.time - tempsDernierTir < delaiTirLocal) return;

        //2.
        StartCoroutine(EffetTirCoroutine());

        //3.
        Runner.LagCompensation.Raycast(origineTir.position, vecteurDevant, distanceTir, Object.InputAuthority, out var infosCollisions, layersCollisionTir, HitOptions.IgnoreInputAuthority);

        //4.
        if (infosCollisions.Hitbox != null) {
            // si nous sommes sur le code ex�cut� sur le serveur :
            // On appelle la fonction PersoEstTouche du joueur touch� dans le script GestionnairePointsDeVie
            if (Object.HasStateAuthority) {
                infosCollisions.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>().PersoEstTouche(joueurReseau, 1);
            }
        }
        //5.
        tempsDernierTir = Time.time;
    }

    /* Coroutine qui d�clenche le syst�me de particules localement et qui g�re la variable bool ilTir en l'activant
     * d'abord (true) puis en la d�sactivant apr�s un d�lai d�finit dans la variable delaiTirServeur.
     */
    IEnumerator EffetTirCoroutine() {
        ilTir = true; // comme la variable networked est chang�, la fonction OnTir sera appel�e (voir fonction Render plus bas)
        if (Object.HasInputAuthority) {
            if (!Runner.IsResimulation) particulesTir.Play(); // pour que les particules soient activ�es une seule fois
        }
        yield return new WaitForSeconds(delaiTirServeur);

        ilTir = false;
    }

    /* Fonction Render (semblable au update) dans laquelle on utilise la variable de type ChangeDetector
    "detecteurDeChangements" pour g�rer le tir d'un joueur sur les autres clients
    - Le foreach permet de r�cup�rer tout les changements qu'il y a eu dans les variables networked. La d�tection de changement
    permet de r�cup�rer la nouvelle valeur de la variable, mais aussi la valeur qu'elle avait auparavant.
    -� l'aide d'un switch, on v�rifie si le changement concerne la variable ilTir. Si c'est le cas, on r�cup�re
    la nouvelle et l'ancienne valeur de cette variable de type bool.
    - On d�clenche la fonction OnTir en passant la nouvelle et l'ancienne valeur de la variable ilTir.
        */
    public override void Render() {
        foreach (var change in detecteurDeChangements.DetectChanges(this, out var previousBuffer, out var currentBuffer)) {
            switch (change) {
                case nameof(ilTir):
                    var boolReader = GetPropertyReader<bool>(nameof(ilTir));
                    var (previousBool, currentBool) = boolReader.Read(previousBuffer, currentBuffer);
                    OnTir(previousBool, currentBool);
                    break;
            }
        }
    }

    /* Fonction appel�e par le serveur lorsque la variable ilTir est modifi�e
     * On appelle la fonction TirDistant() seulement si la variable ilTire actuelle est = true alors qu'elle �tait = false juste
     avant. Cela permet d'�viter que le joueur tir plus d'une fois.
     */
    void OnTir(bool ilTirValeurAncienne, bool ilTirValeurActuelle) {
        if (ilTirValeurActuelle && !ilTirValeurAncienne) // pour tirer seulement une fois
            TirDistant();
    }

    /* Fonction qui permet d'activer le syst�me de particule pour le personnage qui a tir�
    * sur tous les client connect�s. Sur l'ordinateur du joueur qui a tir�, l'activation du syst�me
    * de particules � d�j� �t� faite dans la fonction TirLocal(). Il faut cependant s'assurer que ce joueur
    * tirera aussi sur l'ordinateur des autres joueurs.
    * On d�clenche ainsi le syst�me de particules seulement si le client ne poss�de pas le InputAuthority
    * sur le joueur.
    *
    */
    void TirDistant() {
        //seulement pour les objets distants (par pour le joueur local car c'est d�j� fait)
        if (!Object.HasInputAuthority) {
            particulesTir.Play();
        }
    }
}