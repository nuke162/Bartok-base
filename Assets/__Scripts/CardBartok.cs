using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//CBState includes both states for the game and to states for movement
public enum CBState{
	drawpile,
	toHand,
	hand,
	toTarget,
	target,
	discard,
	to,
	idle
}

//cardbartok extends card just as cardprospector did.
public class CardBartok : Card {

	public Player callbackPlayer = null;

	//these static fields are used to set values that will be the same
	//for all instances of CardBartok
	static public float MOVE_DURATION = 0.5f;
	static public string MOVE_EASING = Easing.InOut ;
	static public float CARD_HIGHT = 3.5f;
	static public float CARD_WIDTH = 2f;

	public CBState state = CBState.drawpile;

	//fields to store info the card will use to move and rotate 
	public List<Vector3> bezierPTS;
	public List<Quaternion> bezierRots;
	public float timeStart, timeDuration; //declares 2 feilds

	public int eventualSortOrder;
	public string eventualSortLayer;

	//when the card is done moving, it will call report finshto.sendmsessage()
	public GameObject reportFinishTo=null;

	void Awake(){
		callbackPlayer = null;
	}
	//moveto tells the card to interpolate to a new position and rotation
	public void MoveTo (Vector3 ePos, Quaternion eRot){
		//make new interpolation lists for the card
		//position and rotation will each have only two points
		bezierPTS = new List<Vector3>();
		bezierPTS.Add (transform.localPosition); //current positino
		bezierPTS.Add (ePos); //New Position
		bezierRots=new List<Quaternion>();
		bezierRots.Add (transform.rotation); //current roattion
		bezierRots.Add (eRot); //new rotation

		//if timeStart is 0, then its set to start immediatly,
		//otherwise, it starts at timeStart. this way, if timestart is 
		//already set, it wont be overwritten
		if (timeStart ==0){
			timeStart = Time.time;
		}//timeDuration always starts the same but can be altered later
		timeDuration = MOVE_DURATION;

		//setting state to eaither tohand or totraget will be handled by
		//callijng methord
		state = CBState.to;
	}
	//this overload od moveto doesnt rquire a rotation argument
	public void MoveTo(Vector3 ePos){
		MoveTo (ePos, Quaternion.identity);
	}
	
	// Update is called once per frame
	void Update () {


		switch (state) {
		//all the to states are ones where the card is interpolating
		case CBState.toHand:
		case CBState.toTarget:
		case CBState.to:
			//get u from the current time and duration
			// u ranges from 0 to 1 usually
			float u = (Time.time - timeStart)/timeDuration;

			//easing class for utils to curve the u value
			float uC= Easing.Ease (u, MOVE_EASING);

			if (u<0){ //if u<0, then we shouldnt move yet
				//Stay at teh intila position
				transform.localPosition = bezierPTS[0];
				transform.rotation = bezierRots[0];
				return ;
			}else if (u>=1){//if u>=1 were finsihed moving
				uC = 1 ; //set uc=1 so we dont overshoot
				//Move from the to state to the following state
				if (state == CBState.toHand) state = CBState.hand;
				if (state == CBState.toTarget) state = CBState.toTarget ;
				if (state == CBState.to) state = CBState.idle ;
				//move to the final position
				transform.localPosition = bezierPTS[bezierPTS.Count-1];
				transform.rotation = bezierRots[bezierPTS.Count-1];
				//reset timestartt to 0 so it gets overwritten next time 
				timeStart = 0;

				if (reportFinishTo != null){//if theres a callback gameobject
					//... then use sendmessage to call the cbcallback method
					//with this as the parameter 
					reportFinishTo.SendMessage("CBCallback", this) ;
					//After calling sendmessage(), report finsih to must be set
					//to null so that it the card doesnt continue to report
					// to the same gameobject every subsequent time it moves
					reportFinishTo = null;
				}else if (callbackPlayer != null){
					//if there s a callback player
					//then call cbcallback directory o nthe player
					callbackPlayer.CBCallback(this);
					callbackPlayer = null;

				}else { // if there is nothing else to callback
					//do nothing
				}
			}else{ //0<=u<1, which means that this is interpolating now 
				//use bezier curve to move this to the right point 
				Vector3 pos = Utils.Bezier(uC,bezierPTS);
				transform.localPosition = pos;
				Quaternion rotQ = Utils.Bezier(uC, bezierRots);
				transform.rotation = rotQ ;

				if (u>0.5f&&spriteRenderer[0].sortingOrder!=eventualSortOrder){
					//jump to the proper sort order
					SetSortingOrder(eventualSortOrder);
				}
				if(u>0.75f&&spriteRenderer[0].sortingLayerName!=eventualSortLayer){
					//jump to the proper sort layer
					SetSortingLayerName(eventualSortLayer);
				}
			}
			break ;
		}
	}
	//this allows the card to react to being clicked
	override public void OnMouseUpAsButton(){
		//call the cardclicked method on the bartok singleton
		Bartok.S.CardClicked (this);
		//also call the base class card.cs version of this mthod
		base.OnMouseUpAsButton ();
	}
}
