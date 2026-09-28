# MVP - Application de suivi des finances familiales

**Version :** 1.0  
**Statut :** Spécification initiale destinée au développement avec VS Code Agent  
**Stack cible :** .NET 8, JavaScript natif, Web Components, SQLite

---

## 1. Objectif

Développer une application web de suivi des finances familiales destinée à deux utilisateurs (les deux membres du foyer), partageant les mêmes données et le même espace de travail.

L'application doit remplacer progressivement un fichier Excel utilisé pour suivre les dépenses par année, avec les catégories en lignes et les mois en colonnes.

Le MVP se concentre sur le compte bancaire commun. Il permet de suivre **les dépenses et les revenus**, de saisir des opérations manuellement, d'importer des relevés bancaires CSV de la Société Générale et de consulter les résultats par mois et par année.

L'application doit d'abord fonctionner localement sur un ordinateur. Son architecture devra rester portable afin d'envisager ultérieurement un déploiement sur une VM Debian (notamment celle de la Freebox). Un hébergement sur Vercel pourra être étudié séparément, en fonction des contraintes de l'API .NET et de la persistance des données.

## 2. Objectifs du MVP

- Remplacer le suivi Excel par une application structurée.
- Enregistrer les revenus et les dépenses du compte commun.
- Organiser les opérations par catégories et sous-catégories.
- Importer les opérations depuis un fichier CSV bancaire.
- Éviter les doublons lors d'importations successives.
- Catégoriser rapidement les opérations, avec des règles réutilisables.
- Définir des budgets mensuels récurrents, avec possibilité de dérogation pour un mois donné.
- Consulter les dépenses, revenus et budgets dans des vues mensuelles et annuelles.
- Permettre la saisie et la correction manuelles.
- Partager les mêmes données entre les deux membres du foyer, sans comptes utilisateurs distincts dans le MVP.

## 3. Hors périmètre du MVP

Ne pas développer pour la première version :

- Synchronisation bancaire automatique ou connexion à une API bancaire.
- Import PDF.
- Gestion des comptes d'épargne.
- Virements entre comptes.
- Gestion de plusieurs foyers ou de plusieurs comptes bancaires.
- Authentification multi-utilisateur et gestion de rôles.
- Notifications push, e-mail ou SMS.
- Prévisions financières avancées.
- Gestion des justificatifs et pièces jointes.
- Application mobile native.
- Import permanent de fichiers Excel.

L'import du fichier Excel existant est une **migration ponctuelle**. Il peut être réalisé par un script ou un outil temporaire, séparé de l'application et supprimé ou archivé après migration.

## 4. Utilisateurs et accès

Le MVP est destiné à deux personnes qui utilisent le même espace et partagent les mêmes opérations, catégories et budgets.

Pour la première version locale :

- Pas de création de compte ni de gestion des rôles.
- L'application est utilisée sur l'ordinateur où elle est lancée.
- Les données sont persistées dans une base SQLite locale.
- Ne pas exposer l'application sur le réseau sans ajouter au préalable une authentification et une configuration de sécurité adaptées.

L'architecture ne doit pas empêcher l'ajout ultérieur d'une authentification.

## 5. Périmètre fonctionnel

### 5.1 Opérations

Une opération représente un revenu ou une dépense du compte commun.

Champs fonctionnels :

- Identifiant.
- Date de l'opération.
- Libellé bancaire ou libellé saisi.
- Montant positif, stocké en valeur décimale.
- Type : `Expense` (dépense) ou `Income` (revenu).
- Catégorie, facultative pour permettre les opérations non catégorisées.
- Source : saisie manuelle ou import.
- Date de création et date de dernière modification.
- Référence externe éventuelle fournie par la banque.
- Identifiant d'import éventuel.

Règles :

