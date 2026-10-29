# Konta — référence métier

**Statut :** source de vérité des besoins métier de l'application  
**Périmètre :** gestion du budget prévisionnel du foyer et suivi de sa réalisation

En cas de contradiction avec d'anciens documents de cadrage, ce document prévaut pour les besoins métier. Les choix techniques et les détails d'implémentation peuvent être décrits ailleurs, mais ne doivent pas modifier ces objectifs sans décision explicite.

## 1. Objectif de l'application

Konta aide le foyer à prévoir ses finances sur une année et à garder le contrôle de son budget au fil des mois. L'application doit permettre d'anticiper les revenus et les dépenses par poste, d'adapter le plan quand la situation évolue, puis de comparer les prévisions aux opérations réellement constatées sur les comptes.

L'objectif est d'éviter de découvrir trop tard qu'une dépense importante ou un manque de revenus met le foyer en difficulté. Le budget prévisionnel annuel est le point de départ et la référence du suivi. L'import et l'analyse des opérations bancaires servent à mesurer le réel par rapport à ce plan.

## 2. Utilisateurs et unité de gestion

- L'application sert à gérer le budget partagé du foyer.
- Les membres du foyer consultent et font évoluer le même budget et les mêmes opérations.
- Le vocabulaire et les catégories doivent pouvoir être adaptés au foyer ; aucun exemple de poste ne doit être imposé comme une taxonomie figée.

## 3. Le plan budgétaire annuel

### 3.1 Organisation

- L'utilisateur peut créer et consulter un budget pour une année donnée.
- Le budget présente les mois de janvier à décembre et les postes budgétaires en lignes.
- Un poste peut représenter un revenu ou une dépense.
- Les postes sont nommés librement, par exemple : salaires, loyer, électricité, internet, assurances, courses, loisirs, impôts, vacances ou imprévus.
- Les revenus prévisionnels comprennent notamment les salaires et peuvent inclure d'autres sources.
- Les dépenses prévisionnelles couvrent les charges régulières comme les dépenses variables ou ponctuelles.
- Dans l'interface, les dépenses peuvent être réparties entre « Charges fixes » (montants réguliers liés à un contrat) et « Dépenses variables ». Cette séparation est une classification de présentation : les deux catégories restent des dépenses et alimentent les mêmes totaux et comparaisons.
- Un poste peut avoir un montant prévu différent pour chaque mois. La répétition d'un montant d'un mois à l'autre est une facilité de saisie, pas une obligation.
- Le budget affiche les totaux par mois et par poste, les totaux annuels des revenus et dépenses, ainsi que le disponible prévisionnel mensuel (revenus + extras − dépenses − épargne). Sous cette ligne, il affiche le report du mois précédent, calculé à partir du plan et non éditable ; janvier reprend le disponible prévisionnel cumulé de décembre de l'année précédente. La dernière ligne est le résultat prévisionnel du mois : disponible mensuel plus report du mois précédent. Un report négatif réduit le résultat. Ce résultat n'est pas le solde de clôture d'un compte bancaire.

### 3.2 Types de postes et montants

- Un poste peut être récurrent, variable, ponctuel ou affecté à un objectif d'épargne.
- Les montants prévisionnels sont définis par mois ; ils ne sont pas limités à une valeur mensuelle récurrente unique.
- Un poste peut être prévu à zéro ou ne pas avoir de montant prévu pour un mois.
- Les dépenses variables et les montants mis de côté pour un objectif — par exemple une enveloppe vacances — doivent pouvoir être augmentés, réduits ou suspendus pour un mois donné sans effacer les mois passés.
- Les postes et montants prévisionnels restent distincts des opérations bancaires réalisées : créer un poste dans le budget ne crée pas une transaction.
- Une somme affectée à une enveloppe d'épargne doit être identifiable comme telle et ne doit pas être confondue automatiquement avec une dépense de consommation. Le suivi doit éviter de compter deux fois la même somme lorsqu'elle est mise de côté puis dépensée.

### 3.3 Ajustements en cours d'année

Le budget est révisable. Une modification en cours d'année ne doit pas réécrire l'historique ni masquer le plan qui avait été retenu pour les mois déjà écoulés.

L'utilisateur doit pouvoir :

- ajouter un apport ou un montant exceptionnel (« extra ») pour un mois ;
- augmenter ou réduire un montant prévu pour un poste et un mois ;
- suspendre ou supprimer un poste variable pour un ou plusieurs mois futurs ;
- réaffecter une partie du budget entre postes si nécessaire ;
- conserver les valeurs passées et visualiser les changements apportés au plan restant.

Un extra doit apparaître séparément des revenus réguliers et des postes ordinaires afin que son effet sur le budget soit compréhensible. Il ne doit pas être traité comme un salaire récurrent. La façon de distinguer un nouvel apport d'une réaffectation interne doit être explicite dans l'interface et dans les calculs.

## 4. Suivi du réalisé

### 4.1 Opérations bancaires

