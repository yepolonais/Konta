# Roadmap métier de Konta

Cette roadmap organise la réalisation du besoin décrit dans [konta.md](./konta.md), qui est la source de vérité métier.

## Principe de réalisation

Le budget prévisionnel annuel est le cœur de Konta. L'import des opérations réelles est un socle déjà largement développé : les prochaines étapes doivent le réutiliser et le compléter seulement là où la comparaison prévu/réalisé l'exige. Elles ne consistent pas à reconstruire le parcours d'import.

Les phases ci-dessous sont une proposition de travail. Les décisions métier signalées comme ouvertes devront être confirmées avant de figer les règles concernées.

## Décision d'interface — budget comme écran d'accueil

- [x] Remplacer l'écran/onglet « Opérations » comme destination principale par le budget prévisionnel annuel.
- [x] Faire du budget le premier écran présenté à l'ouverture de l'application.
- [x] Ne pas supprimer le socle d'import ni les données des opérations : ils restent nécessaires au calcul du réalisé.
- [x] Rendre les opérations consultables depuis le suivi prévu/réalisé, notamment en ouvrant le détail des opérations derrière le montant réalisé d'un poste.
- [x] Garder l'import accessible comme parcours dédié, sans maintenir une liste d'opérations isolée comme écran d'accueil.

## Organisation fonctionnelle — Budget et Opérations réelles

L'application doit être organisée autour de deux espaces complémentaires, plutôt que d'un écran générique de planification :

### Budget

- Le budget prévisionnel annuel est l'écran d'accueil et le parcours principal.
- Les postes et montants prévisionnels, leur ajustement en cours d'année, les budgets, les échéances et les contributions aux objectifs d'épargne relèvent de cet espace.
- La comparaison mensuelle prévu/réalisé est présentée dans le contexte du budget, afin de montrer les écarts et d'aider à ajuster les mois restants.

### Opérations réelles

- L'import CSV, le classement des opérations, les règles de catégorisation et les synthèses existantes restent accessibles dans cet espace.
- Les opérations détaillées sont consultables depuis les montants réalisés de la comparaison budgétaire, sans faire de leur liste un écran d'accueil.
- L'alignement du solde de référence d'un compte reste dans cet espace, comme outil de rapprochement du compte.

### À redistribuer depuis « Planification »

