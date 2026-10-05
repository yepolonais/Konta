---
name: 'Konta Tests'
description: 'Conventions des tests unitaires et d’intégration de Konta.'
applyTo: 'src/Konta.Tests/**/*.cs'
---
# Tests Konta

- Garde les tests dans le projet `src/Konta.Tests`, séparés par dossiers `Unit` et `Integration`. Utilise xUnit (`[Fact]`, `Assert`) pour les deux catégories.
- Un test unitaire couvre un comportement précis d’une classe sans démarrer l’hôte Web ni utiliser une vraie base de données. Mocker avec Moq les dépendances externes ou les ports dont le comportement n’est pas celui testé ; évite de mocker les détails internes du framework ou la classe testée.
- Utilise AutoFixture pour générer les données et objets de test complexes. Avec les dépendances mockées, utilise `AutoFixture.AutoMoq` et gèle (`Freeze`) les mocks injectés dans le système testé. Garde explicites les valeurs qui déterminent le scénario et n’utilise pas AutoFixture pour le système testé ni les ressources techniques (`Stream`, `HttpClient`, etc.).
- N’ajoute qu’un comportement indépendant par test. Plusieurs assertions sont acceptables lorsqu’elles vérifient ensemble le même résultat observable ; sépare les comportements indépendants.
- Nomme les tests `Methode_ResultatAttendu_QuandCondition`, par exemple `PreviewAsync_AssociatesAccount_WhenAccountNumberIsMapped`. Utilise un suffixe de condition uniquement quand il clarifie le scénario.
- Marque chaque classe avec `[Trait("Category", "Unit")]` ou `[Trait("Category", "Integration")]` selon le dossier, afin de pouvoir lancer chaque groupe avec `dotnet test src/Konta.Tests --filter Category=Unit` ou `--filter Category=Integration`.
- Les tests d’intégration couvrent les contrats observables de plusieurs couches, notamment les endpoints HTTP, la sérialisation et la persistance. Utilise `WebApplicationFactory<Program>` et une base SQLite temporaire propre à chaque scénario ; jamais la base de développement ou une base partagée.
- Utilise `sample/transactions-sample.csv` comme fixture de relevé. N’utilise pas de fichiers réels de `secret/` ou de données financières personnelles.
- Vérifie les comportements observables et garde les tests indépendants et déterministes. Nettoie les ressources `IDisposable` et `IAsyncDisposable` créées par le test.
- Lance `dotnet test Konta.sln` après les changements de comportement. `dotnet build Konta.sln` vérifie également la compilation de toute la solution.
- Utilise la convention // Arrange // Act // Assert au sein des TUs
