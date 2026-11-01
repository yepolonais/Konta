# Roadmap métier de Konta

Cette roadmap organise la réalisation du besoin décrit dans [konta.md](./konta.md), qui est la source de vérité métier.

## Principe de réalisation

Le budget prévisionnel annuel est le cœur de Konta. L'import des opérations réelles est un socle déjà largement développé : les prochaines étapes doivent le réutiliser et le compléter seulement là où la comparaison prévu/réalisé l'exige. Elles ne consistent pas à reconstruire le parcours d'import.

Les phases ci-dessous sont une proposition de travail. Les décisions métier signalées comme ouvertes devront être confirmées avant de figer les règles concernées.

## Décision d'interface — budget comme écran d'accueil

- [X] Remplacer l'écran/onglet « Opérations » comme destination principale par le budget prévisionnel annuel.
- [X] Faire du budget le premier écran présenté à l'ouverture de l'application.
- [X] Ne pas supprimer le socle d'import ni les données des opérations : ils restent nécessaires au calcul du réalisé.
- [X] Rendre les opérations consultables depuis le suivi prévu/réalisé, notamment en ouvrant le détail des opérations derrière le montant réalisé d'un poste.
- [X] Garder l'import accessible comme parcours dédié, sans maintenir une liste d'opérations isolée comme écran d'accueil.

## Organisation fonctionnelle — Budget et Opérations réelles

L'application doit être organisée autour de deux espaces complémentaires, plutôt que d'un écran générique de planification :

### Budget

- Le budget prévisionnel annuel est l'écran d'accueil et le parcours principal.
- Les postes et montants prévisionnels, leur ajustement en cours d'année, et les contributions aux objectifs d'épargne relèvent de cet espace.
- La comparaison mensuelle prévu/réalisé est présentée dans le contexte du budget, afin de montrer les écarts et d'aider à ajuster les mois restants.

### Opérations réelles

- L'import CSV, le classement des opérations, les règles de catégorisation et les synthèses existantes restent accessibles dans cet espace.
- Les opérations détaillées sont consultables depuis les montants réalisés de la comparaison budgétaire, sans faire de leur liste un écran d'accueil.
- L'alignement du solde de référence d'un compte reste dans cet espace, comme outil de rapprochement du compte.

### Suppression de l'onglet « Planification »

