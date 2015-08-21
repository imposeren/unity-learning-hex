﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp;

/************************************************************************************************************\
 * HexGrid.cs by Daniel Carrier																				*
 * 																											*
 * This program gives coordinates to positions on a hex grid, checks which cell your cursor is on, and      *
 * outlines that cell. It uses basic mathematical operations on the position of the cursor to find the cell *
 * rather than checking each hexagon.                                                                       *
 *                                                                                                          *
 * I have also included a function to calculate the distance between two hexes.								*
 *																											*
\************************************************************************************************************/

public class HexGrid : MonoBehaviour {

	public GameObject marker;
	public GameObject unitsRoot;
	public GameObject obstacles;
	private List<Unit> units = new List<Unit>();		//I should make this thread safe.
	//public GameObject[] players;
	
	private bool waiting = false;
	//private int timeout = 0;		//waiting is still false as this counts down.
	//private const int MAX_TIME = 1000;
	
	private enum Turn {SELECT, MOVE, ATTACK};
	private Turn turn = Turn.SELECT;
	public int PLAYERS = 2;
	private int player = 0;
	private int updating = 0;
	private HexPosition mouse = null;
	private HexPosition selection = null;
	private HexPosition[] path = null;
	private AI ai;
	bool gameOver = false;
	bool modeSelected = false;
	bool computerPlayer;

	public void wait() {
		waiting = true;
	}

	//SendMessage must call this on the completion of an action.
	public void actionComplete() {
		waiting = false;
	}
	
	//Is this thread safe? I don't know how thread safety works.
	void AddUnit (Unit unit) {
		while(updating > 0) {
			//do nothing.
		}
		++updating;
		units.Add(unit);
		--updating;
		unit.Coordinates = new HexPosition (unit.transform.position);
	}

	void SpawnUnit (UnitTypes unitType, int player) {
		GameObject unit;
		while(updating > 0) {
			//do nothing.
		}

		if (unitType == UnitTypes.box) {
			++updating;
			unit = new GameObject("Unit Cube");
			var unitBehaveior = unit.AddComponent<Unit>();
			unitBehaveior.PLAYER = player;
			unitBehaveior.MAX_HP = 20;
			unitBehaveior.STRENGTH = 10;
			unitBehaveior.VARIATION = 5;
			unitBehaveior.SPEED = 5;
			unitBehaveior.RANGE = 1;
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			var cubeRenderer = cube.GetComponent<MeshRenderer>();
			if (player == 0) {
				cubeRenderer.material.color = Color.red;
			} else {
				cubeRenderer.material.color = Color.blue;
			}

			cube.transform.position = new Vector3(0, 1, 0);
			cube.transform.SetParent(unit.transform);
			unit.transform.SetParent(unitsRoot.transform);
			--updating;
			unit.SendMessage("SetGrid", this);

		} else if (unitType == UnitTypes.sphere) {
			++updating;
			Debug.Log ("Created sphere");
			--updating;
		} else {
			Debug.Log ("Panic");
			throw new UnityException();
		}
	}
	
	public void remove (Unit unit) {
		units.Remove (unit);
	}
	
	//Returns true if there are any selectable units.
	private bool selectSelectable () {
		bool nonempty = false;
		foreach (Unit otherUnit in units) {
			if (otherUnit.PLAYER == player && otherUnit.Status != Unit.State.WAIT) {
				otherUnit.Coordinates.select ("Selectable");
				nonempty = true;
			}
		}
		return nonempty;
	}
	
	//TODO: Move to Unit.cs
	private bool isAttackable (Unit attacker, Unit attacked, HexPosition coordinates) {
		return attacked.PLAYER != player && coordinates.dist (attacked.Coordinates) <= attacker.RANGE;
	}
	private bool isAttackable (Unit attacker, Unit attacked) {
		return isAttackable (attacker, attacked, attacker.Coordinates);
	}
	
