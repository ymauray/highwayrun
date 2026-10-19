# AGENTS.md

HighwayRun recrée un jeu de course arcade des années 80 (voir `CONCEPT.md`) en .NET 10 avec Raylib-cs.
Il compte 15 étapes en arbre, avec une fourche à la fin de chacune, et 5 arrivées (A à E).

## Commandes

```sh
dotnet build                                  # compiler
dotnet run                                    # jouer
dotnet run -- --shots shots/                  # partie en pilote automatique + captures PNG dans shots/
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64   # binaire unique autonome (osx-x64, linux-x64, win-x64)
```

Il n'y a pas de tests automatisés. Pour vérifier un changement visuel ou de gameplay, utiliser le mode
`--shots` et regarder les captures. Ce mode n'enregistre ni le meilleur score ni les réglages.

## Architecture (`src/`)

- `Cfg.cs` : constantes. La résolution logique est de 480×270, avec `ArtScale = 2`. Les distances
  sont en unités monde (segments de 200 unités).
- `Renderer.cs` : projection pseudo-3D des segments, peints de l'arrière vers l'avant, puis la parallaxe.
- `Track.cs` : segments, génération des étapes et des fourches (`Track.Choose` rebase les coordonnées au choix).
- `Themes.cs` : palettes et caractère des 15 étapes.
- `Canvas.cs`, `Art.cs`, `Backdrops.cs` : sprites et décors dessinés sur CPU. Les primitives prennent des
  coordonnées logiques, multipliées par `ArtScale`. Chaque texture a son masque, qui sert au brouillard.
- `Audio.cs` : synthèse temps réel du moteur, des bruitages et de trois morceaux. Les volumes de la
  musique et des bruitages sont séparés.
- `Game.cs` (écrans, physique, trafic), `Hud.cs`, `Endings.cs`, `Input.cs`, `Fonts.cs`, `Settings.cs`.

## Conventions

- Aucun fichier graphique ou sonore dans le dépôt : tout est généré par le code. Seule exception, la police
  Russo One (OFL), dans `assets/fonts/`. Elle est embarquée comme ressource.
- Commentaires et textes en français, identifiants en anglais. Indentation de 4 espaces.
- Les compteurs du HUD utilisent des chiffres à chasse fixe (`Hud.Mono*`), pour qu'ils ne « vibrent » pas.
- Les réglages de publication autonome (`SelfContained`, `PublishSingleFile`) ne s'appliquent que si un
  RuntimeIdentifier est donné.
- Toute modification passe par une PR vers `main`, mergée en squash. Les quatre checks `Build (...)` doivent
  passer.

## Releases

Pousser un tag `vX.Y.Z` sur `main` : le workflow `build.yml` crée la release GitHub avec les quatre archives.
Les pushes et les PR ne produisent que des artefacts de run.

## Données du joueur

Le record (`best.txt`) et les volumes (`settings.txt`) sont stockés dans
`Environment.SpecialFolder.ApplicationData/HighwayRun/`, soit `~/Library/Application Support/HighwayRun/`
sur macOS. Ce sont les vraies données du joueur : ne jamais les supprimer ni les réécrire pendant un test.
Le mode `--shots` ne doit écrire ni l'un ni l'autre.

## Pièges connus

- `Renderer.DrawSegment` dessine la bande d'herbe aux bornes flottantes exactes. Si on les arrondit, au loin,
  l'herbe efface l'asphalte des segments suivants.
- Une fois la fourche choisie, `Segment.Nearest` ne renvoie plus que la route choisie. Le maintien latéral et
  le pilote automatique s'appuient dessus, pour qu'on ne roule pas sur la branche abandonnée.
- La position est bornée à `track.Length - Seg * 30` : avec une grande `DrawDistance`, une borne plus courte
  bloquerait la voiture avant la fourche.
- Les rectangles source des sprites prennent la taille réelle de la texture (`ArtScale` fois la taille logique),
  jamais la taille logique.
