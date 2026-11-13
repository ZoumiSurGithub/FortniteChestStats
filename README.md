# FortniteStats

🇫🇷 Français · [🇬🇧 English](README.en.md)

Application Windows (WPF, .NET 10) qui lit les **rediffusions Fortnite** de votre PC et affiche, après chaque partie,
tout ce que vous avez fouillé (coffres, boîtes de munitions, autres contenants) avec vos stats de partie.
Un **overlay** par-dessus le jeu montre la dernière partie, et un onglet dédié suit la **chasse au trésor**.

> Projet non officiel, sans lien avec Epic Games. Tout est analysé **localement** sur votre PC.

## Fonctionnalités

- **Parties** : total fouillé, coffres / munitions / autres, classement, éliminations, durée, coffres de l'équipe, trésors reçus, historique et cumul de toutes vos parties.
- **Overlay** par-dessus le jeu (transparent aux clics) : `Ctrl+F7` masquer / afficher, `Ctrl+F8` quitter.
- **Chasse au trésor** : coffres fouillés, récompenses avec images, chances par coffre et chance cumulée, récompenses cochables à la main.
- **Mon compte** (facultatif) : stats à vie du compte via l'API publique [fortnite-api.com](https://fortnite-api.com) (clé gratuite, aucun mot de passe Epic).
- Mode jour / nuit, animations, **français / anglais / espagnol**.
- Surveillance automatique : une vérification au lancement, puis seulement la dernière rediffusion.

## Installation

1. Téléchargez `FortniteStats.exe` dans l'onglet [**Releases**](../../releases) (aucune installation, .NET inclus).
2. Dans Fortnite, activez les rediffusions : **Paramètres → Jeu → Replays**.
3. Lancez l'application, cliquez sur **Lancer**, jouez une partie : les stats s'affichent dès qu'elle est terminée.

> Windows SmartScreen peut avertir au premier lancement (exécutable non signé) : « Informations complémentaires » → « Exécuter quand même ».

## Compiler soi-même

Prérequis : Windows 10/11 et le [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bat
CREER_EXE.bat
```

ou

```bat
dotnet publish App\FortniteStats.csproj -c Release -o publish
```

L'exécutable se trouve dans `publish\FortniteStats.exe`.

## Données et vie privée

| Donnée | Où | Envoyée à |
|---|---|---|
| Rediffusions Fortnite | `%LOCALAPPDATA%\FortniteGame\Saved\Demos` (lecture seule) | personne |
| Historique, cumul, réglages | `%APPDATA%\FortniteStats` | personne |
| Images des récompenses | téléchargées une fois depuis GitHub, gardées dans `%APPDATA%\FortniteStats\images` | — |
| Pseudo + clé API (onglet « Mon compte », facultatif) | `settings.json` (en clair) | fortnite-api.com uniquement |

## Limites

- Seules les rediffusions encore présentes sur le disque peuvent être lues (Fortnite supprime les plus anciennes).
- L'API officielle d'Epic ne fournit ni coffres ni trésors : ces données ne viennent que des rediffusions.
- Un trésor n'est détecté que s'il sort d'un coffre doré pendant la partie ; les récompenses obtenues autrement peuvent être cochées à la main.
- Les chances affichées ne tiennent pas compte des « Heures du trésor ».

## Crédits

Le moteur d'analyse est celui de [ChestCounter](https://github.com/LED-N/Fortnite-ChestCounter) par **LED-N** (MIT), basé sur
FortniteReplayDecompressor. Voir [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Licence

[GPL-3.0-or-later](LICENSE) : le moteur inclut OozSharp (GPL-3). Les autres composants tiers (MIT) gardent leur propre licence, voir [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
"# FortniteChestStats" 
