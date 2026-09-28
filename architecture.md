
# Architecture proposée

Résumé de l'architecture DDD pour l'application Konta (backend .NET 8 + frontend Web Components PWA).

Arborescence projet (schéma simplifié) :

-- Konta.sln
-- src
 |-- Api
 |    |-- Program.cs
 |    |-- Controllers/
 |    |-- DTOs/
 |    |-- Middlewares/
 |
 |-- Domain
 |    |-- Entities/
 |    |-- ValueObjects/
 |    |-- Exceptions/
 |    |-- Interfaces/ (repositories, services)
 |
 |-- Application
 |    |-- UseCases/
 |    |-- Services/
 |    |-- DTOs/ (application-level)
 |
 |-- Infrastructure
 |    |-- Persistence/
 |    |    |-- KontaDbContext.cs
 |    |    |-- Migrations/
 |    |-- Repositories/
 |    |-- Files/ (import handling)
 |
 |-- Frontend
 |    |-- index.html
 |    |-- manifest.json
 |    |-- service-worker.js
 |    |-- components/
 |    |-- views/
 |
-- tests
 |-- Konta.Tests (unitaires)
 |-- Konta.IntegrationTests

-- docs/
-- sample/ (exemples CSV)
-- secret/ (dossier ignoré — ne pas committer)

Notes rapides :

- `Api` expose les endpoints REST et sert éventuellement les assets statiques pour un déploiement simple.
- `Domain` contient le modèle métier pur (entités, règles, value objects).
- `Application` orchestre les cas d'usage et effectue la validation/coordination.
- `Infrastructure` implémente les repositories EF Core et gère la persistence SQLite + migrations.
- `Frontend` est une application JS natif + Web Components, conçue comme PWA pour accès smartphone.

Si vous validez ce plan d'arborescence, je peux :

- scaffolder la solution `.NET` et les projets vides, ou
- créer un prototype du parseur CSV utilisant `sample/transactions-sample.csv`.
