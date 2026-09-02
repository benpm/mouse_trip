using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.ComponentModel;

/**
 * GameplayTile is a custom tile class that extends Unity's Tile class.
 * It allows for additional properties to define the behavior of tiles in a game.
 * 
 * Properties:
 * - category: A string representing the category of the tile (e.g., "Ground", "Water").
 * - solid: A boolean indicating if the tile is solid (player cannot pass through).
 * - damagesPlayer: A boolean indicating if the tile damages the player on contact.
 * - damage: An integer representing the amount of damage dealt to the player.
 * - collectible: A boolean indicating if the tile can be collected by the player.
 * - climbable: A boolean indicating if the tile can be climbed by the player.
 * - oneWay: A boolean indicating if the tile allows one-way movement (e.g., platforms).
 * - bouncy: A boolean indicating if the tile causes a bounce effect on contact.
 * - breakable: A boolean indicating if the tile can be broken by the player or other means.
 * - interactive: A boolean indicating if the tile can interact with other game elements.
 * - liquid: A boolean indicating if the tile behaves like a liquid (e.g., water, lava).
 */
[CreateAssetMenu(fileName = "GameplayTile", menuName = "Kenney/Gameplay Tile")]
public class GameplayTile : Tile
{
    public string category;
    public bool solid;
    public bool damagesPlayer;
    public int damage;
    public bool collectible;
    public bool climbable;
    public bool oneWay;
    public bool bouncy;
    public bool breakable;
    public bool interactive;
    public bool liquid;

    void OnValidate()
    {
        // Ensure that if the tile is solid, it has a collider type set to Grid
        if (solid && colliderType == ColliderType.None)
        {
            colliderType = ColliderType.Grid;
        }

        ///TODO: Add additional validation logic as needed for other properties
    }
}
