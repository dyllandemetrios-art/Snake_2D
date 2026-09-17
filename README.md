# Eden Snake 🐍🍎

*Snake arcade narratif développé sous Unity*

![Status](https://img.shields.io/badge/status-released-brightgreen)
![Engine](https://img.shields.io/badge/engine-Unity-black)
![Platform](https://img.shields.io/badge/platform-HTML5%20%7C%20Windows-blue)

🎮 **[Jouer sur itch.io](https://demetrios-games.itch.io/eden-snake)**

## À propos

Eden Snake revisite le classique snake sous forme d'immersion narrative arcade. Choisissez votre pomme — Adam, Eve ou Lilith — chacune ouvrant un chemin différent. Essayez-les toutes pour découvrir les différentes fins du jeu.

## Contrôles

| Action | Touche |
|---|---|
| Déplacement | Flèches |
| Pause | P |
| Valider | Entrée / Souris |

## Ce que ce projet m'a appris

- **Programmation orientée objet** : architecture par héritage pour les types de pommes et les comportements du serpent
- **Gestion de scènes** : transitions fluides entre menu, gameplay et séquences narratives
- **Service Locator** : managers découplés pour l'audio, l'UI, le score et le suivi de parcours
- **Gameplay classique revisité** : mécaniques de snake enrichies de buffs progressifs, de choix philosophiques et d'un boss caché
- **Intégration narrative** : système de dialogue Ink tissé dans le gameplay arcade
- **State machine** : gestion des états de jeu (pause, renaissance, victoire) sans casser le flow

## Points techniques

- Spawn dynamique des pommes selon les choix du joueur
- Système de meilleur score persistant
- Feedback visuel temps réel (couleurs des murs, screen shake, particules)
- **Boss à comportement autonome** (cible les pommes, vole des segments, combat par collision)

## Tech stack

- **Moteur** : Unity
- **Langage** : C#
- **Dialogue** : Ink
- **Plateformes** : HTML5, Windows

## Statut

✅ Sorti

## Auteur

**Dyllan** — [demetrios-games sur itch.io](https://demetrios-games.itch.io)
