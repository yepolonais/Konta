# Konta

Application de suivi des finances du foyer. L'espace Budget permet de créer un budget prévisionnel annuel distinct des opérations réelles, avec des postes de revenus, de dépenses (classées dans l'interface en charges fixes ou dépenses variables) et d'épargne planifiée. L'application fournit une API .NET 8, une base SQLite et une interface JavaScript avec Web Components.

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
- `GET /api/annual-budgets/{year}` : consulter les postes, montants mensuels, reports prévisionnels et totaux d'une année.
- `GET /api/annual-budgets/{year}/months/{month}` : comparer le prévu au réalisé pour un mois, avec le report prévisionnel, le détail des opérations et les opérations non rapprochées.
- `POST /api/annual-budgets/posts/{id}/months/{month}/copy-forward` : recopier le montant d'un mois sur les mois suivants.
- `PUT /api/annual-budgets/{year}/posts/order` : définir l'ordre des postes actifs dans une même section.
- `POST /api/annual-budgets/{year}/extras` et `DELETE /api/annual-budgets/extras/{id}` : ajouter ou supprimer un extra ponctuel.
- `POST /api/annual-budgets/{year}/posts` : ajouter un poste de revenu, dépense ou épargne planifiée.
- `PUT /api/annual-budgets/posts/{id}` et `DELETE /api/annual-budgets/posts/{id}` : modifier ou supprimer définitivement un poste (avec ses montants mensuels).
- `PUT /api/annual-budgets/posts/{id}/months/{month}` et `DELETE /api/annual-budgets/posts/{id}/months/{month}` : définir ou effacer la prévision d'un mois. Un montant nul explicite est différent d'une prévision absente.
- `PUT /api/annual-budgets/posts/{id}/categories` : associer une ou plusieurs catégories réelles à un poste de revenu ou dépense (une catégorie par poste actif et par année).
- L’écran d’accueil propose une grille annuelle pour consulter et modifier les prévisions. Un onglet « Suivi mensuel » compare le prévu au réalisé des revenus et dépenses catégorisés, et c’est là que l’on rattache les catégories réelles à chaque poste. Les enveloppes d'épargne sont rapprochées comme les autres postes, via des catégories rattachées : seuls les versements sortants comptent, sans gonfler les dépenses.
- Les opérations réelles restent distinctes du budget et sont consultables depuis les montants réalisés.
- L'import CSV Société Générale prend en charge la prévisualisation, la validation, la catégorisation et la détection des doublons.

## Limites métier connues

- Le plan est simple : modifier un montant écrase l'ancien, sans conserver le plan initial. Les mois passés ne sont jamais modifiés par un ajustement futur.
- Le rapprochement se fait par catégorie (pas par sous-catégorie) : une catégorie ne peut alimenter qu'un poste actif par année.
- Les virements entre comptes sont exclus des revenus et dépenses. Pour l'épargne, seuls les versements sortants comptent ; les retraits depuis l'épargne sont ignorés.
- Le disponible prévisionnel affiché est le net du mois (revenus + extras − dépenses − épargne), hors report. Le report cumulé est affiché séparément, et la ligne « Résultat prévisionnel du mois » est calculée comme disponible mensuel plus report (qui peut être négatif). Le report reprend le disponible prévisionnel cumulé de décembre de l'année précédente en janvier.
- Les opérations sans catégorie rattachée à un poste sont listées comme « non rapprochées » et ne sont pas comptées dans les postes.
- La réaffectation entre postes et l'historique du plan initial sont des évolutions possibles (voir [roadmap.md](./roadmap.md)).

Le parseur reconnaît l'export échantillon (`;`, ISO-8859-1, dates `jj/MM/aaaa`, montants à virgule). Les références de compte et les libellés complets sont conservés dans l'aperçu. Les erreurs sont retournées par ligne.
