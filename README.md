# ATLAS — MediaLib

ATLAS est une bibliothèque numérique de démonstration complète : une API ASP.NET Core 10, une bibliothèque de contrats partagée et une application Blazor Web App Interactive Server. Le catalogue, les comptes, les emprunts, les retards, les retours et l'administration sont alimentés par une base SQLite réelle (aucune donnée factice figée dans l'interface).

## 1. Prérequis

- .NET 10 SDK (10.0.100 ou supérieur)
- Visual Studio 2026 (ou VS Code + CLI .NET)
- SQLite (le pilote NuGet `Microsoft.EntityFrameworkCore.Sqlite` suffit ; aucun serveur à installer)
- Windows, macOS ou Linux — les commandes ci-dessous sont données pour Windows / PowerShell

Vérifier l'installation :

```powershell
dotnet --version
dotnet tool list --global | Select-String dotnet-ef
```

## 2. Architecture du projet

```
MediaLib.sln
├── MediaLib.Shared      # DTOs, constants, validateurs DataAnnotations (aucune dépendance EF/API/Web)
├── MediaLib.Api         # ASP.NET Core Web API, EF Core, Identity, JWT, SQLite, OpenAPI + Scalar
└── MediaLib.Web         # Blazor Web App (Interactive Server), HttpClient, design system ATLAS
```

Flux :

1. Le navigateur ouvre `MediaLib.Web` (port 5045).
2. Le circuit Blazor conserve les jetons dans un `AtlasTokenStore` scopé et appelle l'API via `HttpClient`.
3. L'API valide le JWT, applique les policies (`PeutGererCatalogue`, `PeutEmprunter`, `EstAdmin`) et exécute la logique métier via EF Core.
4. `MediaLib.Shared` est référencé par les deux applications : aucune entité EF n'est exposée ni utilisée dans `MediaLib.Web`.

## 3. Création des projets

La solution a été créée avec les modèles .NET 10 :

```powershell
dotnet new sln -n MediaLib
dotnet new webapi -n MediaLib.Api -f net10.0 --use-controllers
dotnet new classlib -n MediaLib.Shared -f net10.0
dotnet new blazor -n MediaLib.Web -f net10.0 --interactivity Server --all-interactive
dotnet sln MediaLib.sln add MediaLib.Api MediaLib.Shared MediaLib.Web
dotnet add MediaLib.Api reference MediaLib.Shared
dotnet add MediaLib.Web reference MediaLib.Shared
```

Les dossiers se trouvent à la racine de la solution, comme attendu par les fichiers de configuration (`MediaLib.Api/appsettings.Development.json`, etc.).

## 4. Installation des packages

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12

dotnet add MediaLib.Api package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.12
dotnet add MediaLib.Api package Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet add MediaLib.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.12
dotnet add MediaLib.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.12
dotnet add MediaLib.Api package Microsoft.AspNetCore.OpenApi --version 10.0.12
dotnet add MediaLib.Api package Scalar.AspNetCore --version 3.1.1
```

`MediaLib.Web` n'a aucun package NuGet supplémentaire : `HttpClient`, `AuthenticationStateProvider` et `AddAuthorizationCore` sont fournis par le framework.

## 5. Commandes de migration

La migration initiale est livrée dans `MediaLib.Api/Data/Migrations/`. Pour la régénérer après une modification du modèle :

```powershell
dotnet ef migrations add InitialCreate `
  --project MediaLib.Api `
  --startup-project MediaLib.Api `
  --output-dir Data/Migrations

dotnet ef database update --project MediaLib.Api --startup-project MediaLib.Api
```

Appliquer les migrations existantes :

```powershell
dotnet ef database update --project MediaLib.Api --startup-project MediaLib.Api
```

## 6. Initialisation de la base

L'API applique les migrations **et** amorce la base au démarrage via `DbInitializer` :

- 10 auteurs, 5 catégories, 30 livres (ISBN valides, stocks variés)
- 3 comptes de démonstration avec rôles Identity
- quelques emprunts actifs, historiques et un retard pour rendre les tableaux de bord parlants

Aucune commande manuelle n'est nécessaire. Pour repartir d'une base vierge :

```powershell
Remove-Item MediaLib.Api/atlas-medialib.db* -Force
dotnet run --project MediaLib.Api/MediaLib.Api.csproj --launch-profile http
```

## 7. Lancement de l'API

```powershell
dotnet build MediaLib.sln
dotnet run --project MediaLib.Api/MediaLib.Api.csproj --launch-profile http
```

L'API écoute sur `http://localhost:5196`. Le premier démarrage crée `MediaLib.Api/atlas-medialib.db`.

## 8. Lancement de Blazor

Dans un second terminal, pendant que l'API tourne :

