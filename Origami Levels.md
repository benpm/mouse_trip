# Origami Levels

## Cube Levels
Cube puzzles are based on behaviors of opposite. Many puzzles can arise from elements such as backfaces, trigger blocks, nesting, rotation about different axes, and traversing to change basis vecs, allowing player to rotate themselves relative to a region just by traversing the faces in a particular order.

1. opaque faces (level regions have background)
2. backgrounds have holes which are transparent and reveal the opposite 
3. transparent faces - player interacts with and sees the backside of opposite face. An "on" toggle block is "off" when seeing it from behind.
4. Matroishka cubes / nested cubes - smaller cube inside of outer one. Inner cube can be rotated

Another variant - gearbox

## Vanishing Point
Level regions can be projected onto triangular shape or other convex shape, making polyhedra, or combining with rectangular regions to make more complex 3D surfaces. One vertex of a triangle has to be the vanishing point where the region collapses inward. Moving into this collapsed part of a region could do something interesting but it has to be consistent, or at least it has to be communicated effectively if the result of traveling into it is different between levels. Some ideas:

- Death if you fall in? Or when the point is above them, get sucked in?
- Traverses to a spatially disconnected region that could be hidden from view - e.g. a square bipyramid which consists of 4 triangles + 1 square might have walls between the triangles, but the player could travel up into and over the top vertex, traversing to one of the adjacent triangles, thus getting around the walls.
- Infinitely more level appears as you approach it, and below the player the region could become compressed, or the camera could zoom in as the player shrinks, never reaching an end.

## Holes / Topological
Holes provide a unique way to traverse betwen regions. Cube with hole allows player to instantly travel between opposite faces.


# Clones
Clones can appear in several ways. If you die when a clone lives, you then will control that clone instead

## Reflections
Mouse is reflected, player is simultaneously controlling two copies / reflections of the Mouse. 

## Refraction / Scaling
Mouse reflection is through a lense or something, so the player-controlled copy is twice bigger, allowing them to push large buttons, move large crates, but preventing them from entering single-tile sized holes.


# Puzzle Elements
- One-way platforms that can face any direction, so can be walls, floors, or ceilings. Useful to combo with 
- Teleporter that creates line of sight beams that connect them to each other. If your beam hits two teleporter exits at the same time, it will clone you. You'd have to use that to solve puzzle
- Portable Rooms - objects that contain entire rooms that you can pick up and move around, like in Recursed

# Misc Levels
- Level where each solution exponentially growing the number of cloned rooms in a huge grid
    - Second part should make the levels extend into 3D then 4D as a hyperlattice
- Pipe jungle level - you have to find map fragments that fit together like puzzle pieces?
- Levels like Cube movie where many connected rooms which movie around in a giant grid. You'll figure out how to use landmarks to identify specific rooms. 
- Double helix - two infinite tubes connected by bridges. The bridges are long rectangular prisms.

# Void
Intro to Void - you fall off into the darkness and your clone sticks behind you, like branching paths in time. After free-falling for a while, you eventually slow down and float gently into a room that looks like the accelerator room, but you find that it loops - going left will eventually cause you to apoear on the right. Going up gets you back to the bottom. Upon solving, the camera pans out to reveal you are on a Torus surface. Using a hole, you then appear on the interior of the Torus, by the torus everting. 