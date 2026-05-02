# Oracle — Tactical RPG isométrique (Unity)

Jeu tactique au tour par tour en vue isométrique 2.5D (deck de sorts, passifs). Ce dépôt est **la racine du projet Unity** : ouvre ce dossier directement dans l’éditeur.

## Démarrage rapide

| Élément | Détail |
|--------|--------|
| **Unity** | **2022.3.62f3** LTS (voir `ProjectSettings/ProjectVersion.txt`) |
| **Pipeline** | URP |
| **Scène principale** | `Assets/Monjeu.unity` |

Phase 3 — Réseau → ~65% Ce qu'il reste vraiment :

Sync du RNG de sélection des passifs entre les deux clients (risque de divergence actuellement)
Gestion déconnexion propre (défaite auto + retour au hub)
Edge cases MasterClient (timer réseau, cast simultané)
Phase 4 — DA → ~35%

Animations personnage multi-directions (Idle, Walk, Hit, Death)
VFX par sort
Sons (musiques Suno + impacts/UI Freesound)
Conformité pixel art (PPU constant, filtre Point)
Phase 5 — Polish MVP → ~55%

Écran Victoire/Défaite
Sons de base
Tests bout en bout 1v1 (idéalement avec ParrelSync)
Décision design à trancher : ordre des tours aléatoire ou par initiative (le code dit initiative décroissante, le GDD dit aléatoire)
Mise à jour README/ROADMAP (toujours en retard sur le code)
Phase 6 — Post-MVP → 0% (2v2/3v3, MMR, historique, cosmétiques — ne pas toucher avant que le 1v1 soit stable)

---

## Doc complète du projet

Le plan détaillé, les phases GDD et l’**audit à jour** (avril 2026) sont dans **`ROADMAP_ORACLE.txt`** à la racine de ce dossier.

---

## Dépôt

[https://github.com/kyaminq-ui/oracle](https://github.com/kyaminq-ui/oracle) — branche **`main`**.

Bonne session.
