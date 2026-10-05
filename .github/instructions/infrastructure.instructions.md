---
name: 'Konta Infrastructure'
description: 'Conventions des adaptateurs de persistance, CSV et infrastructure Konta.'
applyTo: 'src/Konta.Infrastructure/**/*.cs'
---

# Infrastructure Konta

- Cette couche implémente les abstractions de `Konta.Application` et peut utiliser `Konta.Domain`. Ne fais pas dépendre les autres couches de ses implémentations concrètes.
- La persistance actuelle utilise EF Core avec SQLite. Centralise le mapping dans `Persistence/KontaDbContext.cs`, les accès dans les repositories et les migrations dans `Persistence/Migrations`.
- Lors d’un changement de schéma, mets à jour les migrations de façon cohérente avec le modèle EF Core ; ne modifie pas une migration déjà appliquée sans raison explicite.
- Enregistre les implémentations dans `DependencyInjection.cs`. Utilise les durées de vie et le fournisseur de base déjà établis.
- Les appels de base de données transmettent le `CancellationToken` fourni par l’appelant. Garde les requêtes et écritures dans les repositories plutôt que dans les contrôleurs.
- Le parseur Société Générale doit préserver le format constaté : séparateur `;`, encodage Latin-1, dates `dd/MM/yyyy`, montants français et erreurs rapportées par ligne. Garde les règles de lecture CSV dans l’adaptateur dédié, pas dans l’API.
- N’inclus jamais les fichiers de relevés réels ou le contenu du dossier `secret/` dans les fixtures, les logs ou le code. Utilise `sample/transactions-sample.csv` pour les exemples de test.
- Couvre les parseurs et repositories par des tests adaptés dans `src/Konta.Tests`.