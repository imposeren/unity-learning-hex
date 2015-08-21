HexGrid by Daniel Carrier

Scene.unity is designed to be playable (if not particularly fun) out of the box. It's possible to make something prettier without delving into the code, but I would not recommend it.

How to play:

Each player has two units, a sphere and a cube. The sphere has long range, low speed, low offense, and high HP. The cube has short range, high speed, high offense, and low HP. There is also a grey blob in the middle of the field. This is an obstacle, and cannot be moved through, but has no other effect on the game.

First, you select one player or two player. If you choose one player, a simple AI takes the place of your opponent.

The game starts on Player 1's turn (the red player). Each turn, you can make each of your units move and attack. They can't move after they can attack, but beyond that it's flexible. You can move on unit, then make another unit move and attack, and then make your first unit attack. You can end your turn early by clicking "End Turn", and it will end automatically if there's nothing left you can do. The game ends when one player runs out of units.

The controls would take a while to explain, but they're meant to be intuitive. For the most part, just click on units you want to move, where you want to move them, and who to attack.

Changing the initial position of the units:

Just move them around on the game board. Their initial position isn't stored anywhere. They just move to the center of the nearest hex at the start of the game. To illustrate this, the cubes are actually not quite in the right place initially, but they're fixed when the game starts. The same applies to the grey blob. Just be careful to move the parent empty, not the mesh.

Adding more units:

UnitCube, UnitSphere, UnitCube2, and UnitSphere2 can be copied and pasted and will continue to work appropriately. Just make sure that they stay children of Units.

Adding more unit types:

The easy way is to just copy and paste one of the existing units and altering it. Each unit is made up of a parent, which is an empty running Unit.cs, and a child, which is a model with a collider. The collider is what lets you click on the unit, so if you want to change the model, be sure to give it an appropriate collider. To alter the attributes of the unit, just change the variables in the Unit.cs script. Their use is as follows:

PLAYER:		0 for player 1, and 1 for player 2.
MAX_HP:		Starting number of hitpoints.
STRENGTH:	Average damage while attacking.
VARIATION:	The smaller this is, the less the damage will very. The damage is actually calculated using a negative binomial distribution and then adding one, which means that the minimum damage is one and there's no upper limit regardless of what value you set for VARIATION. There's also a significant rounding error, so take it with a grain of salt.
SPEED:		How many hexes the unit can move in one turn.
RANGE:		How far the unit can attack. One means that they can only hit adjacent hexes, two means they can hit hexes adjacent to those, etc.



If you've toyed around enough and you actually want to modify the code, here's an overview. For more details, there's comments in the code itself.

HexGrid.cs is the main script to control the game. It is run by the playing field. It should be modified to change the controls of the game.

Instances of HexPosition.cs are used as positions on the grid and have various methods to make dealing with these positions easier. While it does have an underlying two-dimensional coordinate system, using it directly is not recommended. There are also static methods and variables centered around the playing field. They let you do things like assign variables to positions. This class is the most complete, and is intended to be modified little if at all. It is also the most heavily documented. You may need to read through that class to see what the public methods are, but there is no need to read the code itself. The documentation is in the code, though it's written in a way that lets MonoDevelop show it to you when you use the methods.

Unit.cs is a script that must be attached to units. All of the GameObjects running this script must be parented to a GameObject held by HexGrid.cs. It should be modified if you want to change how units in general work. If you want particular units that work differently, then it is recommended to make a new class that inherits from Unit.cs.

AStar.cs is a simple implementation of A*, a path-finding algorithm. It should be modified if you want to change which cells can be moved through, make some cells take longer to cross, etc. However, it is not currently set up to easily be modified, so if you need to do that, sorry. Currently, units can pass through any cell except ones containing units or marked "Obstacle". If you want to be specific about your obstacles, you can assign a value to the variable Obstacle at that HexPosition and it will work just as well.

NegativeBinomialDistribution.cs is a random-number generator that generates numbers in a negative binomial distribution. I made it because I don't see any reason to stick with uniform distributions for damage and such in games. Negative binomial distribution is a discrete, positive, infinitely divisible distribution and I think that makes it perfect for games with discrete HP, although it would be better if r didn't have to be an integer. If you want to use floats instead of integers for HP, I suggest gamma distribution. If you feel the need to use a different distribution, replacing this class entirely would probably be better than modifying it.

AI.cs controls the AI. It's currently built to start attacking with its strongest unit, then move to weaker units, and to chase and attack enemies in order of their strength-to-HP ratio.