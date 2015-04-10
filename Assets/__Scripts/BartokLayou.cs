using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Slot def class is not based on monobehavior so it doesnt need its own file
[System.Serializable]//make slot def able to be seeen in the unity inspector
public class SlotDef{
	public float x;
	public float y;
	public bool faceUp = false;
	public string layerName = "Default";
	public int layerID = 0;
	public int id;
	public List<int> hiddenBy = new List<int>();//unused in bartok
	public float rot; //rotation of the hands
	public string type="slot" ;
	public Vector2 stagger;
	public int player ;   //player number of a hand
	public Vector3 pos;   //pos derived from x and y and multiplayer
}

public class BartokLayou : MonoBehaviour {

	public PT_XMLReader xmlr;
	public PT_XMLHashtable xml;
	public Vector2 multiplier;
	//slot def ref
	public List<SlotDef> slotDefs ; //the slotdefs hands
	public SlotDef drawPile; 
	public SlotDef discardPile;
	public SlotDef target;

	//this function is called to read in the layoutxml.xml file
	public void ReadLayout(string xmlText){
		xmlr = new PT_XMLReader ();
		xmlr.Parse (xmlText); //the xml is parsed
		xml = xmlr.xml ["xml"] [0]; //and xml is set as a shortcut to the xml

		//read in the multiplier, which sets card spaceing
		multiplier.x = float.Parse (xml["multiplier"][0].att ("x"));
		multiplier.y = float.Parse (xml["multiplier"][0].att ("x"));

		//read in the slots 
		SlotDef tSD;
		//slotsXis used as a shortcut to all the <slots>s
		PT_XMLHashList slotsX = xml["slot"];

		for (int i =0;i<slotsX.Count;i++){
			tSD = new SlotDef() ; //create a new slotdef instance
			if (slotsX[i].HasAtt("type")){
				//if this slot has a tpye atribute parse it
				tSD.type = slotsX[i].att ("type");
			}else{
				//if not set its type to slot 
				tSD.type = "slot";
			}

			//various attributes are parsed into numberical values
			tSD.x = float.Parse(slotsX[i].att("x"));
			tSD.y = float.Parse(slotsX[i].att("y"));
			tSD.pos = new Vector3(tSD.x*multiplier.x,tSD.y*multiplier.y,0);

			//sorting layers
			tSD.layerID = int.Parse(slotsX[i].att ("layer"));
			//in this game , the sorting layers are named 1,2,3 tyhrough 10
			//this converts the number of the layer id into a text layername
			tSD.layerName = tSD.layerID.ToString() ;
			//the layers are used to make sure that the correct cards are 
			//on top of the others. in unity 2d all of your assets are 
			//effectivly at the same z depth, so sorting layers are used 
			//to differentiate between them
			//publl additional attributes based on the type of each slot
			switch (tSD.type){
			case "slot":
				//ignore slots that are just of the slot type 
				break;

			case "drawpile":
				//the drawpile xstagger is read but not actually used in bartok
				tSD.stagger.x = float.Parse(slotsX[i].att ("xstagger")) ;
				drawPile = tSD ;
				break ;

			case "discardpile":
				discardPile = tSD ; 
				break ;

			case "target":
				//the target card has a diferent layer from discardpile
				target = tSD ;
				break;

			case "hand":
				//information for each players hand 
				tSD.player = int.Parse(slotsX[i].att ("player"));
				tSD.rot = float.Parse(slotsX[i].att ("rot")) ;
				slotDefs.Add(tSD) ;
				break ;

			}
		}

	}


}
