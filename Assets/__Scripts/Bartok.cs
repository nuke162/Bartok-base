using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this enum contains the differnet phases of a game turn
public enum TurnPhase{
	idle,
	pre,
	waiting,
	post,
	gameOver
}

public class Bartok : MonoBehaviour {
	static public Bartok S;
	//this field is a static to enforce that theris only 1 current player
	static public Player CURRENT_PLAYER;

	public TextAsset deckXML;
	public TextAsset layoutXML;
	public Vector3 layoutCenter = Vector3.zero;

	//the number of deg to fan each card in a hand
	public float handFanDegrees = 10f ;

	public bool _______________________;

	public Deck deck;
	public List<CardBartok> drawPile ;
	public List<CardBartok> discardPile;

	public BartokLayou layout;
	public Transform layoutAnchor;

	public List<Player> players;
	public CardBartok targetCard ;

	public int numStartingCards = 7;
	public float drawTimeStagger =0.1f;

	public TurnPhase phase = TurnPhase.idle ;
	public GameObject turnLight;

	public GameObject GTGameOver;
	public GameObject GTRoundResult;

	void Awake(){
		S = this;

		//find the turnlight by name
		turnLight = GameObject.Find ("TurnLight");
		GTGameOver = GameObject.Find ("GTGameOver");
		GTRoundResult = GameObject.Find ("GTRoundResult");
		GTGameOver.SetActive (false);
		GTRoundResult.SetActive (false);
	}

	// Use this for initialization
	void Start () {
		deck = GetComponent<Deck>() ;//get the deck
		deck.InitDeck (deckXML.text); //pass deckXml to it
		Deck.Shuffle (ref deck.cards); //this shuffles the deck
		//the ref keyword passes a reference to deck.card which allows 
		//deck.cards to be modified by deck.shuffle

		layout = GetComponent<BartokLayou> (); // get the layout
		layout.ReadLayout (layoutXML.text); //pass layoutxml to it

		drawPile = UpgradeCardList (deck.cards);

		LayoutGame ();
	
	}

	//upgradeCardList casts the cards in lcd to be cardbartoks
	//of course they were all along but this lets unity know it
	List<CardBartok> UpgradeCardList(List<Card> lCD){
		List<CardBartok> lCB = new List<CardBartok>();
		foreach(Card tCD in lCD){
			lCB.Add(tCD as CardBartok);
		}
		return (lCB);
	}

	public void CardClicked(CardBartok tCB){
		//if its not the humans turn dont respond
		if (CURRENT_PLAYER.type != PlayerType.human) return;
		//if the game is waiting on a card to move dont respond
		if (phase==TurnPhase.waiting)return;

		//act differently based on whether it was a crd in hand
		//or on the drawpile
		switch (tCB.state) {
		case CBState.drawpile:
			//draw the top card not necessarily the one clicked
			CardBartok cb = CURRENT_PLAYER.AddCard(Draw ()) ;
			cb.callbackPlayer = CURRENT_PLAYER;
			Utils.tr (Utils.RoundToPlaces(Time.time),"Bartok.CardClicked()","Draw",cb.name);
			phase = TurnPhase.waiting ;
			break;
		case CBState.hand:
			//check to see whether the card is valid
			if (ValidPlay(tCB)&& tCB.faceUp==true ){ //added "&& tCB.faceUp==true so player cant move other players cards
				CURRENT_PLAYER.RemoveCard(tCB);
				MoveToTarget(tCB);
				tCB.callbackPlayer = CURRENT_PLAYER ;
				Utils.tr (Utils.RoundToPlaces(Time.time),"Bartok.CardClicked()","Play",tCB.name,targetCard.name+" is target");
				phase = TurnPhase.waiting;
			}else{
				//just ignore it
				Utils.tr (Utils.RoundToPlaces(Time.time),"Bartok.CardClicked()","Attempt to Play",tCB.name,targetCard.name+" is target");
			}
			break ;
		}
	}
	//position all the cards in the drawpile properly
	public void ArrangeDrawPile(){
		CardBartok tCB;

		for(int i=0;i<drawPile.Count;i++){
			tCB = drawPile[i] ;
			tCB.transform.parent = layoutAnchor ;
			tCB.transform.localPosition = layout.drawPile.pos ;
			//rotation should start at 0
			tCB.faceUp = false;
			tCB.SetSortingLayerName(layout.drawPile.layerName) ;
			tCB.SetSortingOrder(-i*4); //order them fron to back
			tCB.state = CBState.drawpile;
		}

	}

