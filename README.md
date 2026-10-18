# VoixDuQuartier : frontend .NET MAUI

Application mobile de **signalement collectif des problèmes locaux** (voirie, éclairage, déchets) : on signale avec une photo, les voisins soutiennent, on suit le traitement jusqu'à la résolution.

Cette première étape contient **tout le frontend** (10 écrans fidèles aux maquettes Figma, plus les états erreur, vide, chargement, désactivé). Les données sont pour l'instant **fictives et en mémoire** ; le backend viendra ensuite sans changer les écrans.

---

## 1. Configurer (une seule fois)

**a) Installer .NET et vérifier la version.** Dans un terminal :

```
dotnet --version
```

Ouvrez `VoixDuQuartier.csproj` et regardez la ligne tout en haut :

| `dotnet --version` affiche | Valeur de `<DotNetTfm>` |
|---|---|
| 10.x | `net10.0` (valeur actuelle) |
| 9.x | `net9.0` |
| 8.x | `net8.0` |

Si votre version n'est pas la 10, **changez uniquement cette ligne**.

**b) Installer la charge de travail MAUI :**

```
dotnet workload install maui
```

**c) Pour tester sur Android :** installer Android Studio (ou l'extension **.NET MAUI** de VS Code, qui guide l'installation du SDK Android), puis créer un émulateur (Device Manager) ou brancher un téléphone en mode développeur.

> Sur Windows, vous pouvez aussi tester directement **sans téléphone** : l'application s'ouvre dans une fenêtre en forme de téléphone.

## 2. Exécuter

Dans le dossier du projet (celui qui contient `VoixDuQuartier.csproj`) :

| Cible | Commande |
|---|---|
| Fenêtre Windows | `dotnet build -t:Run -f net10.0-windows10.0.19041.0` |
| Émulateur ou téléphone Android | `dotnet build -t:Run -f net10.0-android` |

(Remplacez `net10.0` par votre version si besoin.) Avec VS Code, vous pouvez aussi choisir la cible dans la barre du bas puis appuyer sur **F5**.

**Compte de démonstration :** n'importe quelle adresse e-mail valide et n'importe quel mot de passe fonctionnent (aucune vérification tant qu'il n'y a pas de backend). Pour créer un compte, le mot de passe doit faire au moins 8 caractères.

## 3. Compiler une version finale

```
dotnet run -f net10.0-windows10.0.19041.0
```

Le fichier `.apk` se trouve dans `bin/Release/net10.0-android/publish/`. Il peut être copié sur un téléphone Android et installé directement.
Pour publier sur le Play Store, il faudra une **clé de signature personnelle** : ne la mettez jamais sur GitHub (le `.gitignore` bloque déjà `*.keystore`).

## 4. Tester

```
dotnet test tests/VoixDuQuartier.Tests
```

Ces tests vérifient les règles de validation (e-mail, mot de passe, description de 10 caractères minimum) et la construction du titre d'un signalement.

---

## Comment c'est organisé (architecture MVVM)

Imaginez un restaurant : la **Vue** est la salle (ce que voit le client), le **ViewModel** est le serveur (il prend les ordres et rapporte les réponses), les **Services** sont la cuisine (ils préparent les données). La salle ne parle jamais directement à la cuisine.

| Dossier | Rôle |
|---|---|
| `Views/` | Les 10 pages XAML (une par écran des maquettes) |
| `ViewModels/` | La logique de chaque page : commandes, validation, états (chargement, vide, erreur) |
| `Models/` | Les données : `Report`, `AppNotification`, statuts et catégories |
| `Services/` | `IDataService` (signalements, notifications) et `ISessionService` (connexion) : aujourd'hui en mémoire, demain reliés à l'API |
| `Controls/` | Composants réutilisables : champ de formulaire, pastille de filtre, pastille de statut, logo |
| `Resources/Styles/` | Couleurs et styles (palette des maquettes, thème sombre et clair) |
| `Helpers/` | Règles de validation et titre (sans dépendance MAUI, donc testables) |
| `Platforms/` | Fichiers propres à Android, Windows, iOS (permissions caméra et position) |
| `tests/` | Tests unitaires |

### Écrans des maquettes ↔ pages

| Maquette | Page | Remarques |
|---|---|---|
| 01 Démarrage | `SplashPage` | 1,8 s puis connexion (ou fil si déjà connecté) |
| 02 Connexion (+ erreur) | `LoginPage` | Champ rouge et message dès qu'une règle n'est pas respectée |
| 03 Inscription | `RegisterPage` | Nom, e-mail, quartier, mot de passe, confirmation |
| 04 Fil (+ chargement) | `FeedPage` | Filtres par catégorie et par statut, squelettes gris pendant le chargement, état vide |
| 05 Nouveau signalement (+ erreur) | `NewReportPage` | Photo (caméra ou galerie), catégorie, position GPS, description 10 à 300 caractères ; bouton désactivé tant que c'est invalide |
| 06 Confirmation | `ConfirmationPage` | Bouton « Voir mon signalement » |
| 07 Détail (+ soutenu) | `DetailPage` | Soutenir / Soutenu, frise du statut, localisation |
| 08 Carte du quartier | `MapPage` | Épingles par catégorie, filtres, légende |
| 09 Notifications (+ vide) | `NotificationsPage` | « Tout marquer comme lu », état vide |
| 10 Profil | `ProfilePage` | Mes signalements (en cours / résolus), mode sombre, notifications, langue, déconnexion |

La barre du bas (Accueil, Carte, +, Alertes, Profil) est gérée par `AppShell.xaml`.

## Écoconception (reprise de votre page de justification)

Mode sombre par défaut, **aucune image bitmap** (logo et icône en vectoriel, emojis pour les pictogrammes), une seule famille de police (Inter, 2 graisses), pas d'animation, parcours court. Le choix du mode sombre est mémorisé.

## Sécurité et maintenance : ce qui est déjà fait

- Validation côté application de tous les formulaires (à répéter côté serveur plus tard).
- Aucun mot de passe n'est stocké ni écrit dans les journaux ; la session de démonstration est clairement marquée comme telle dans `SessionService.cs`.
- Permissions demandées **au moment de l'usage** (caméra, position) avec un message expliquant pourquoi ; l'application reste installable sans caméra ni GPS.
- Sauvegardes cloud Android désactivées (`allowBackup=false`).
- Injection de dépendances : on peut remplacer un service sans toucher aux pages.

## Limites actuelles (frontend seul)

- **Pas de vraie carte** : un plan simplifié avec épingles (le vrai plan viendra avec le backend et `Microsoft.Maui.Controls.Maps`).
- **Langue** : le choix Français / English est mémorisé, mais les textes ne sont pas encore traduits (fichiers `.resx` à venir).
- **Photos** conservées localement sur l'appareil uniquement.
- Ce code a été vérifié par analyse statique (syntaxe C#, XAML, liaisons), mais **n'a pas encore été compilé** dans l'environnement où il a été écrit : lancez `dotnet build` en premier et signalez toute erreur pour qu'on la corrige.

## Étapes suivantes proposées

1. **Backend** : ASP.NET Core Web API (`/auth`, `/reports`, `/reports/{id}/support`, `/notifications`) avec base SQLite puis PostgreSQL, authentification par jeton JWT, HTTPS obligatoire.
2. **Client** : une classe `ApiDataService` (HttpClient) à la place de `InMemoryDataService`, jeton stocké dans `SecureStorage`.
3. Vraie carte et géolocalisation stockée par signalement, téléversement des photos, notifications push, traduction `.resx`.

---

*Police Inter : licence SIL Open Font License 1.1 (libre d'utilisation).*