```powershell
dotnet run --project MediaLib.Web/MediaLib.Web.csproj --launch-profile http
```

L'application web écoute sur `http://localhost:5045` et utilise `ApiBaseUrl` (`MediaLib.Web/appsettings*.json`, valeur par défaut `http://localhost:5196`). Dans Visual Studio 2026, définir plusieurs projets de démarrage : `MediaLib.Api` puis `MediaLib.Web`.

## 9. URLs

| Ressource | URL |
|---|---|
| Application ATLAS | http://localhost:5045 |
| Catalogue public | http://localhost:5045/catalogue |
| Connexion | http://localhost:5045/connexion |
| API (base) | http://localhost:5196 |
| Documentation Scalar / OpenAPI | http://localhost:5196/scalar/v1 |
| Contrat OpenAPI brut | http://localhost:5196/openapi/v1.json |

## 10. Comptes de démonstration

| Rôle | E-mail |
|---|---|
| Administrateur | `atlas.admin@medialib.local` |
| Bibliothécaire | `atlas.librarian@medialib.local` |
| Membre | `atlas.member@medialib.local` |

Ces comptes existent uniquement dans l'amorçage de développement. Un compte supplémentaire créé via `/api/auth/inscription` reçoit le rôle `Membre`.

## 11. Mots de passe

Tous les comptes de démonstration utilisent :

```
Atlas@2026
```

Le mot de passe est stocké sous forme de hash Identity (PBKDF2). Les validateurs partagés imposent au moins 8 caractères, une majuscule, une minuscule, un chiffre et un caractère spécial. La clé JWT de développement est volontairement locale et doit être remplacée en production.

## 12. Comment tester chaque rôle

### Visiteur anonyme

- Parcourir `/catalogue`, filtrer par collection/auteur/disponibilité, trier, paginer.
- Ouvrir la fiche d'un livre (`/livres/{id}`).
- Tenter `/membre`, `/bibliothecaire` ou `/admin/utilisateurs` : la page est protégée et renvoie vers `/connexion` (ou affiche « Accès protégé » dans le circuit Blazor).

### Membre

1. Se connecter avec `atlas.member@medialib.local`.
2. Le tableau de bord `/membre` affiche prêts actifs, retards, échéance et historique réels.
3. Emprunter un livre depuis sa fiche ; la limite de 5 prêts actifs est appliquée côté API.
4. Consulter `/membre/emprunts`, `/membre/historique`, modifier son profil et son mot de passe dans `/membre/profil`.

### Bibliothécaire

1. Se connecter avec `atlas.librarian@medialib.local` → redirection vers `/bibliothecaire`.
2. `/bibliothecaire/livres` : créer, modifier, supprimer un livre, ajuster le stock (un livre ayant un historique de prêt ne peut pas être supprimé).
3. `/bibliothecaire/auteurs` et `/bibliothecaire/categories` : CRUD ; la suppression est refusée si des livres sont associés.
4. `/bibliothecaire/emprunts` et `/bibliothecaire/emprunts/en-retard` : recherche, pagination, filtre des retards.
5. `/bibliothecaire/retours` : enregistrer un retour ; le stock est restauré une seule fois.

### Administrateur

1. Se connecter avec `atlas.admin@medialib.local`.
2. `/admin/utilisateurs` : recherche par nom/e-mail, filtre par rôle, pagination, lien vers le détail.
3. Détail d'un utilisateur : identité, rôles, historique des emprunts, modification du nom.
4. `/admin/roles` : attribuer/retirer `Membre`, `Bibliothecaire`, `Admin` (son propre compte et le dernier admin sont protégés).
5. `/admin/activation` : activer/désactiver un compte (ses sessions/refresh tokens sont révoqués ; son propre compte est protégé).

## 13. Documentation des endpoints API

Tous les endpoints sont documentés avec résumés, paramètres et codes de réponse dans Scalar (`/scalar/v1`).

### Authentification — `/api/auth`

| Méthode | Route | Accès | Description |
|---|---|---|---|
| POST | `/inscription` | anonyme + rate limit | Crée un compte `Membre` |
| POST | `/connexion` | anonyme + rate limit | Retourne `{ accessToken, refreshToken, utilisateur }` |
| POST | `/refresh` | anonyme + rate limit | Fait tourner la paire de jetons (détection de réutilisation) |
| POST | `/deconnexion` | Bearer | Révoque le refresh token |
| GET | `/moi` | Bearer | Profil du compte courant |
| PUT | `/profil` | Bearer | Modifie prénom/nom |
| PUT | `/mot-de-passe` | Bearer | Change le mot de passe |

Pour tester dans Scalar : appeler `/connexion`, copier `accessToken`, cliquer sur **Authorize** et coller `Bearer <token>`.

### Catalogue