- Le montant est toujours strictement positif ; le type détermine s'il s'agit d'un revenu ou d'une dépense.
- Une dépense réduit le solde de la période ; un revenu l'augmente.
- Une opération doit pouvoir être créée, consultée, modifiée et supprimée.
- La suppression doit demander une confirmation dans l'interface.
- La date détermine le mois et l'année de rattachement.
- Une opération peut rester sans catégorie.
- Les opérations importées et les opérations manuelles sont modifiables après enregistrement.

Le MVP ne gère qu'un seul compte, le compte commun. Le modèle peut néanmoins inclure un compte par défaut si cela simplifie une évolution future, sans introduire d'interface de gestion de comptes.

### 5.2 Catégories et sous-catégories

Les catégories sont personnalisables.

Une catégorie possède :

- Identifiant.
- Nom.
- Catégorie parente facultative.
- Type autorisé : dépense, revenu ou les deux.
- Ordre d'affichage.
- Indicateur actif/inactif.

Règles :

- Une catégorie peut être une catégorie racine ou une sous-catégorie.
- Limiter la hiérarchie à deux niveaux dans le MVP.
- Une opération est affectée à une seule catégorie ou sous-catégorie.
- Les totaux d'une catégorie parente incluent les opérations directement associées à celle-ci et celles de ses sous-catégories.
- Une catégorie utilisée ne doit pas être supprimée physiquement ; elle peut être désactivée.
- Une catégorie désactivée reste visible dans l'historique des opérations, mais n'est plus proposée pour les nouvelles opérations.
- Prévoir une catégorie système « Non catégorisé » dans les vues, sans obligation de créer une entité spéciale en base.

Exemples de catégories initiales (modifiables) :

- Logement : crédit immobilier, électricité, eau, internet, assurance habitation.
- Alimentation : supermarché, boulangerie, restaurants.
- Transport : carburant, entretien automobile, assurance automobile.
- Loisirs : sorties, vacances, abonnements.
- Santé.
- Revenus : salaires, remboursements, autres revenus.

Ne pas imposer ces exemples comme une taxonomie figée.

### 5.3 Budgets mensuels

Un budget est défini pour une catégorie de dépense et s'applique par défaut chaque mois.

Champs :

- Identifiant.
- Catégorie.
- Montant budgété.
- Date de début d'application.
- Date de fin facultative.
- Actif/inactif.

Règles :

- Le budget est mensuel et récurrent à partir de sa date de début.
- Il est possible de définir une valeur différente pour un mois précis, sans modifier les autres mois.
- La valeur ponctuelle est une dérogation au budget récurrent.
- Un seul budget effectif par catégorie et par mois.
- Le budget concerne les dépenses uniquement.
- Le disponible est calculé ainsi : `budget effectif - dépenses réalisées de la catégorie sur le mois`.
- Le disponible peut être négatif si le budget est dépassé.
- Afficher le montant dépensé, le montant disponible et le pourcentage consommé.
- Afficher un état visuel à l'approche du plafond (seuil initial configurable, valeur par défaut : 80 %) et en cas de dépassement.
- Pas de notification externe dans le MVP.

Si aucun budget n'est défini pour une catégorie, afficher « Aucun budget » plutôt qu'un budget nul.

### 5.4 Import CSV bancaire

L'import CSV est une fonctionnalité permanente du MVP. Le premier format pris en charge est celui des exports d'opérations de la Société Générale.

Le format exact devra être confirmé à partir d'un fichier CSV anonymisé fourni pendant le développement. Ne pas inventer les noms de colonnes ou supposer un séparateur, un encodage ou un format de date sans les vérifier.

Parcours utilisateur :

1. L'utilisateur sélectionne un fichier CSV.
2. L'application analyse le fichier sans enregistrer immédiatement les opérations.
3. L'interface affiche les lignes interprétées et les éventuelles erreurs.
4. L'application identifie les opérations potentiellement déjà importées.
5. Les catégories sont proposées à partir des règles connues.
6. L'utilisateur peut corriger les catégories et exclure certaines lignes.
7. L'utilisateur valide l'import.
8. Seules les lignes valides, non exclues et non déjà importées sont enregistrées.
9. L'application affiche un bilan : importées, doublons, exclues et en erreur.

