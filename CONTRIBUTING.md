# Contribuer à HighwayRun

Merci de vous intéresser à HighwayRun ! Ce document résume comment proposer une contribution.

## Avant de commencer

Pour une nouvelle fonctionnalité ou un changement important, ouvrez d'abord une issue ou une
[discussion](https://github.com/ymauray/highwayrun/discussions) pour en parler avec le mainteneur.

## Environnement de développement

Il faut le SDK .NET 10. raylib est fourni par le paquet NuGet Raylib-cs : rien d'autre à installer.

```sh
dotnet build                       # compiler
dotnet run                         # jouer
dotnet run -- --shots shots/       # partie en pilote automatique, avec captures d'écran dans shots/
```

Le projet n'a pas de tests automatisés : vérifiez le rendu avec le mode `--shots`, et que
`dotnet publish -c Release -r <rid>` produit toujours un binaire unique.

## Proposer une modification

1. Créez une branche dédiée depuis `main` (ex. `fix/...`, `feature/...`).
2. Faites des commits atomiques, avec des messages clairs.
3. Vérifiez que le jeu compile et tourne en local.
4. Ouvrez une pull request vers `main` en remplissant le modèle fourni. La CI doit passer sur les quatre
   plateformes ; les PR sont mergées en squash.

## Conventions

- Commentaires et textes en français, identifiants en anglais.
- Aucun fichier graphique ou sonore : sprites, décors, musiques et bruitages sont générés par le code
  (`src/Art.cs`, `src/Backdrops.cs`, `src/Audio.cs`). Seule exception, la police embarquée, sous licence OFL.