	//perform the intial game layout
	void LayoutGame(){
		//create an empty gameObject to serve as an anchor for the tables
		if (layoutAnchor == null){
			GameObject tGO = new GameObject("_LayoutAnchor");
			//create an empty gamobject anemd alyout anchor in the higherarchy
			layoutAnchor = tGO.transform; //grab its transform
			layoutAnchor.transform.position = layoutCenter ; //positionit
		}

		//position the drawpile cards
		ArrangeDrawPile ();

		//Set up the players 
		Player p1;
		players = new List<Player> ();

		foreach(SlotDef tSD in layout.slotDefs){
			p1 = new Player() ;
			p1.handSlotDef = tSD;
			players.Add(p1);
			p1.playerNum = players.Count ;
		}
		players [0].type = PlayerType.human;//make the 0th player human

		CardBartok tCB;
		//deal 7 card to each player
		for (int i=0;i<numStartingCards;i++){
			for (int j=0;j<4;j++){ // number of players
				tCB= Draw();// draw a card
				//stagger the draw time a bit. remember order of operations
				tCB.timeStart = Time.time + drawTimeStagger * (i*4+j);
				//by setting the timestart before calling add card we 
				//override the automatic setting of timestart in
				//cardbartok.moveTO
				//add the card to the players hand. the modulus (%4)
				//results in a number from 0 to 3
				players[(j+1)%4].AddCard(tCB);
			}
		}

		//callbartokdrawfirsttarget when the hand cards have been drawn
		Invoke ("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
	}

	public void DrawFirstTarget(){
		//flip up the first target card from the draw pile
		CardBartok tCB = MoveToTarget (Draw ());
		//set the cardbartok to call cbcallback on this bartok when it is done
		tCB.reportFinishTo = this.gameObject;
	}

	//this call back is used by the last card to be delt at the begining 
	//iot is only used once per game
	public void CBCallback(CardBartok cb){
		//you sometimes want to have reporting of mehod calls like this
		Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.CBCallback()", cb.name);

		StartGame (); //start the game
	}

	public void StartGame(){
		//pick the player to the left of the human to go first
		//player[0] is human
		PassTurn (1);
	}

	public void PassTurn(int num =-1 ){
		//if no number was pased in pic kthe next player
		if (num == -1){
			int ndx = players.IndexOf(CURRENT_PLAYER);
			num = (ndx+1)%4;
		}
		int lastPlayerNum = -1;
		if (CURRENT_PLAYER != null){
			lastPlayerNum = CURRENT_PLAYER.playerNum ;
			//check for game over and need to reshuffle idscards
			if (CheckGameOver()){
				return;
			}
		}
		CURRENT_PLAYER = players [num];
		phase = TurnPhase.pre;

		CURRENT_PLAYER.TakeTurn ();

		//move the turn light to shine on the new currnet player
		Vector3 lPos = CURRENT_PLAYER.handSlotDef.pos + Vector3.back * 5;
		turnLight.transform.position = lPos;

		//report the turn passing
		Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);

	}

	//valid play verifies that the card chosen can be played on the discard pile 
	public bool ValidPlay(CardBartok cb){
		//its a valid play if the rank is the same
		if (cb.rank == targetCard.rank)return true;

		if (cb.suit == targetCard.suit)return true;

		return false;
	}
	public CardBartok MoveToTarget (CardBartok tCB){
		tCB.timeStart = 0;
		tCB.MoveTo (layout.discardPile.pos+Vector3.back);
		tCB.state = CBState.toTarget;
		tCB.faceUp = true;
		tCB.SetSortingLayerName("10"); //layout.target.layername
		tCB.eventualSortLayer = layout.target.layerName;
		if (targetCard != null){
			MoveToDiscard(targetCard);
		}
		targetCard = tCB;

		return(tCB);
	}

	//the draw functions will pull a single card from the drawpile and return it
	public CardBartok Draw(){
		CardBartok cd = drawPile [0]; //pull the 0th card
		drawPile.RemoveAt (0);      //then remove it
		return(cd);					//and return it
	}

	public CardBartok MoveToDiscard(CardBartok tCB){
		tCB.state = CBState.discard;
		discardPile.Add (tCB);
		tCB.SetSortingLayerName (layout.discardPile.layerName);
		tCB.SetSortingOrder (discardPile.Count * 4);
		tCB.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

		return (tCB);

	}

	public bool CheckGameOver(){
		//see if we need to reshuffile the discard pilke into the draw pile
		if (drawPile.Count == 0){
			List<Card> cards = new List<Card>();
			foreach(CardBartok cb in discardPile){
				cards.Add(cb) ;
			}
			discardPile.Clear ();
			Deck.Shuffle(ref cards);
			drawPile = UpgradeCardList(cards);
			ArrangeDrawPile();
		}

		//check to see if the current player has won
		if (CURRENT_PLAYER.hand.Count == 0){
			//the current player has won!
			if (CURRENT_PLAYER.type == PlayerType.human){
				GTGameOver.guiText.text = "You Won!" ;
				GTRoundResult.guiText.text="";
			}else {
				GTGameOver.guiText.text = "Game Over";
				GTRoundResult.guiText.text="Player "+CURRENT_PLAYER.playerNum + " won";
			}
			GTGameOver.SetActive(true);
			GTRoundResult.SetActive(true);
			phase = TurnPhase.gameOver;
			Invoke("RestartGame",1) ;
			return (true) ;
		}
		return (false);
	}

	public void RestartGame(){
		CURRENT_PLAYER = null;
		Application.LoadLevel ("__Bartok_Scene_0");
	}
	/*
	//this update method is used to test adding cards to players hands
	void Update(){
		if (Input.GetKeyDown(KeyCode.Alpha1)){
			players[0].AddCard(Draw ()) ;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2)){
			players[1].AddCard(Draw ()) ;
		}
		if (Input.GetKeyDown(KeyCode.Alpha3)){
			players[2].AddCard(Draw ()) ;
		}
		if (Input.GetKeyDown(KeyCode.Alpha4)){
			players[3].AddCard(Draw ()) ;
		}


	}
*/
}
