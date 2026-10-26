# Meilleur HUD hydratation & satiété

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

Mod HUD pour **Escape from Duckov** : affiche la quantité manquante et la valeur **actuelle / max** à côté des anneaux d'hydratation et de satiété.

> **v2.1.2** — un seul DLL, **10 langues du jeu**, **raccourci F10 modifiable**, **aucun prérequis**.

## Points clés

- **Raccourci modifiable** — le panneau s'ouvre avec F10 par défaut. En cas de conflit, change-le dans **Paramètres → Valeurs du HUD**.
- **10 langues du jeu** — suit automatiquement la langue du jeu : anglais, 简体中文, 繁體中文, 日本語, 한국어, français, allemand, espagnol, portugais, russe.
- **Aucun prérequis** — pas de ModConfig / ModSetting / HarmonyLib.
- **Panneau F10 en jeu** — met le jeu en pause, souris utilisable, réglage en direct de la position / taille / format, sauvegarde automatique.
- Bouton de réinitialisation par valeur, alerte valeur basse, pourcentage / décimales, onglet natif dans les options.

## Installation

1. S'abonner sur le [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771).
2. Activer le mod dans la liste **Mods** du jeu.
3. Charger une sauvegarde. Appuyer sur `F10` pour ouvrir le panneau.

## Compilation

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

Le projet référence les DLL managées du jeu sans les inclure. Voir `docs/` pour l'API, les tests et les notes de version.

## Liens

- Steam Workshop : <https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- Notes de version : [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- Rapport de test : [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## Auteur

— Dorei Hime ♥