- [ ] Intégrer les budgets mensuels et dérogations dans le parcours Budget, ou les faire évoluer en montants prévisionnels par poste et par mois.
- [ ] Intégrer les échéances prévisionnelles dans le budget, sans les confondre avec les opérations réelles.
- [ ] Intégrer les contributions aux objectifs d'épargne dans le budget et leur comparaison au réalisé.
- [ ] Déplacer l'alignement du solde de référence dans Opérations réelles.
- [ ] Conserver les écrans Synthèses et Règles, qui sont utiles dans l'organisation actuelle.
- [x] Permettre de supprimer définitivement un poste de revenus ou de dépenses saisi par erreur (remplace l'archivage).

## Décision d'interface — grille annuelle et suivi mensuel

L'espace Budget combine une vue d'ensemble pour planifier et un suivi détaillé pour piloter chaque mois :

- [x] Proposer une grille annuelle avec les postes en lignes et les mois en colonnes, ainsi qu'un total annuel par poste.
- [x] Permettre d'éditer les montants directement dans la grille, mois par mois.
- [x] Ajouter une facilité pour recopier un montant sur les mois suivants (bouton « Recopier » par poste).
- [x] Présenter clairement des sections distinctes pour les revenus, les dépenses et l'épargne planifiée.
- [x] Permettre de sélectionner un mois et d'ouvrir son suivi détaillé : prévisionnel, réalisé et écart par poste ; le réalisé de l'épargne est suivi via les catégories rattachées.
- [x] Depuis un montant réalisé, permettre d'accéder aux opérations qui le composent.
- [ ] Rendre les ajustements du budget futur accessibles depuis le suivi mensuel, sans modifier les prévisions passées.
- [x] Sur petit écran, présenter un mois à la fois plutôt que de réduire la grille annuelle au point de la rendre illisible, tout en gardant accès aux totaux annuels.

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

- [x] Définir les postes budgétaires configurables, chacun de type revenu ou dépense.
- [x] Définir les enveloppes d'épargne planifiées comme une affectation distincte des dépenses de consommation.
- [x] Définir le modèle de montants prévisionnels mensuels par poste, y compris les mois sans prévision et les montants explicitement nuls.
- [x] Permettre de créer, renommer et désactiver les postes sans perdre les données historiques.
- [x] Prévoir une saisie ou une édition pratique des montants sur une grille annuelle (postes en lignes, mois en colonnes).
- [x] Calculer et afficher les totaux mensuels et annuels des revenus, des dépenses de consommation, des contributions d'épargne planifiées et du disponible prévisionnel.
- [x] Tester les calculs, les changements d'année et la persistance après redémarrage.

**Résultat attendu :** le foyer peut bâtir et consulter son budget annuel, y compris ce qu'il prévoit de mettre de côté, sans devoir créer de fausses opérations réelles.

## Phase 2 — Comparer le prévu au réalisé

Relier les opérations importées au plan budgétaire et rendre les écarts visibles.

- [x] Décision : un poste budgétaire peut agréger plusieurs catégories réelles ; une catégorie ne peut alimenter qu'un poste actif par année. Le rapprochement se fait par catégorie, pas encore par sous-catégorie.
- [x] Afficher, pour les revenus et dépenses rapprochés, le prévisionnel, le réalisé et l'écart mensuel (réalisé − prévu).
- [x] Afficher séparément les totaux mensuels prévisionnels et réels des revenus et dépenses de consommation.
- [x] Comparer les contributions d'épargne prévues aux transferts réellement effectués (catégories rattachées, côté sortant du virement), sans les classer comme dépenses de consommation ni les compter deux fois.
- [ ] Compléter ces totaux avec l'épargne affectée et le disponible réel ; leur calcul reste à définir après le rapprochement des transferts.
- [x] Permettre d'ouvrir les opérations réelles qui composent un montant réalisé.
- [x] Signaler les opérations non classées ou non rapprochées au lieu de les omettre silencieusement.
- [ ] Vérifier tous les calculs avec des données connues, notamment les mois sans budget, sans opération et en dépassement.

**Résultat intermédiaire :** les revenus et dépenses de consommation catégorisés peuvent être comparés au plan mois par mois ; les opérations associées sont consultables et les opérations restantes sont signalées. L'épargne est comparée aux versements sortants des catégories rattachées ; les retraits depuis l'épargne et le disponible réel restent à définir.

## Phase 3 — Ajuster le budget en cours d'année

Permettre de réagir aux écarts sans effacer le plan passé.

- [ ] Modifier les montants prévus d'un ou plusieurs mois futurs sans altérer les mois déjà écoulés.
- [ ] Ajouter un extra ponctuel et l'afficher séparément des revenus réguliers.
- [ ] Réduire, suspendre ou remettre à zéro un poste variable pour les mois futurs.
- [ ] Réaffecter un montant entre postes et afficher clairement l'effet sur le résultat prévisionnel.
- [ ] Distinguer le plan initial, le plan révisé et le réalisé selon la règle d'historique retenue.
- [ ] Tester qu'une révision future ne réécrit pas les valeurs antérieures et que les totaux restent justes.

**Décision métier à confirmer avant cette phase :** comment représenter un « extra » (revenu exceptionnel, apport au compte ou autre ajustement), et quel niveau d'historique conserver pour les versions successives du budget.

## Phase 4 — Approfondir le suivi des enveloppes (évolution)

Les contributions planifiées et leur comparaison au réalisé font partie des phases 1 et 2. Cette phase complémentaire concerne un suivi plus détaillé des objectifs ou des soldes détenus.

- [ ] Décider s'il faut suivre le solde réel de chaque enveloppe ou seulement les contributions prévues et réalisées.
- [ ] Si nécessaire, rapprocher les transferts, les soldes des comptes d'épargne et les dépenses finales associées aux objectifs.
- [ ] Tester les rapprochements sans compter deux fois un transfert puis une dépense issue de cette épargne.

Cette évolution ne doit pas retarder le budget annuel et la comparaison des contributions prévues avec les transferts réalisés.

## Phase 5 — Stabiliser les parcours métier

- [ ] Tester le parcours complet : création du budget annuel, import des opérations, comparaison mensuelle et ajustement des mois restants.
- [ ] Vérifier les libellés, formats de date et montants français, ainsi que les états vides et les erreurs.
- [ ] Vérifier que les opérations non classées, transferts et revenus exceptionnels sont présentés sans ambiguïté.
- [ ] Mettre à jour la documentation de démarrage et les limites métier connues.
- [ ] Confirmer que les critères essentiels de [konta.md](./konta.md) sont couverts par des tests automatisés ou des vérifications de parcours.

## Hors de cette roadmap métier

La PWA, l'installation mobile et le déploiement sur Debian/Freebox sont des chantiers techniques distincts. Ils ne doivent pas retarder la validation du budget annuel et de la comparaison prévu/réalisé.
