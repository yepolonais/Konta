
# Roadmap concise

Objectif: construire un POC web (API .NET 8 + frontend Web Components), puis transformer en PWA mobile et déployer sur Debian/Freebox.

Phases principales

- [x] Phase 0 — Cadrage (1-2 jours)
  - [x] Valider l'échantillon CSV (`sample/transactions-sample.csv`) et l'encodage.
  - [x] Confirmer SDK .NET 8 installé et chemin de la DB SQLite.

- [x] Phase 1 — POC Web (1-2 semaines)
  - [x] Créer la solution `Konta.sln` et les projets `Konta.Api`, `Konta.Application`, `Konta.Domain`, `Konta.Infrastructure`, `Konta.Tests` (`net8.0`) avec leurs références.
  - [x] API: `Program.cs`, configuration et `GET /api/health` pour vérifier le démarrage.
  - [x] Domaine: modèles `Transaction`, `Category` et enums nécessaires; montant positif, type revenu/dépense, compte source conservé.
  - [x] Infrastructure: EF Core SQLite, `KontaDbContext`, configurations et première migration; chemin de la base configurable hors du dépôt.
  - [x] Application: cas d'usage et contrats pour consulter les opérations et prévisualiser un import; API sans accès direct au `DbContext`.
  - [x] Import CSV: parser le format observé (`;`, ISO-8859-1, dates `dd/MM/yyyy`, montants avec virgule); afficher les erreurs par ligne. Garder dates, compte (`Num Compte`, `Libellé Compte`), libellés et montant/type dans le modèle importé. Pas de persistance à l'étape preview.
  - [x] API: `POST /api/imports/preview` reçoit un fichier, renvoie opérations interprétées et erreurs, sans enregistrer les transactions.
  - [x] Frontend `frontend/`: HTML, CSS et JavaScript natif avec Web Components; liste des opérations et formulaire d'import/preview utilisant `fetch`.
  - [x] Tests: parser sur `sample/transactions-sample.csv` (encodage, séparateur, montants, compte) et tests API; `dotnet build` et `dotnet test` doivent réussir.
  - [x] Validation de fin: démarrer l'API et le frontend, prévisualiser l'échantillon, confirmer qu'aucune transaction n'est persistée avant une future étape de commit.

- [x] Phase 2 — Import opérationnel et comptes multiples
  - [x] Modéliser les comptes du foyer (compte commun, personnel et épargne) et associer chaque compte SG à un compte Konta à partir de `Num Compte`; demander confirmation pour un compte inconnu.
  - [x] Étendre l'aperçu CSV à la sélection, l'exclusion et la correction des opérations avant validation; conserver l'affectation au compte correspondant.
    - [x] Sélectionner ou exclure chaque opération dans l'aperçu; corriger sa date, son libellé, son type et son montant avant le commit.
    - [x] Corriger l'affectation du compte et la catégorie d'une opération directement dans l'aperçu.
  - [x] Détecter les doublons, enregistrer l'import de façon atomique et conserver son historique. Ne pas fusionner aveuglément des opérations identiques légitimes.
  - [x] Ajouter le CRUD des catégories et leur association aux opérations importées; permettre de revoir rapidement les opérations non catégorisées.
  - [x] Ajouter le CRUD API et l'interface de gestion des règles de catégorisation avec validation, détection des doublons et désactivation.
  - [x] Appliquer à l'aperçu et au commit la règle active la plus spécifique pour catégoriser les opérations sans catégorie; afficher la suggestion et permettre la correction manuelle.
  - [x] Ajouter les vues mensuelle et annuelle calculées depuis les opérations, avec filtres par compte/catégorie et accès aux opérations composant les totaux.
  - [x] Tester l'import couvrant plusieurs comptes, l'affectation, les doublons, les suggestions de catégorie et les calculs par période.

- [x] Phase 3 — Budgets, prévisions et soldes
  - [x] Gérer les budgets mensuels récurrents et leurs dérogations ponctuelles.
  - [x] Créer des prévisions d'échéances récurrentes séparées des transactions réelles; les prévisions ne modifient pas les dépenses ni les soldes constatés.
  - [x] À l'import, proposer le rapprochement entre une prévision et une opération réelle du même compte. Après confirmation, marquer l'échéance comme réalisée et utiliser les données de l'opération importée; signaler les écarts et laisser les cas ambigus à confirmer.
  - [x] Permettre de définir, pour chaque compte, un solde de référence associé à une opération. Ce solde correspond au compte juste après cette opération; calculer le solde constaté en ajoutant les mouvements ultérieurs, sans compter deux fois l'opération de référence.
  - [x] Afficher séparément le solde constaté et le solde prévisionnel; inclure les échéances futures uniquement dans ce dernier.
  - [x] Tester les rapprochements, les échéances manquantes ou inattendues, les écarts de montant et le calcul des soldes à partir de la référence.

- [ ] Phase 4 — Transferts et objectifs d'épargne
  - [ ] Représenter les transferts entre comptes comme des mouvements liés, neutres dans les totaux de dépenses et de revenus du foyer.
  - [ ] Repérer les transferts potentiels entre comptes à l'import et demander confirmation lorsque la correspondance est incertaine.
  - [ ] Gérer les objectifs d'épargne (compte associé, montant cible et contribution prévue) et comparer contributions prévues et réalisées à partir des transferts enregistrés.
  - [ ] Tester qu'un transfert ne crée ni dépense ni revenu et que les contributions d'épargne ne sont pas comptées deux fois.

- [ ] Phase 5 — PWA / Mobile (1 semaine)
  - [ ] Rendre le frontend PWA (manifest, service-worker), responsive et installable.

- [ ] Phase 6 — Déploiement sur Debian/Freebox (1 semaine)
  - [ ] Option Docker Compose (recommandé) ou service `systemd`.
  - [ ] `nginx` reverse-proxy, volume persistant pour la DB, sauvegardes et HTTPS si exposition externe.

État au 18/10/2026: Phases 0 à 3 terminées. L'import multi-comptes permet de revoir/corriger les opérations, de détecter les doublons et d'associer les catégories. Les catégories et règles sont gérables depuis l'interface. Les règles proposent une catégorie pendant l'aperçu (motif le plus spécifique prioritaire), avec possibilité de correction avant commit. Les synthèses mensuelles et annuelles incluent des filtres par compte/catégorie et le détail des opérations composant chaque total. La phase 3 ajoute les budgets mensuels et dérogations, les prévisions récurrentes (mensuelles ou annuelles), leur rapprochement explicite à l'import, et les soldes constatés/prévisionnels à partir d'une opération de référence. Les échéances non rapprochées restent distinctes des opérations réelles; les soldes prévisionnels de l'interface sont calculés jusqu'à la fin du mois sélectionné. La suite complète de tests (34 tests) et les parcours navigateur de planification ont été validés. La phase 4 (transferts et objectifs d'épargne) reste à réaliser. La phase 5 PWA est reportée jusqu'à la fin des fonctionnalités métier des phases 3 et 4.
