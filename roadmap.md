
# Roadmap concise

Objectif: construire un POC web (API .NET 8 + frontend Web Components), puis transformer en PWA mobile et déployer sur Debian/Freebox.

Phases principales

- Phase 0 — Cadrage (1-2 jours)
  - Valider l'échantillon CSV (`sample/transactions-sample.csv`) et l'encodage.
  - Confirmer SDK .NET 8 installé et chemin de la DB SQLite.

- Phase 1 — POC Web (1-2 semaines)
  - Scaffolder solution DDD (.sln + projets Domain, Application, Infrastructure, Api, Tests).
  - Implémenter entités principales + `KontaDbContext` et migrations SQLite.
  - Écrire parseur CSV POC (preview non persistante) et endpoint `POST /api/imports/preview`.
  - Frontend minimal: liste opérations + écran d'import preview.

- Phase 2 — Features MVP (2-4 semaines)
  - CRUD catégories, règles de catégorisation, budgets et tableaux (mensuel/annuel).
  - Déduplication import (empreinte + référence externe) et import commit atomique.
  - Tests unitaires et d'intégration pour règles métier et import.

- Phase 3 — PWA / Mobile (1 semaine)
  - Rendre le frontend PWA (manifest, service-worker), responsive et installable.

- Phase 4 — Déploiement sur Debian/Freebox (1 semaine)
  - Option Docker Compose (recommandé) ou service `systemd`.
  - `nginx` reverse-proxy, volume persistant pour la DB, sauvegardes et HTTPS si exposition externe.

Prochain choix (à vous): je démarre par 1) scaffolder la solution `.NET` ou 2) écrire le parseur CSV POC. Indiquez 1 ou 2.
