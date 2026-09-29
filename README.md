# Konta

Application de suivi des finances du foyer. La phase 1 fournit une API .NET 8, une base SQLite, une interface JavaScript avec Web Components et la prévisualisation des relevés CSV Société Générale.

## Prérequis

- .NET 8 SDK

## Démarrer en local

```bash
dotnet restore Konta.sln
dotnet run --project src/Konta.Api
```

Ouvrir l'URL locale affichée par ASP.NET Core dans le terminal. L'API et le frontend sont servis par le même processus.

La base est créée automatiquement par les migrations EF Core, par défaut dans le répertoire de données local de l'utilisateur (`~/.local/share/Konta/konta.db` sous Linux). Le chemin peut être changé avec `Konta__DatabasePath`. Ne pas exposer le serveur sur le réseau sans ajouter authentification et HTTPS.

## Vérifier

```bash
dotnet build Konta.sln
dotnet test Konta.sln
```

## Fonctionnalités disponibles

- `GET /api/health` : état de l'API.
- `GET /api/transactions` : liste des opérations enregistrées (vide au premier démarrage).
- `POST /api/imports/preview` : envoyer un CSV en multipart, champ `file`; analyse en mémoire uniquement, aucune transaction n'est enregistrée.
- Interface web pour consulter les opérations et prévisualiser un CSV.

Le parseur reconnaît l'export échantillon (`;`, ISO-8859-1, dates `jj/MM/aaaa`, montants à virgule). Les références de compte et les libellés complets sont conservés dans l'aperçu. Les erreurs sont retournées par ligne. L'import définitif n'est pas encore implémenté.
