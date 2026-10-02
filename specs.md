# Fonctionnalités observées dans `Budget-anonym.xlsx`

## Vue d'ensemble

Le classeur est un budget familial annuel organisé autour de l'année 2026. Il contient une seule feuille visible, `2026`, avec les mois de janvier à décembre en colonnes. Les montants peuvent être saisis directement ou calculés par des formules. Le fichier sert à prévoir et suivre les dépenses, comparer revenus et sorties, suivre le solde disponible et répartir une épargne par objectif.

## Fonctionnalités

### 1. Prévision et suivi mensuel des dépenses

- Saisie d'un montant par mois pour chaque poste : prêt, énergie, eau, frais bancaires, internet, assurances, voiture, carburant, impôts, alimentation, école, vacances, enfants, travaux, imprévus et autres dépenses.
- Possibilité d'exprimer un montant comme une formule arithmétique, par exemple pour additionner plusieurs frais ou soustraire un remboursement.
- Répétition de montants mensuels prévisionnels pour des charges récurrentes.
- Calcul d'un total annuel et d'une moyenne mensuelle par poste. Une estimation hebdomadaire est également calculée à partir du total divisé par 52.

### 2. Détail des dépenses variables

- La rubrique « autres » est décomposée en postes tels que sport, fêtes, transports/parking/courrier, santé, vêtements, restaurant, autres et chat. Un total mensuel agrège ces postes.
- L'alimentation dispose de plusieurs lignes de détail mensuel, dont les montants sont additionnés dans un total par mois.
- Ces totaux détaillés alimentent les rubriques correspondantes du budget principal par formules.
- Le détail est saisi sous forme de montants et de sommes de montants dans les cellules ; le classeur n'enregistre pas de transactions avec date, commerçant ou justificatif.

### 3. Revenus et résultat mensuel

- Suivi mensuel de plusieurs sources de revenus : deux revenus nominatifs anonymisés (`YEPO` et `JEN`) et la CAF.
- Calcul des revenus reçus par mois et sur l'année.
- Calcul de la différence entre revenus reçus et dépenses.
- Prise en compte d'un antécédent/report et calcul d'un total avec antécédent, pour suivre l'évolution du disponible d'un mois à l'autre.

### 4. Suivi du compte et du reste à financer

- Le haut de la feuille contient une date de référence et un montant « sur le compte » saisi manuellement.
- Des formules utilisent ce montant et les dépenses pour estimer le reste disponible au fil des mois.
- Une ligne « ce qu'il nous manque » compare cette estimation au résultat budgétaire afin d'indiquer un écart à couvrir.

### 5. Épargne affectée à des objectifs

- Suivi mensuel de sommes mises de côté pour des enveloppes : impôts, vacances, imprévus, chat, enfants, travaux et voitures.
- Calcul des montants épargnés par mois et par enveloppe, avec total annuel, moyenne mensuelle et estimation hebdomadaire.
- Tableau de correspondance entre plusieurs objectifs d'épargne et le compte associé (par exemple LDD, CEL ou compte travaux).

### 6. Comparaison avec des années précédentes

- La feuille comprend des colonnes intitulées `2024` et `2023`, avec des valeurs de total et de moyenne pour plusieurs postes.
- Ces valeurs historiques sont présentes comme données dans le classeur ; leur méthode de mise à jour n'est pas explicitée.
- La feuille `2025`, dans le classeur d'origine, reprend la même structure annuelle pour l'année précédente. Certaines formules de `2026` s'appuient sur ses valeurs pour assurer la continuité entre les exercices.

### 7. Organisation de la feuille

- Les deux premières lignes et la première colonne sont figées lors du défilement, ce qui conserve les mois et les libellés visibles.
- La feuille ne contient pas de tableau Excel structuré, filtre, liste de validation, graphique ou mise en forme conditionnelle détectable.

## Limites et points à clarifier

- La copie anonymisée fournie ne contient que la feuille `2026` ; la feuille homologue `2025` du classeur d'origine n'est pas incluse. Les liens entre exercices sont donc identifiés dans les formules de `2026`, mais les valeurs sources de `2025` ne sont pas visibles dans cette copie.
- Plusieurs colonnes de comparaison ou de calcul n'ont pas d'intitulé explicite. Leur sens exact, notamment certaines valeurs hebdomadaires, est déduit de formules comme `total / 52` et reste à confirmer.
- Les données mélangent prévisions récurrentes, montants réalisés et calculs manuels. Le classeur n'indique pas systématiquement le statut d'un montant ni la règle de passage du prévu au réalisé.
- Les noms de catégories et de personnes sont ceux présents dans la copie anonymisée ; ils ne définissent pas nécessairement les libellés définitifs du besoin.
