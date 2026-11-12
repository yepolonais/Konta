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
- `PUT /api/accounts/{id}/balances/reference` et `GET /api/accounts/{id}/balances` : définir le solde constaté d'un compte juste après une opération (facultatif : l'import CSV le renseigne automatiquement depuis la ligne de résumé du relevé) et consulter le solde reconstitué.
- `GET /api/annual-budgets/{year}` : consulter les postes, montants mensuels, reports prévisionnels et totaux d'une année.
- `GET /api/categories/orphans` : lister les catégories normales sans poste rattaché, avec le nombre d'opérations et le montant.
- `GET /api/annual-budgets/{year}/months/{month}` : comparer le prévu au réalisé pour un mois, avec le report prévisionnel, le détail des opérations et les opérations non rapprochées.
- `POST /api/annual-budgets/posts/{id}/months/{month}/copy-forward` : recopier le montant d'un mois sur les mois suivants.
- `PUT /api/annual-budgets/{year}/posts/order` : définir l'ordre des postes actifs dans une même section.
- `POST /api/annual-budgets/{year}/posts` : ajouter un poste de revenu, dépense ou épargne planifiée.
- `PUT /api/annual-budgets/posts/{id}` et `DELETE /api/annual-budgets/posts/{id}` : modifier ou supprimer définitivement un poste (avec ses montants mensuels).
- `PUT /api/annual-budgets/posts/{id}/months/{month}` et `DELETE /api/annual-budgets/posts/{id}/months/{month}` : définir ou effacer la prévision d'un mois. Un montant nul explicite est différent d'une prévision absente.
- `PUT /api/annual-budgets/posts/{id}/categories` : associer des catégories normales à un poste ; les opérations existantes et futures de ces catégories sont affectées à ce poste.
- `PUT /api/categories/{id}/multi-post` : activer le mode multi-postes, supprimer le rattachement général et remettre les opérations de la catégorie à rapprocher individuellement.
- `DELETE /api/categories/{id}/multi-post` : revenir au mode normal et retirer les postes individuels, sans effacer les libellés personnels ; la catégorie redevient à rattacher.
- `PUT /api/transactions/{id}/budget-assignment` : enregistrer le poste facultatif et le libellé personnel d'une opération multi-postes.
- L’écran d’accueil propose une grille annuelle pour consulter et modifier les prévisions. Un onglet « Prévu vs réalisé » compare le prévu au réalisé. L’onglet « Rapprochement » rattache les catégories normales aux postes ou permet de passer une catégorie en multi-postes pour affecter chaque opération individuellement. Les enveloppes d'épargne ne comptent que les versements sortants, sans gonfler les dépenses.
- Si un poste rapproché a des opérations mais aucune prévision pour le mois, son écart est calculé avec une prévision de zéro ; la prévision reste absente jusqu'à sa saisie ou son alignement sur le réalisé.
- Les opérations réelles restent distinctes du budget et sont consultables depuis les montants réalisés.
- L'import CSV Société Générale prend en charge la prévisualisation, la validation, la catégorisation et la détection des doublons. La catégorisation repose sur une clé extraite du libellé complet selon le type d'opération (carte, virement, prélèvement, prêt, frais bancaires) ; les règles associent un motif, éventuellement limité à un type, à une catégorie. Les chèques et formats non reconnus restent à catégoriser manuellement. L'onglet « Import CSV » conserve dans la base SQLite locale l'identifiant bancaire et l'adresse du portail, que l'utilisateur peut modifier. « Go » ouvre le portail mais ne télécharge pas le CSV automatiquement.

## Limites métier connues

- Le plan est simple : modifier un montant écrase l'ancien, sans conserver le plan initial. Les mois passés ne sont jamais modifiés par un ajustement futur.
- Une catégorie normale alimente au plus un poste ; une catégorie multi-postes permet de choisir un poste par opération, sans répartir une même opération entre plusieurs postes. Les postes budgétaires ne dépendent pas de l'année : seuls leurs montants mensuels en portent une.
- Les virements entre comptes sont exclus des revenus et dépenses. Pour l'épargne, seuls les versements sortants comptent ; les retraits depuis l'épargne sont ignorés.
- Le disponible prévisionnel affiché est le net du mois (revenus − dépenses − épargne), hors report. Le report cumulé est affiché séparément, et la ligne « Résultat prévisionnel du mois » est calculée comme disponible mensuel plus report (qui peut être négatif). Le report reprend le disponible prévisionnel cumulé de décembre de l'année précédente en janvier.
- Les opérations sans poste sont listées comme « non rapprochées » et ne sont pas comptées dans les postes. Les calculs utilisent le poste enregistré sur chaque opération.
- L'historique du plan initial reste une évolution possible.

Le parseur reconnaît l'export échantillon (`;`, ISO-8859-1, dates `jj/MM/aaaa`, montants à virgule). Les références de compte et les libellés complets sont conservés dans l'aperçu. Les erreurs sont retournées par ligne.