	//Returns true if there's at least one attackable unit.
	private bool selectAttackable (Unit attacker, HexPosition coordinates) {
		bool nonempty = false;
		foreach (Unit otherUnit in units) {
			if (isAttackable (attacker, otherUnit, coordinates)) {
				otherUnit.Coordinates.select ("Attack");
				nonempty = true;
			}
		}
		return nonempty;
	}
	
	//Returns true if there's at least one attackable unit.
	private bool selectAttackable (Unit attacker) {
		return selectAttackable (attacker, attacker.Coordinates);
	}

	void Start () {
		unitsRoot.BroadcastMessage ("SetGrid", this);
		//timeout = MAX_TIME;
		HexPosition.setColor("Path", Color.yellow, 1);
		HexPosition.setColor("Selection", Color.green, 2);
		HexPosition.setColor("Selectable", Color.green, 3);
		HexPosition.setColor("Attack", Color.red, 4);
		HexPosition.setColor("Cursor", Color.blue, 5);
		HexPosition.Marker = marker;
		foreach (Transform child in obstacles.transform) {
			HexPosition position = new HexPosition(child.position);
			child.position = position.getPosition();
			position.flag("Obstacle");
		}
	}
	
	private void select () {
		if (mouse.isSelected ("Selectable")) {
			HexPosition.clearSelection ("Selectable");
			selection = mouse;
			mouse.select ("Selection");
			Unit unit = (Unit) mouse.getValue ("Unit");
			selectAttackable (unit);
			switch (unit.Status) {
			case Unit.State.MOVE:
				turn = Turn.MOVE;
				break;
			case Unit.State.ATTACK:
				turn = Turn.ATTACK;
				break;
			default:
				print ("Error: Action " + ((Unit) mouse.getValue ("Unit")).Status + " not implemented.");
				break;
			}
		}
	}
	
	public void endTurn () {
		HexPosition.clearSelection ();
		foreach (Unit unit in units) {	//I only need to do this with units on that team, but checking won't speed things up. I could also only do it when player overflows.
			unit.newTurn ();
		}
		player = (player + 1) % PLAYERS;
		if(player == 0 || !computerPlayer) {
			selectSelectable ();
		}
	}
	
	private void unselect () {
		HexPosition.clearSelection ();
		selection = null;
		mouse.select ("Cursor");
		if (!selectSelectable ()) {
			endTurn ();
		}
		turn = Turn.SELECT;
	}
	
	private void checkGameOver() {
		gameOver = true;
		foreach (Unit unit in units) {
			if (unit.PLAYER != player) {
				gameOver = false;
				break;
			}
		}
		if (gameOver) {
			return;
		}
	}
	
	private void actuallyAttack () {
		((Unit) selection.getValue("Unit")).attack ((Unit) mouse.getValue("Unit"));
		checkGameOver ();
		unselect ();
	}
	
	private void move () {
		if (mouse.Equals (selection)) {
			unselect ();
		} else if (!mouse.containsKey ("Unit")) {
			if (path.Length > 0) {
				Unit myUnit = ((Unit) selection.getValue ("Unit"));
				myUnit.move (path);
				HexPosition.clearSelection ();
				selection = mouse;
				selection.select ("Selection");
				if (selectAttackable (myUnit)) {
					turn = Turn.ATTACK;
				} else {
					myUnit.Status = Unit.State.WAIT;
					unselect ();
				}
			}
		} else {
			object enemy = null;
			if (mouse.tryGetValue ("Unit", out enemy)) {
				Unit myUnit = ((Unit) selection.getValue ("Unit"));
				if (isAttackable (myUnit, (Unit) enemy)) {
					actuallyAttack ();
				}
			}
		}
	}
	
	private void attack () {
		if (mouse.isSelected ("Attack")) {
			actuallyAttack ();
		}
	}
		
