# GJG-GoodLevelUp-Task

This project was created for the GJG Summer Internship Program.

## How to Start?
- Start the game with PlayScene.
- Game settings can be changed in the Inspector panel from the TileManager object.

## Script Features:
- Tile Types: Defines the assets used in the scene.
- Tile Type Range: Specifies the number of different colors available.
- A, B, and C Conditions: Change the tile texture based on the number of same-colored tiles in a group. When certain conditions are met, the tile texture updates dynamically.
- Rows and Cols: Adjust the tile map size.

## Deadlock Testing
- To test a deadlock situation:
- Set Rows and Cols to the minimum possible value.
- Set Tile Type Range to 6.
- When a deadlock occurs, a warning log will appear in the editor, and the script will automatically change the color of a random tile.

## Deadlock Resolution
- To resolve a deadlock, the script selects random tiles and makes the same color with the neighboring tile.