Exigences :

- Supporter les montants avec virgule décimale et les signes utilisés dans le fichier SG.
- Gérer les encodages et séparateurs effectivement observés dans le fichier d'exemple.
- Gérer les lignes vides et les éventuelles lignes d'en-tête ou de solde.
- Ne jamais interpréter silencieusement une ligne invalide comme une opération valide.
- Présenter les erreurs de manière compréhensible.
- L'import doit être atomique au moment de la validation : en cas d'échec d'enregistrement, ne pas laisser un import partiel.
- Conserver un historique minimal des imports (nom du fichier, date, nombre d'opérations importées).
- Ne pas conserver le contenu brut du relevé au-delà de ce qui est nécessaire au traitement, sauf décision explicite ultérieure.

#### Détection des doublons

La détection doit fonctionner même si l'utilisateur importe des périodes qui se chevauchent.

Ordre de préférence :

1. Utiliser une référence stable fournie par la banque si elle existe et si son unicité est confirmée.
2. Sinon, calculer une empreinte à partir des données normalisées disponibles (date, libellé normalisé, montant et type).
3. Si plusieurs opérations identiques peuvent légitimement exister le même jour, ne pas les fusionner aveuglément : signaler les cas ambigus ou utiliser un mécanisme de rapprochement qui tient compte du nombre d'occurrences déjà enregistrées.

La détection doit être documentée et testée. L'utilisateur doit pouvoir examiner les doublons détectés avant validation.

#### Format interne commun

Le parseur bancaire doit convertir les lignes du CSV en un modèle indépendant du format bancaire, par exemple :

```csharp
public sealed record ImportedTransaction(
    DateOnly Date,
    string Label,
    decimal Amount,
    TransactionType Type,
    string? ExternalReference
);
```

Ce modèle pourra évoluer si le fichier réel contient des informations utiles supplémentaires.

### 5.5 Règles de catégorisation

L'application propose une catégorie en fonction du libellé d'une opération.

Champs d'une règle :

- Identifiant.
- Texte ou motif de correspondance.
- Catégorie cible.
- Priorité.
- Indicateur actif/inactif.

Comportement :

- La première version utilise une correspondance textuelle insensible à la casse, après normalisation des espaces.
- La règle doit être réutilisable lors des imports suivants.
- En cas de plusieurs règles correspondantes, appliquer la priorité la plus élevée ; si la priorité est identique, appliquer une règle de départage déterministe.
- Ne pas utiliser d'expressions régulières arbitraires dans le MVP.
- L'utilisateur peut modifier la catégorie proposée avant validation.
- Depuis une opération, l'utilisateur peut demander à créer ou mettre à jour une règle à partir de son libellé.
- Une règle ne doit pas écraser une catégorie choisie manuellement lors de la modification d'une opération existante.

### 5.6 Saisie manuelle

L'utilisateur peut créer une opération sans passer par l'import.

Champs requis :

- Date.
- Libellé.
- Montant.
- Type (dépense ou revenu).

La catégorie est facultative.

L'interface doit proposer une saisie rapide, avec validation des montants et des dates.

### 5.7 Tableau de bord mensuel

Le tableau de bord affiche le mois sélectionné, avec navigation vers les mois précédents et suivants.

Indicateurs :

- Total des revenus.
- Total des dépenses.
- Différence revenus moins dépenses.
- Dépenses par catégorie.
- Budgets et montants disponibles pour les catégories budgétées.
- Nombre d'opérations non catégorisées.

Les totaux doivent être calculés à partir des opérations enregistrées, et non stockés comme source de vérité.

### 5.8 Vue annuelle

Reproduire la logique du fichier Excel : catégories en lignes, mois en colonnes.

Fonctionnalités :

- Sélection de l'année.
- Une colonne par mois.
- Une ligne par catégorie, avec possibilité de développer les sous-catégories.
- Total mensuel.
- Total annuel par catégorie.
- Total annuel global des dépenses et des revenus, présentés séparément.
- Les cellules vides doivent être distinguées des valeurs nulles si cela améliore la lisibilité.
- Un clic sur un montant ouvre la liste des opérations qui le composent.
- Les catégories inactives restent consultables pour les années où elles ont été utilisées.

Pour le MVP, l'année courante est l'année proposée par défaut. Il doit néanmoins être possible de sélectionner une autre année pour consulter l'historique importé ou saisi.

### 5.9 Recherche et filtres

Dans la liste des opérations :

- Filtrer par période.
- Filtrer par catégorie.
- Filtrer par type (dépense/revenu).
- Rechercher dans le libellé.
- Trier par date et montant.
- Pagination ou chargement progressif si nécessaire.

## 6. Écrans

Navigation principale recommandée :

1. Tableau de bord
2. Vue annuelle
3. Opérations
4. Importer un relevé
5. Catégories
6. Budgets

L'interface doit être responsive, utilisable sur ordinateur et sur smartphone via navigateur. Le MVP n'est pas une application mobile native.

Priorités UX :

- Saisie d'une opération en peu d'étapes.
- Modification rapide des catégories dans le tableau d'import.
- Navigation simple entre les mois.
- Montants et dates affichés au format français (`fr-FR`).
- États explicites de chargement, de succès et d'erreur.
- Confirmation avant suppression.
- Accessibilité clavier pour les formulaires et tableaux.

## 7. Modèle de données proposé

Le modèle exact pourra être ajusté lors de l'implémentation, mais doit couvrir les concepts suivants.

### Transaction

- `Id` : identifiant.
- `Date` : date de l'opération (`DateOnly`).
- `Label` : libellé.
- `Amount` : montant positif (`decimal`).
- `Type` : `Expense` ou `Income`.
- `CategoryId` : nullable.
- `Source` : `Manual` ou `Import`.
- `ExternalReference` : nullable.
- `ImportId` : nullable.
- `CreatedAt` et `UpdatedAt`.

### Category

- `Id`
- `Name`
- `ParentCategoryId` nullable
- `AllowedTransactionType` (Expense, Income, Both)
- `SortOrder`
- `IsActive`

### Budget

- `Id`
- `CategoryId`
- `Amount` (`decimal`)
- `StartMonth` (date normalisée au premier jour du mois)
- `EndMonth` nullable
- `IsActive`

### BudgetOverride

- `Id`
- `CategoryId`
- `Month` (premier jour du mois)
- `Amount`

Une contrainte d'unicité doit empêcher plusieurs dérogations pour la même catégorie et le même mois.

### CategorizationRule

- `Id`
- `Pattern`
- `CategoryId`
- `Priority`
- `IsActive`

### ImportBatch

- `Id`
- `FileName`
- `ImportedAt`
- `TotalRows`
- `ImportedRows`
- `DuplicateRows`
- `ExcludedRows`
- `ErrorRows`

Les noms peuvent être adaptés aux conventions du projet. Utiliser des clés étrangères et des index pertinents. Ne pas ajouter de champs ou d'entités sans besoin fonctionnel identifié.

## 8. API proposée

Endpoints REST indicatifs :

### Transactions

- `GET /api/transactions?from=&to=&categoryId=&type=&search=&page=&pageSize=`
- `GET /api/transactions/{id}`
- `POST /api/transactions`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

### Categories

- `GET /api/categories`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}` ou désactivation via `PATCH`

### Budgets

- `GET /api/budgets?month=YYYY-MM`
- `PUT /api/budgets/{categoryId}` pour créer ou modifier le budget récurrent
- `PUT /api/budgets/{categoryId}/overrides/{month}` pour définir une dérogation
- `DELETE /api/budgets/{categoryId}/overrides/{month}` pour supprimer une dérogation

### Dashboard

- `GET /api/dashboard/monthly?month=YYYY-MM`
- `GET /api/dashboard/yearly?year=YYYY`

### Imports

- `POST /api/imports/preview` : recevoir et analyser le fichier, sans persister les transactions.
- `POST /api/imports/commit` : valider un aperçu et enregistrer les opérations.
- `GET /api/imports` : historique des imports.

L'implémentation doit choisir une stratégie fiable pour relier l'aperçu à la validation (par exemple un identifiant temporaire côté serveur avec expiration, ou un jeton signé contenant les données normalisées). Ne pas faire confiance aux données de prévisualisation renvoyées et modifiées côté client sans validation serveur.

Pour un premier MVP local, les endpoints peuvent être adaptés, mais conserver des contrats cohérents et documentés.

## 9. Architecture et technologies

### Backend

- ASP.NET Core Web API sur .NET 8.
- Entity Framework Core.
- SQLite.
- Validation des entrées côté serveur.
- DTO distincts des entités EF Core.
- Pas d'AutoMapper.
- Pas de couche Application imposée.
- Injection de dépendances via le conteneur natif.
- Gestion centralisée des erreurs avec réponses HTTP cohérentes.
- Configuration via `appsettings.json` et variables d'environnement.

### Frontend

- JavaScript natif avec modules ES.
- Web Components pour les composants réutilisables.
- Pas de framework SPA dans le MVP.
- `fetch` pour appeler l'API.
- Chart.js uniquement si les graphiques apportent une valeur réelle au MVP.
- Séparer les appels API, les composants et les vues.
- Éviter une architecture frontend surdimensionnée.

### Tests

- xUnit pour les tests unitaires.
- Tests des règles métier et de catégorisation.
- Tests du parseur CSV sur des fichiers d'exemple anonymisés.
- Tests de détection des doublons, y compris les opérations identiques légitimes.
- Tests d'intégration API avec une base SQLite temporaire ou en mémoire compatible.
- Vérifier les calculs mensuels et annuels, les budgets et les dérogations.

### Exécution locale

- Le projet doit pouvoir démarrer localement avec une procédure documentée.
- SQLite stocke les données dans un fichier persistant hors du répertoire temporaire.
- Prévoir les migrations EF Core.
- Fournir un jeu de données de démonstration facultatif, distinct des données réelles.
- Ne pas committer de base contenant des données personnelles.

## 10. Déploiement futur

Le MVP doit fonctionner localement sans dépendance à un service externe.

### VM Debian / Freebox

Option à étudier après le MVP :

- Conteneur Docker pour l'API et le frontend, ou serveur web séparé.
- Volume persistant pour la base SQLite.
- Sauvegarde régulière de la base.
- HTTPS et authentification avant tout accès depuis l'extérieur.
- Configuration des secrets via variables d'environnement.

## 11. Sécurité et confidentialité

Même pour une application locale :

- Ne pas journaliser les contenus complets des relevés ou des opérations sensibles.
- Limiter la taille des fichiers importés.
- Valider le type et le contenu des fichiers côté serveur.
- Ne pas exécuter de contenu issu des fichiers.
- Protéger les endpoints contre les entrées invalides.
- Prévoir une sauvegarde et une restauration de la base.

Avant toute exposition sur le réseau local ou Internet, ajouter une authentification, une protection contre les accès non autorisés et HTTPS. Le fait que deux personnes partagent le même espace ne signifie pas que l'application doit être publiquement accessible sans protection.

## 12. Critères d'acceptation du MVP

Le MVP est considéré comme utilisable lorsque :

1. L'application démarre localement à partir des instructions du dépôt.
2. Les catégories peuvent être créées, modifiées, ordonnées et désactivées.
3. Une dépense et un revenu peuvent être saisis manuellement.
4. Une opération peut être modifiée et supprimée.
5. Un CSV SG conforme au format d'exemple peut être prévisualisé.
6. Les lignes invalides sont signalées et ne sont pas importées silencieusement.
7. Les doublons sont identifiés et exclus de l'import, avec possibilité de vérifier les cas ambigus.
8. L'utilisateur peut corriger les catégories avant validation.
9. Une règle de catégorisation enregistrée est proposée lors d'un import ultérieur.
10. La validation d'un import est atomique.
11. Le tableau de bord mensuel affiche correctement revenus, dépenses et différence.
12. La vue annuelle affiche les catégories en lignes et les mois en colonnes.
13. Un montant de la vue annuelle permet de consulter les opérations correspondantes.
14. Les budgets récurrents et leurs dérogations mensuelles sont correctement calculés.
15. Le montant disponible peut être négatif en cas de dépassement.
16. Les données persistent après redémarrage de l'application.
17. Les tests automatisés couvrent les règles métier principales.

## 13. Ordre de réalisation recommandé pour l'agent VS Code

Développer par petites étapes, en gardant le dépôt compilable et testable à chaque étape.

### Phase 0 - Cadrage technique

- Inspecter le dépôt existant avant toute modification.
- Si le dépôt est vide, créer la solution et les projets.
- Ajouter un README avec les commandes de démarrage.
- Confirmer les versions SDK disponibles.
- Ne pas ajouter de fonctionnalités hors périmètre.

### Phase 1 - Modèle et persistance

- Créer les entités, enums, DbContext et migrations.
- Ajouter les catégories initiales uniquement si elles sont clairement identifiées comme données de démonstration ou de démarrage.
- Ajouter les tests du modèle et des contraintes.

### Phase 2 - CRUD des opérations et catégories

- Implémenter les endpoints.
- Ajouter les écrans Opérations et Catégories.
- Ajouter la saisie et la modification manuelles.
- Tester les validations.

### Phase 3 - Tableau de bord et vue annuelle

- Implémenter les agrégations côté serveur.
- Construire les vues mensuelle et annuelle.
- Vérifier les totaux avec des jeux de données connus.

### Phase 4 - Budgets

- Implémenter budgets récurrents et dérogations.
- Construire l'écran de suivi.
- Tester les changements de mois et les dépassements.

### Phase 5 - Import CSV SG

- Demander/inspecter un exemple anonymisé du CSV réel avant de finaliser le parseur.
- Implémenter la prévisualisation, les erreurs, la catégorisation et les doublons.
- Implémenter la validation atomique et l'historique.
- Ajouter des tests avec plusieurs variantes réelles du format si disponibles.

### Phase 6 - Stabilisation

- Vérifier les parcours complets.
- Améliorer les messages d'erreur.
- Vérifier l'affichage mobile.
- Documenter sauvegarde/restauration et limites connues.
- Ne pas ajouter la synchronisation bancaire automatique dans cette phase.

## 14. Consignes à donner à l'agent

- Lire ce document avant de coder et le traiter comme la référence fonctionnelle du MVP.
- Inspecter le dépôt et les fichiers existants avant de créer ou modifier quoi que ce soit.
- En cas d'ambiguïté qui affecte le modèle de données, la sécurité ou les règles de calcul, poser une question plutôt que d'inventer une règle.
- Ne pas implémenter l'import PDF, la synchronisation bancaire, les comptes d'épargne ou les transferts inter-comptes.
- Ne pas inventer le format CSV SG : attendre un fichier d'exemple anonymisé.
- Préférer une solution simple, lisible, testable et maintenable à une architecture générique prématurée.
- Écrire les tests en même temps que les règles métier.
- Ne pas introduire de dépendances sans justification.
- Mettre à jour le README lorsque les commandes ou le comportement du projet changent.
- À la fin de chaque phase, résumer les fichiers créés/modifiés, les tests exécutés, les résultats et les éventuels points bloquants.
- Ne pas commencer la phase suivante si la phase courante ne compile pas ou si ses tests échouent.

---

## Décisions restant à confirmer

- Le format exact du CSV Société Générale, à partir d'un exemple anonymisé.
- Les catégories initiales à migrer depuis le fichier Excel.
- La règle exacte de détection des doublons, selon les champs présents dans le CSV.
- Le mode de sauvegarde local.
- La stratégie d'authentification avant tout accès depuis un autre appareil ou depuis Internet.
