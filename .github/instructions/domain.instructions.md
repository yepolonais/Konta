---
name: 'Konta Domain'
description: 'Conventions du modèle métier et des règles du domaine Konta.'
applyTo: 'src/Konta.Domain/**/*.cs'
---

# Domaine Konta

- Le domaine reste une bibliothèque .NET autonome et ne dépend ni d’ASP.NET Core, ni d’EF Core, ni de `Konta.Application` ou `Konta.Infrastructure`.
- Organise le modèle par concepts métier (`Accounts`, `Categories`, `Transactions`) et conserve les namespaces alignés sur ces dossiers.
- Porte les invariants métier au plus près des entités et objets concernés. Valide les arguments avant de modifier l’état ; conserve l’encapsulation observée avec des setters privés et des constructeurs adaptés à EF Core.
- N’introduis pas de détails de transport, de persistance ou d’interface utilisateur dans les types du domaine.
- Réutilise les types métier existants, notamment les enums, plutôt que de représenter leurs valeurs avec des chaînes libres.
- Couvre par des tests les invariants et transitions métier, sans nécessiter la base de données ni l’hôte Web.