	void Update () {
		if (waiting || gameOver || !modeSelected) {
			return;
		}
		if (player == 1 && computerPlayer) {
			if (ai.go()) {
				endTurn ();
			}
			checkGameOver ();
			return;
		}
		/*if (timeout > 0) {
			--timeout;
			if(timeout == 0) {
				print ("Warning: HexGrid.cs timed out.");
			}
			return;
		}*/
		if (!Input.mousePresent) {
			mouse = null;
		} else {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(ray);
			if (hits.Length == 0) {
				if (mouse != null && turn == Turn.MOVE) {
					HexPosition.clearSelection ("Path");
					HexPosition.clearSelection ("Attack");
					path = null;
				}
				mouse = null;
			} else {
				float minDist = float.PositiveInfinity;
				int min = 0;
				for (int i=0; i<hits.Length; ++i) {
					if(hits[i].distance < minDist) {
						minDist = hits[i].distance;
						min = i;
					}
				}
				HexPosition newMouse = new HexPosition (hits[min].point);
				if (newMouse != mouse) {
					if (mouse != null) {
						mouse.unselect ("Cursor");
					}
					if (newMouse.containsKey ("Obstacle")) {	//The Obstacle tag is being used to make the tile unselectable.
						if (mouse != null && turn == Turn.MOVE) {
							HexPosition.clearSelection ("Path");
							HexPosition.clearSelection ("Attack");
							path = null;
						}
						mouse = null;
						return;
					}
					mouse = newMouse;
					mouse.select ("Cursor");
					if(turn == Turn.MOVE) {
						Unit unit = (Unit) selection.getValue ("Unit");
						HexPosition.clearSelection ("Path");
						HexPosition.clearSelection ("Attack");
						path = AStar.search (selection, mouse, unit.SPEED);
						HexPosition.select ("Path", path);
						selectAttackable (unit, mouse);
					}
				}
				if (Input.GetButtonDown ("Fire1")) {
					switch (turn) {
					case Turn.SELECT:
						select ();
						break;
					case Turn.MOVE:
						move ();
						break;
					case Turn.ATTACK:
						attack ();
						break;
					default:
						print ("Error: Turn " + turn + " not implemented.");
						break;
					}
					return;
				}
			}
		}
	}
	
	void OnGUI () {
		if (!modeSelected) {
			if (GUI.Button (new Rect (10, 10, 90, 20), "1 Player")) {
				selectSelectable ();
				computerPlayer = true;
				modeSelected = true;
				ai = new AI(units, 1);
				return;
			}
			if (GUI.Button (new Rect (10, 40, 90, 20), "2 Player")) {
				selectSelectable ();
				computerPlayer = false;
				modeSelected = true;
				return;
			}
			if (GUI.Button (new Rect (10, 70, 90, 20), "Add unit")) {
				SpawnUnit(UnitTypes.box, 0);
				return;
			}
			return;
		}
		if (gameOver) {
			GUIStyle style = new GUIStyle ();
			style.fontSize = 72;
			style.alignment = TextAnchor.MiddleCenter;
			GUI.Box (new Rect (10, 10, Screen.width-20, Screen.height-20), "Player " + (player + 1) + " Wins!", style);
			return;
		}
		if (waiting || (player == 1 && computerPlayer)) {
			return;
		}
		GUI.Box (new Rect (10, 10, 90, 20), "Player " + (player + 1));
		switch (turn) {
		case Turn.SELECT:
			GUI.Box (new Rect (10, 40, 90, 20), "Select");
			if (GUI.Button (new Rect (10, 70, 90, 20), "End Turn")) {
				endTurn ();
			}
			break;
		case Turn.MOVE:
			GUI.Box (new Rect (10, 40, 90, 20), "Move");
			if (GUI.Button (new Rect (10, 70, 90, 20), "Cancel Move")) {
				unselect ();
			}
			break;
		case Turn.ATTACK:
			GUI.Box (new Rect (10, 40, 90, 20), "Attack");
			if (GUI.Button (new Rect (10, 70, 90, 20), "Skip Attack")) {
				HexPosition.clearSelection ();
				selection = null;
				if (mouse != null) {
					mouse.select ("Cursor");
				}
				selectSelectable ();
				turn = Turn.SELECT;
			}
			break;
		}
	}
}