- [X] Supprimer l'onglet « Planification » (budgets par catégorie, échéances, objectifs d'épargne) : le budget annuel couvre ces besoins.
- [X] Déplacer l'alignement du solde de référence dans l'import CSV (opération par défaut : la dernière du compte).
- [ ] Conserver les écrans Synthèses et Règles, qui sont utiles dans l'organisation actuelle.
- [X] Permettre de supprimer définitivement un poste de revenus ou de dépenses saisi par erreur (remplace l'archivage).

## Décision d'interface — grille annuelle et suivi mensuel

L'espace Budget combine une vue d'ensemble pour planifier et un suivi détaillé pour piloter chaque mois :

- [X] Proposer une grille annuelle avec les postes en lignes et les mois en colonnes, ainsi qu'un total annuel par poste.
- [X] Permettre d'éditer les montants directement dans la grille, mois par mois.
- [X] Ajouter une facilité pour recopier un montant sur les mois suivants (bouton « Recopier » par poste).
- [X] Présenter clairement des sections distinctes pour les revenus, les charges fixes, les dépenses variables et l'épargne planifiée ; fixes et variables restent des dépenses dans les calculs.
- [X] Réordonner les postes au sein de leur section par glisser-déposer ou au clavier, avec un ordre conservé.
- [X] Permettre de sélectionner un mois et d'ouvrir son suivi détaillé : prévisionnel, réalisé et écart par poste ; le réalisé de l'épargne est suivi via les catégories rattachées.
- [X] Depuis un montant réalisé, permettre d'accéder aux opérations qui le composent.
- [X] Sur petit écran, présenter un mois à la fois plutôt que de réduire la grille annuelle au point de la rendre illisible, tout en gardant accès aux totaux annuels.

## Socle existant — import et suivi des opérations

Déjà présent dans l'application : import CSV Société Générale avec prévisualisation et validation, comptes, détection des doublons, catégorisation et règles, synthèses par période, budgets mensuels, échéances et suivi de soldes.

Avant de s'appuyer dessus pour le budget annuel, vérifier que les parcours et données existants permettent bien :

- [ ] D'importer et consulter les opérations nécessaires au suivi du foyer sans reconstruire le flux.
- [ ] De rattacher les opérations aux postes utilisés par le budget, ou d'identifier les adaptations nécessaires.
- [ ] De corriger le classement d'une opération importée et de garder les totaux cohérents.
- [ ] De vérifier que les transferts entre comptes et les mouvements d'épargne ne gonflent pas artificiellement les revenus ou dépenses du foyer.
- [ ] De documenter les limites fonctionnelles actuelles de l'import et du classement.

## Phase 1 — Définir le budget annuel et ses enveloppes

Permettre de créer un plan pour une année, distinct des transactions bancaires.

- [X] Définir les postes budgétaires configurables, chacun de type revenu ou dépense.
- [X] Définir les enveloppes d'épargne planifiées comme une affectation distincte des dépenses de consommation.
- [X] Définir le modèle de montants prévisionnels mensuels par poste, y compris les mois sans prévision et les montants explicitement nuls.
- [X] Permettre de créer, renommer et désactiver les postes sans perdre les données historiques.
- [X] Prévoir une saisie ou une édition pratique des montants sur une grille annuelle (postes en lignes, mois en colonnes).
- [X] Calculer et afficher les totaux mensuels et annuels des revenus, des dépenses de consommation, des contributions d'épargne planifiées et du disponible prévisionnel.
- [X] Reporter automatiquement le disponible prévisionnel d'un mois sur le suivant ; en janvier, reprendre celui de décembre de l'année précédente. Le montant est calculé depuis le plan et n'est pas éditable.
- [X] Tester les calculs, les changements d'année et la persistance après redémarrage.

**Résultat attendu :** le foyer peut bâtir et consulter son budget annuel, y compris ce qu'il prévoit de mettre de côté, sans devoir créer de fausses opérations réelles.

## Phase 2 — Comparer le prévu au réalisé

Relier les opérations importées au plan budgétaire et rendre les écarts visibles.

- [X] Fonctionnement actuel : un poste budgétaire peut agréger plusieurs catégories réelles ; une catégorie ne peut alimenter qu'un poste actif par année. Les sous-catégories ont été supprimées : seules les catégories classent les opérations.
- [X] Afficher, pour les revenus et dépenses rapprochés, le prévisionnel, le réalisé et l'écart mensuel (réalisé − prévu).
- [X] Afficher séparément les totaux mensuels prévisionnels et réels des revenus et dépenses de consommation.
- [X] Comparer les contributions d'épargne prévues aux transferts réellement effectués (catégories rattachées, côté sortant du virement), sans les classer comme dépenses de consommation ni les compter deux fois.
- [X] Compléter ces totaux avec l'épargne et le disponible du mois : prévu = revenus − dépenses − épargne ; réel = revenus − dépenses de consommation − versements d'épargne (transferts exclus).
- [X] Permettre d'ouvrir les opérations réelles qui composent un montant réalisé.
- [X] Signaler les opérations non classées ou non rapprochées au lieu de les omettre silencieusement.
- [X] Vérifier tous les calculs avec des données connues, notamment les mois sans budget, sans opération et en dépassement (tests d'intégration).

**Résultat intermédiaire :** les revenus et dépenses de consommation catégorisés peuvent être comparés au plan mois par mois ; les opérations associées sont consultables et les opérations restantes sont signalées. L'épargne est comparée aux versements sortants des catégories rattachées ; les retraits depuis l'épargne et le disponible réel restent à définir.

## Phase 3 — Ajuster le budget en cours d'année

Permettre de réagir aux écarts sans effacer le plan passé.

- [X] Modifier les montants prévus d'un ou plusieurs mois futurs sans altérer les mois déjà écoulés (saisie, recopie vers l'avant).
- [X] Modifier ou remettre à zéro les montants prévus d'un poste variable pour les mois futurs, par édition directe des cellules.
- [X] Tester qu'une révision future ne réécrit pas les valeurs antérieures et que les totaux restent justes.

**Décisions retenues :** un extra est une ligne ponctuelle rattachée à un mois, affichée séparément des revenus ; le plan est simple (valeurs écrasées, pas de plan initial conservé pour l'instant, évolution possible plus tard).

## Phase 4 — Approfondir le suivi des enveloppes (évolution)

Les contributions planifiées et leur comparaison au réalisé font partie des phases 1 et 2. Cette phase complémentaire concerne un suivi plus détaillé des objectifs ou des soldes détenus.

- [ ] Associer chaque enveloppe d'épargne à son compte de destination, afin de savoir où sont versées les sommes prévues (par exemple : « Impôts » vers le LDD de Jen).
- [ ] Décider s'il faut suivre le solde réel de chaque enveloppe ou seulement les contributions prévues et réalisées.
- [ ] Si nécessaire, rapprocher les transferts, les soldes des comptes d'épargne et les dépenses finales associées aux objectifs.
- [ ] Tester les rapprochements sans compter deux fois un transfert puis une dépense issue de cette épargne.

Cette évolution ne doit pas retarder le budget annuel et la comparaison des contributions prévues avec les transferts réalisés.

## Phase 5 — Stabiliser les parcours métier

- [X] Tester le parcours complet : création du budget annuel, import des opérations, comparaison mensuelle et ajustement des mois restants (test d'intégration).
- [X] Vérifier les libellés, formats de date et montants français, ainsi que les états vides et les erreurs (états vides corrigés, mois invalide testé).
- [X] Vérifier que les opérations non classées, transferts et revenus exceptionnels sont présentés sans ambiguïté (note sur les virements, opérations non rapprochées listées, extras à part).
- [X] Mettre à jour la documentation de démarrage et les limites métier connues (README).
- [X] Confirmer que les critères essentiels de [konta.md](./konta.md) sont couverts par des tests automatisés ou des vérifications de parcours : critères 1 à 3 et 5 à 8 couverts ; le 4 l'est pour l'extra et la suspension, la réaffectation étant une évolution possible.
- [ ] Ajouter un cumul annuel prévu/réalisé sur les mois écoulés (konta.md §4.2).

## Évolutions possibles (non planifiées)

- [X] Séparer les catégories d'opérations des postes budgétaires et ajouter un onglet « Rapprochement » :
  - [X] Supprimer les sous-catégories (domaine, règles, import, API, frontend, migration) ; l'import propose « Mémoriser pour ce libellé » pour créer une règle de catégorie.
  - [X] Interdire la suppression d'une catégorie liée à des opérations (409 avec le nombre d'opérations) ; le renommage reste possible. La suppression d'une catégorie supprime ses règles.
  - [X] Conserver les catégories quand un poste budgétaire est supprimé : elles deviennent orphelines et doivent être réaffectées.
  - [X] Rendre les postes budgétaires et leurs rapprochements indépendants de l'année (les années sont portées par les montants mensuels ; la suppression d'un poste, confirmée dans l'interface, le retire pour toutes les années).
  - [X] Endpoint des catégories orphelines (utilisées par des opérations, sans poste rattaché), avec nombre d'opérations et montant.
  - [X] Onglet « Rapprochement » : postes avec leurs catégories rattachées (ajout/retrait) et zone « À rattacher » pour les catégories orphelines.
- [X] Catégoriser les opérations importées à partir d'une clé extraite du « Libellé complet » selon le type d'opération :
  - [X] Extraire la clé : commerçant (carte), `DE:` (virement reçu, prélèvement), `POUR:` nom + compte (virement émis), `ECHEANCE PRET N°…` (prêt), `COMMISSION D'INTERVENTION` (frais bancaires). Chèques et formats inconnus : aucune clé, catégorie manuelle.
  - [X] Règles génériques (motif → catégorie) avec type d'opération optionnel : comparaison normalisée (casse, accents, espaces), règle typée prioritaire, puis motif le plus long.
  - [X] Import : affichage du type et de la clé détectés ; « Mémoriser » crée une règle sur la clé et le type détectés.
  - [ ] Gérer les formats restants au fil des besoins (retrait DAB, `COTISATION MENSUELLE`) ; déjà ajoutés : `PRLV EUROP PONCTUEL`, remboursement carte `REMBT`, `INTERETS DEBITEURS`.
- [ ] Faire évoluer le « Suivi mensuel » en « Suivi », avec le choix d'une vue au mois ou à l'année pour consulter le prévu et le réalisé sur la période souhaitée.
- [ ] Lors de la préparation du budget de l'année suivante, afficher pour chaque poste la moyenne mensuelle réalisée sur l'année précédente, afin d'aider à ajuster le prévisionnel.
- [ ] Réaffecter un montant d'un poste à un autre pour un mois donné (formulaire « de… vers… montant… mois »), avec l'effet visible sur le résultat prévisionnel. Le disponible annuel reste inchangé. Une variante avec traçabilité et annulation, comme les extras, est possible.
- [ ] Distinguer le plan initial, le plan révisé et le réalisé en conservant le plan de départ, pour mesurer les dérives en fin d'année.
- [ ] Rendre les ajustements du budget futur accessibles depuis le suivi mensuel, sans modifier les prévisions passées.

## Hors de cette roadmap métier

La PWA, l'installation mobile et le déploiement sur Debian/Freebox sont des chantiers techniques distincts. Ils ne doivent pas retarder la validation du budget annuel et de la comparaison prévu/réalisé.
