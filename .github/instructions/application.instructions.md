---
name: 'Konta Application'
description: 'Conventions des services applicatifs et contrats de cas d’usage Konta.'
applyTo: 'src/Konta.Application/**/*.cs'
---

# Couche Application

- Cette couche orchestre les cas d’usage et dépend de `Konta.Domain`, pas de l’API ni de `Konta.Infrastructure`. N’y ajoute pas de dépendance à ASP.NET Core ou à Entity Framework.
- Organise le code par fonctionnalité (`Accounts`, `Imports`, `Transactions`) et garde ensemble le service, ses interfaces et ses modèles de résultat associés.
- Place les abstractions nécessaires aux cas d’usage (par exemple repositories et parseurs) dans cette couche ; leurs implémentations concrètes restent dans `Konta.Infrastructure`.
- Les services applicatifs coordonnent les étapes et les ports, comme `ImportPreviewService`; les règles métier invariantes appartiennent au domaine.
- Pour les accès asynchrones, accepte et propage un `CancellationToken`. Garde les appels annulables asynchrones jusqu’au repository.
- Préserve les contrats et modèles existants. Utilise les records et les transformations non mutantes déjà présents lorsque tu fais évoluer les résultats d’un cas d’usage.
- Ajoute ou adapte des tests ciblés dans `src/Konta.Tests` pour chaque nouveau comportement applicatif.