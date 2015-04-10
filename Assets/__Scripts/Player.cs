using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; //enables linq queries which iwll be explained soon

//the player can either be human or an ai
public enum PlayerType{
	human,
	ai
}

//the individual player of the game
//note player does not exettend monbehavior
[System.Serializable]
public class Player{

	public PlayerType type = PlayerType.ai;
	public int playerNum ;

	public List<CardBartok> hand; //the cards in this players hand

	public SlotDef handSlotDef;

	//add a card to the hand
	public CardBartok AddCard(CardBartok eCB){
		if (hand == null) hand = new List<CardBartok>();

		//Add the card to the hand
		hand.Add (eCB);

		//sort the cards by rank using linq1 if this is a human player
		if (type == PlayerType.human) {
			CardBartok[] cards = hand.ToArray();

			//below is the linq call that works on the array of cardbartoks
			//it is similar to doing a foreach cardbartock cd in cards
			//and sorting them by rank. it then returns a sorted array
			cards = cards.OrderBy(cd =>cd.rank).ToArray() ;

			//confver the array cardbartok[] back to a list cardbartok
			hand = new List<CardBartok>(cards);
			//note linq operations can be a bit slow like it could take a couble 
			//of milliseconds but since were only doing it once
			//every turn it isnt a problem
		}

		eCB.SetSortingLayerName("10"); // this sorts the moving card to the top
		eCB.eventualSortLayer = handSlotDef.layerName;

		FanHand ();
		return (eCB);
	}

	//remove a card from the hand
	public CardBartok RemoveCard(CardBartok cb){
		hand.Remove (cb);
		FanHand ();
		return(cb);
	}

	public void FanHand(){
		//startrot is the rotation about z of hte first card
		float startRot = 0;
		startRot = handSlotDef.rot;
		if (hand.Count>1){
			startRot += Bartok.S.handFanDegrees*(hand.Count-1)/2;
		}
		//then each card is rotated handfandegrees from that to fan the cards

		//move all the cards to their new positoins
		Vector3 pos;
		float rot;
		Quaternion rotQ;
		for (int i=0;i<hand.Count;i++){
			rot = startRot - Bartok.S.handFanDegrees*i; //rotate about hte z axis
			//also adds the roattions of the different players hand
			rotQ = Quaternion.Euler(0,0,rot);
			//quaternion representing the same rotation as rot

			//pos is a v3 half a card hight above [0,0,0] ie, 0,1.75,0
			pos = Vector3.up*CardBartok.CARD_HIGHT / 2f;

			//multiplying a quaternion by a vector 3 rotates that vector 3 by 
			//the rotation stored in the quaternion. the result gives us a 
			// vector above [0,0,0] that has been rotated by rot degrees
			pos = rotQ*pos;

			//ADD the base position of the playerts hand (which will be at the )
			//bottom center of the fan of the cards
			pos += handSlotDef.pos;
			//thisStaggers the cards in the z direction. which isnt visable
			//but which fdoes keep their colliders form overlapping
			pos.z = -0.5f*i;

			//the line below makes sure that the card starts moving immediatly
			//if its not the initaial deal at the begining of the game
			if (Bartok.S.phase != TurnPhase.idle){
				hand[i].timeStart =0;
			}

			hand[i].MoveTo(pos,rotQ); // tell cardbartok to inspector
			hand[i].state = CBState.toHand;
			//after the move cardbartok will set the state to cbstate.hand


			/*
			//set the localposition and rotation of the ith card in the hand 
			hand[i].transform.localPosition = pos ;
			hand[i].transform.rotation = rotQ ;
			hand[i].state = CBState.hand ;
			*/

			//this uses a comparison operator to return a true or flase bool
			//so if thype == player type.human hand .faceup is set to true
			hand[i].faceUp = (type == PlayerType.human);

			//set the sortorder of the cards sothat they overlap proprly
			hand[i].eventualSortOrder = i*4;
			//hand[i].SetSortingOrder(i*4) ;

			//
		}
	}

	//the taketurn fuction enables the ai of hte computer players
	public void TakeTurn(){
				Utils.tr (Utils.RoundToPlaces (Time.time), "Player.TakeTurn");
				//dont need to do anything if this is the human player
				if (type == PlayerType.human)
						return;

				Bartok.S.phase = TurnPhase.waiting;

				CardBartok cb;

				//if this is an ai plaer need to make a choice about what to play
				//find valid plays
				List<CardBartok> validCards = new List<CardBartok> ();
				foreach (CardBartok tCB in hand) {
						if (Bartok.S.ValidPlay (tCB)) {
								validCards.Add (tCB);
						}
				}
				//if there are no valid cards
				if (validCards.Count == 0) {
						//tehn draw a card
						cb = AddCard (Bartok.S.Draw ());
						cb.callbackPlayer = this;
						return;
				}

				//otherwise if there is a card or more to play pick one
				cb = validCards [Random.Range (0, validCards.Count)];
				RemoveCard (cb);
				Bartok.S.MoveToTarget (cb);
				cb.callbackPlayer = this;
		}
	public void CBCallback (CardBartok tCB){
		Utils.tr (Utils.RoundToPlaces (Time.time), "Player.CBCallback()", tCB.name, "Player " + playerNum);
		//the card is done moving so pass the turn
		Bartok.S.PassTurn ();

		}
}
