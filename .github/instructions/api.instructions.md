---
name: 'Konta API'
description: 'Conventions pour les endpoints et l’hébergement ASP.NET Core de Konta.'
applyTo: 'src/Konta.Api/**/*.cs'
---

# API Konta

- Cible ASP.NET Core .NET 8. Garde les contrôleurs minces : ils traitent le protocole HTTP, valident les entrées propres à la requête et délèguent les cas d’usage à `Konta.Application`.
- Suis les conventions existantes : contrôleurs sous `Controllers`, routes sous `/api`, attributs ASP.NET (`[ApiController]`, `[Route]`, `[HttpGet]`, `[HttpPost]`) et réponses typées `ActionResult<T>` lorsque c’est adapté.
- Pour les erreurs HTTP, conserve le format `ProblemDetails` et les codes de statut cohérents avec les endpoints voisins. N’ajoute pas de capture générique qui masque les erreurs.
- Les opérations asynchrones acceptent et transmettent un `CancellationToken` jusqu’aux services appelés.
- Déclare les endpoints avec leurs métadonnées OpenAPI utiles (`Consumes`, `ProducesResponseType`, etc.) comme dans les contrôleurs existants.
- Enregistre les services applicatifs et l’infrastructure dans le point de composition existant (`Program.cs` et `AddKontaInfrastructure`) ; ne place pas de logique métier ou d’accès EF Core dans les contrôleurs.
- Toute évolution d’endpoint doit être couverte par un test d’intégration dans `src/Konta.Tests`.
- Ne journalise pas et ne renvoie pas de données bancaires sensibles qui ne sont pas nécessaires à la réponse.