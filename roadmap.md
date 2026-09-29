
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

- Phase 2 — Features MVP (2-4 semaines)
  - CRUD catégories, règles de catégorisation, budgets et tableaux (mensuel/annuel).
  - Déduplication import (empreinte + référence externe) et import commit atomique.
  - Tests unitaires et d'intégration pour règles métier et import.

- Phase 3 — PWA / Mobile (1 semaine)
  - Rendre le frontend PWA (manifest, service-worker), responsive et installable.

- Phase 4 — Déploiement sur Debian/Freebox (1 semaine)
  - Option Docker Compose (recommandé) ou service `systemd`.
  - `nginx` reverse-proxy, volume persistant pour la DB, sauvegardes et HTTPS si exposition externe.

État actuel: Phases 0 et 1 terminées et validées en local. La prochaine étape est la Phase 2: CRUD et fonctionnalités métier; la validation définitive et la déduplication de l'import restent à développer.
