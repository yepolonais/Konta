---
name: 'Konta Frontend'
description: 'Conventions de l’interface JavaScript native, Web Components et CSS de Konta.'
applyTo: 'frontend/**'
---

# Frontend Konta

- Le frontend est servi par l’API et utilise JavaScript natif, HTML et CSS, sans framework. Préserve cette structure et évite d’ajouter une chaîne de compilation ou une dépendance sans nécessité établie.
- L’interface est implémentée en Web Components ; `app.js` définit `konta-app` et utilise son Shadow DOM. Garde le balisage, les styles et les comportements cohérents avec cette structure.
- Les appels API sont same-origin et utilisent les routes `/api/...`. Gère les états de chargement, de succès et d’erreur sans perdre inutilement la sélection ou les résultats de l’utilisateur.
- Les libellés et données de relevé sont des entrées non fiables : construis les éléments dynamiques avec `document.createElement` et `textContent`. N’insère jamais ces valeurs dans `innerHTML`.
- Préserve l’accessibilité : HTML sémantique, libellés de champs, états désactivés explicites, messages de statut accessibles et interactions utilisables au clavier.
- Les styles vivent dans `styles.css`, utilisent les variables définies sur `:host` et conservent les règles responsive existantes. Vérifie les vues larges et étroites après toute modification visuelle.
- N’ajoute ni identifiants de compte, ni relevés réels, ni secrets dans le code, le DOM de démonstration ou les messages d’erreur.
- Il n’existe pas de commande de tests frontend établie dans le dépôt. Pour une modification UI, vérifie au minimum le lancement de l’application avec `dotnet run --project src/Konta.Api` et le parcours concerné dans le navigateur.