using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [Header("Set in Inspector")]
    public bool startFaceUp = false;
    public Sprite suitClub;
    public Sprite suitDiamond;
    public Sprite suitHeart;
    public Sprite suitSpade;
    public Sprite[] faceSprites;
    public Sprite[] rankSprites;
    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;
    public GameObject prefabCard;
    public GameObject prefabSprite;

    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;   //脚本类
    public List<string> cardNames;
    public List<Card>  cards;
    public List<Decorator> decorators;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictSuits;

    //InitDeck函数被Prospector脚本在MainCamera中调用
    public void InitDeck(string deckXMLText) {
        if(GameObject.Find("_Deck") == null) {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }
        //初始化四种花色的字典
        dictSuits = new Dictionary<string, Sprite>() {
            {"C", suitClub},
            {"D", suitDiamond},
            {"H", suitHeart},
            {"S", suitSpade}
        };
        //读取DeckXML文件
        ReadDeck(deckXMLText);
        MakeCards();
    }

    //把xml文件编译后传递到CarDefinitions中
    public void ReadDeck(string deckXMLText) {
        xmlr = new PT_XMLReader();
        xmlr.Parse(deckXMLText);   //用PT_XMLReader脚本的Parse方法读xml
        string s = "xml[0] decorator[0] ";
        s += "type=" + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += " x=" + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += " y=" + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += " scale=" + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        // print(s);

        //read decorators for all Cards
        decorators = new List<Decorator>();
        PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];  //得到全部“decorator”下的信息
        Decorator deco; //声明其中一个<decorator>
        //对于xml中的每个<decorator>, 这个项目中为Count=4
        for(int i=0; i<xDecos.Count; i++) {
            deco = new Decorator();
            //deco储存每一个decorator中的信息
            deco.type = xDecos[i].att("type");
            deco.flip = (xDecos[i].att("flip") == "1");   //true if flip att is "1"
            deco.scale = float.Parse(xDecos[i].att("scale"));
            deco.loc.x = float.Parse(xDecos[i].att("x"));
            deco.loc.y = float.Parse(xDecos[i].att("y"));
            deco.loc.z = float.Parse(xDecos[i].att("z"));
            decorators.Add(deco);
        }

        //read图案排列图形，xml文件中的所有<card>
        cardDefs = new List<CardDefinition>();
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];  //得到全部<card>下的信息
        for(int i=0; i<xCardDefs.Count; i++) {
            CardDefinition cDef = new CardDefinition();
            cDef.rank = int.Parse(xCardDefs[i].att("rank"));
            PT_XMLHashList xPips = xCardDefs[i]["pip"];  //"pip"不是xml中的att
            if(xPips != null) {   //针对1-10
                for(int j=0; j<xPips.Count; j++) {
                    deco = new Decorator();
                    deco.type = "pip";
                    deco.flip = (xPips[j].att("flip") == "1");
                    deco.loc.x = float.Parse(xPips[j].att("x"));
                    deco.loc.y = float.Parse(xPips[j].att("y"));
                    deco.loc.z = float.Parse(xPips[j].att("z"));
                    if(xPips[j].HasAtt("scale")) {   //针对1
                        deco.scale = float.Parse(xPips[j].att("scale"));
                    }
                    cDef.pips.Add(deco);
                }
            }
            if(xCardDefs[i].HasAtt("face")) {  //针对JQK，“face”是此xml中的att
                cDef.face = xCardDefs[i].att("face");
            }
            cardDefs.Add(cDef);
        }
    }

    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        foreach(CardDefinition cd in cardDefs) {
            if(cd.rank == rnk) {
                return(cd);
            }
        }
        return(null);
    }

    public void MakeCards() {
        cardNames = new List<string>();
        string[] letters = new string[] {"C", "D", "H", "S"};
        foreach(string s in letters) {
            for(int i=0; i<13; i++) {
                cardNames.Add(s+(i+1));  //例如 C1 到 C13
            }
        }

        cards = new List<Card>();
        for(int i=0; i<cardNames.Count; i++) {
            cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum) {
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        cgo.transform.parent = deckAnchor;  //把所有卡的父级设为deckAnchor
        Card card = cgo.GetComponent<Card>();  //获取其中的Card脚本类
        //把卡片堆成堆，并且排成几排
        cgo.transform.localPosition = new Vector3((cNum%13)*3, cNum/13*4, 0);

        //给card赋值
        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));
        if(card.suit == "D" || card.suit == "H") {
            card.colS = "Red";
            card.color = Color.red;
        }
        card.def = GetCardDefinitionByRank(card.rank);
        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;

    private void AddDecorators(Card card) {
        foreach(Decorator deco in decorators) {
            if(deco.type == "suit") {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSR.sprite = dictSuits[card.suit];
            } else {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSp = rankSprites[card.rank];
                _tSR.sprite = _tSp;
                _tSR.color = card.color;
            }
            //令deco Sprites 在Card之上
            _tSR.sortingOrder = 1;
            //令decorator Sprite是Card的child
            _tGO.transform.SetParent(card.transform);
            _tGO.transform.localPosition = deco.loc;
            if(deco.flip) {
                _tGO.transform.rotation = Quaternion.Euler(0,0,180);
            }
            if(deco.scale != 1) {
                _tGO.transform.localScale = Vector3.one * deco.scale;
            }
            _tGO.name = deco.type;
            card.decoGOs.Add(_tGO);
        }
    }

    private void AddPips(Card card) {
        foreach(Decorator pip in card.def.pips) {
            _tGO = Instantiate(prefabSprite) as GameObject;
            _tGO.transform.SetParent(card.transform);
            _tGO.transform.localPosition = pip.loc;
            if(pip.flip) {
                _tGO.transform.rotation = Quaternion.Euler(0,0,180);
            }
            if(pip.scale != 1) {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }
            _tGO.name = "pip";
            _tSR = _tGO.GetComponent<SpriteRenderer>();
            _tSR.sprite = dictSuits[card.suit];
            _tSR.sortingOrder = 1;
            card.pipGOs.Add(_tGO);
        }
    }

    private void AddFace(Card card) {
        if(card.def.face == "") {
            return;
        }
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSp = GetFace(card.def.face + card.suit);
        _tSR.sprite = _tSp;
        _tSR.sortingOrder = 1;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    private Sprite GetFace(string faceS) {
        foreach(Sprite _tSP in faceSprites) {
            if(_tSP.name == faceS) {
                return(_tSP);
            }
        }
        return(null);
    }

    private void AddBack(Card card) {
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;   //把cardBack图片赋给tSR
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tSR.sortingOrder = 2; //higher order than 1
        _tGO.name = "back";
        card.back = _tGO;     //重要：把tGO传递给card.back
        //default to dace-up
        card.faceUp = startFaceUp;
    }

    public static void Shuffle(ref List<Card> oCards) {  //ref关键字确保 oCards被传入进函数进行操作，而不是传入一个copy
        //创建一个临时的list来储存洗牌后的牌组
        List<Card> tCards = new List<Card>();   //声明
        int ndx;
        tCards = new List<Card>();   //初始化
        while(oCards.Count > 0) {
            ndx = Random.Range(0, oCards.Count);
            tCards.Add(oCards[ndx]);
            oCards.RemoveAt(ndx);
        }
        oCards = tCards;
    }
}