| Méthode | Route | Accès |
|---|---|---|
| GET | `/api/livres` | anonyme — `recherche`, `categorieId`, `auteurId`, `disponibleUniquement`, `tri`, `page`, `taillePage` |
| GET | `/api/livres/{id}` | anonyme |
| POST / PUT / DELETE | `/api/livres[/…]` | `PeutGererCatalogue` |
| GET | `/api/auteurs`, `/api/auteurs/{id}` | anonyme |
| POST / PUT / DELETE | `/api/auteurs[/…]` | `PeutGererCatalogue` |
| GET | `/api/categories`, `/api/categories/{id}` | anonyme |
| POST / PUT / DELETE | `/api/categories[/…]` | `PeutGererCatalogue` |

### Emprunts — `/api/emprunts`

| Méthode | Route | Accès | Description |
|---|---|---|---|
| POST | `/` | `PeutEmprunter` | Emprunter `{ livreId }` (max 5 actifs, stock > 0) |
| GET | `/moi` | `PeutEmprunter` | Prêts actifs + historique du compte courant |
| GET | `/` | `PeutGererCatalogue` | Tous les prêts, filtres `actif`, `enRetard`, `recherche`, pagination |
| POST | `/{id}/retour` | `PeutGererCatalogue` | Enregistrer un retour et restaurer le stock |

### Administration et tableaux de bord

| Méthode | Route | Accès |
|---|---|---|
| GET | `/api/admin/utilisateurs` | `EstAdmin` |
| GET | `/api/admin/utilisateurs/{id}` | `EstAdmin` |
| PUT | `/api/admin/utilisateurs/{id}` | `EstAdmin` |
| PUT | `/api/admin/utilisateurs/{id}/roles` | `EstAdmin` |
| PUT | `/api/admin/utilisateurs/{id}/actif` | `EstAdmin` |
| GET | `/api/dashboard/membre` | `PeutEmprunter` |
| GET | `/api/dashboard/bibliothecaire` | `PeutGererCatalogue` |

Les erreurs métier utilisent une enveloppe JSON cohérente :

```json
{ "status": 409, "code": "LIMITE_EMPRUNTS_ATTEINTE", "message": "...", "traceId": "..." }
```

## 14. Décisions de sécurité

- **Identity + rôles** : `Membre`, `Bibliothecaire`, `Admin` ; policies mises en miroir dans l'API et dans Blazor.
- **JWT court (15 min) + refresh token rotatif (7 jours)** : les jetons de refresh sont hachés en base ; une réutilisation révoque la famille de jetons.
- **Rate limiting** sur `/inscription`, `/connexion`, `/refresh` (fixe par IP, en mémoire) et verrouillage du compte après 5 échecs.
- **Propriété des données** : `GET /emprunts/moi` et les tableaux de bord dérivent l'identité du JWT, jamais d'un identifiant fourni par le client.
- **Validation partagée** : `DataAnnotations` côté Web **et** côté API ; ISBN avec checksum réel ISBN-10/13, unicité en base.
- **Blazor Interactive Server** : les jetons d'accès ne sont pas envoyés au navigateur pour les appels API ; ils vivent dans un service `Scoped` côté serveur. Une copie dans `localStorage` (avec un simple cookie de durabilité de route `atlas_auth` + `atlas_roles`, non faisant foi) permet de restaurer la session après rechargement. **Toute décision d'accès aux données est reprise et imposée par l'API.**

## 15. Limites connues

- `localStorage` reste exposé au XSS : un script injecté dans la page pourrait lire les jetons. Le cookie `atlas_roles` est une indication de routage uniquement ; il n'est pas signé et ne doit jamais être utilisé pour autoriser une donnée. Une application de production devrait préférer des cookies `HttpOnly`/`Secure` avec protection CSRF, ou un BFF.
- Le rate limiting et le verrouillage des comptes sont en mémoire : ils ne sont pas partagés entre plusieurs instances (acceptable pour une démonstration mono-instance).
- La clé JWT fournie est une clé de développement. La passer en variable d'environnement / secret avant déploiement.
- Le profil `http` n'utilise pas HTTPS ; utiliser le profil `https` ou un reverse proxy en production.
- SQLite et l'amorçage automatique ciblent la démonstration ; un déploiement réel nécessiterait PostgreSQL/SQL Server et une migration contrôlée.
- Il n'existe pas (encore) de projet de tests automatisés livré dans la solution. La vérification a été effectuée par scripts HTTP et par navigateur headless (voir le rapport final fourni avec la livraison) ; la couverture UI porte sur les flux de connexion et le rendu des pages principales, pas sur chaque interaction secondaire.
- Pas de réinitialisation de mot de passe ni de vérification d'e-mail (hors périmètre de l'exercice).