- Les opérations réelles sont principalement obtenues par l'analyse et l'import des relevés bancaires.
- Une opération importée conserve ses informations utiles, notamment sa date, son libellé, son montant et son type (revenu ou dépense).
- Les opérations peuvent être rattachées à un poste budgétaire afin de comparer le réalisé à la prévision correspondante.
- Les catégories et règles de classement doivent aider à affecter les opérations de manière cohérente, tout en permettant de corriger un classement erroné.
- Les doublons d'import ne doivent pas gonfler les dépenses ou revenus réalisés.
- La saisie manuelle d'une opération peut dépanner pour un cas absent du relevé, mais elle n'est pas le parcours central du produit.

### 4.2 Comparaison prévu / réalisé

Pour un mois sélectionné, l'utilisateur doit pouvoir comparer, par poste :

- le montant prévisionnel retenu pour ce mois ;
- le montant réellement constaté dans les opérations importées ;
- l'écart entre le réalisé et le prévisionnel ;
- le montant restant ou le dépassement.

La comparaison doit également montrer les totaux des revenus et dépenses, ainsi que le disponible prévisionnel du mois, sans le report. Le report du mois précédent est affiché séparément et calculé à partir des montants prévisionnels, sans tenir compte des opérations réelles. Les montants doivent être calculés à partir du budget et des opérations enregistrées, pas maintenus comme des totaux indépendants susceptibles de diverger.

Pour l'année, l'utilisateur doit pouvoir consulter les mois écoulés, le cumul prévu et réalisé, et le budget révisé pour les mois à venir. Un dépassement doit être visible et compréhensible afin de permettre une action sur le reste de l'année.

## 5. Parcours métier principal

1. L'utilisateur crée ou ouvre le budget de l'année.
2. Il définit ses postes de revenus et dépenses, puis inscrit les montants prévisionnels pour chaque mois.
3. Il vérifie les totaux mensuels et annuels et ajuste le plan jusqu'à ce qu'il soit soutenable.
4. Au fil de l'année, il importe les opérations du compte et les classe par poste.
5. Chaque mois, il compare le réalisé aux prévisions et repère les écarts, notamment les dépassements.
6. Si nécessaire, il ajoute un extra, réaffecte des montants ou ajuste/suspend les postes variables pour les mois restants.
7. Il conserve la visibilité sur ce qui était prévu auparavant, ce qui a été réalisé et ce qui est désormais prévu.

## 6. Principes de présentation

- Le budget annuel doit être lisible sous la forme de postes en lignes et de mois en colonnes.
- Les revenus et dépenses sont présentés distinctement.
- Les montants prévus, les montants réalisés et les écarts ne doivent pas être confondus visuellement.
- Les mois sans montant prévu doivent pouvoir être distingués des montants explicitement prévus à zéro lorsque cela est utile.
- Les montants et dates sont présentés selon les usages français.
- L'interface doit rendre visibles les dépassements et le résultat prévisionnel restant, sans nécessiter de recalcul manuel dans un tableur.

## 7. Ce qui n'est pas l'objectif premier

- L'application n'est pas d'abord un simple registre de transactions bancaires.
- Le classement des opérations n'a de valeur que s'il alimente la comparaison avec le budget prévu.
- La saisie manuelle de chaque opération n'est pas requise pour utiliser le parcours principal.
- L'objectif n'est pas de reproduire à l'identique la mise en page ou les formules d'un classeur particulier : il s'agit de conserver et d'améliorer ses usages métier.
- Les évolutions de déploiement, d'interface ou de technologie, dont le passage en PWA, ne définissent pas les besoins métier décrits ici.

## 8. Critères métier essentiels

Le besoin principal est couvert lorsque l'utilisateur peut :

1. bâtir un budget annuel avec des revenus et des dépenses nommés librement, détaillés mois par mois ;
2. voir les totaux et le résultat prévisionnel par mois et pour l'année ;
3. modifier le plan pour l'avenir sans écraser les montants historiques ;
4. ajouter un extra ponctuel et ajuster, réaffecter ou suspendre des postes variables ;
5. importer les opérations réelles et les rattacher aux postes budgétaires ;
6. comparer prévu et réalisé, par poste et par mois, et repérer les écarts ;
7. consulter à la fois le réalisé des mois passés et le budget révisé des mois à venir ;
8. suivre une enveloppe d'épargne sans la confondre avec une dépense ni compter deux fois les mêmes montants.

## 9. Points de décision à préciser avant d'imposer une règle

Les choix suivants ne doivent pas être déduits arbitrairement de l'ancien classeur :

- comment représenter précisément un extra (revenu exceptionnel, apport au compte, ou autre ajustement) et une réaffectation entre postes ;
- si l'application conserve une seule version du plan initial ou un historique détaillé de toutes ses révisions ;
- si les enveloppes d'épargne sont suivies comme des montants planifiés, des soldes réellement détenus, ou les deux ;
- quelles catégories et sous-catégories doivent être proposées au démarrage ;
- quelles règles de rapprochement appliquer aux opérations bancaires lorsque leur libellé ne permet pas une affectation certaine.