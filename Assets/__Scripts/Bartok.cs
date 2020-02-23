using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TurnPhase {
    idle,
    pre,
    waiting,
    post,
    gameOver
}

public class Bartok : MonoBehaviour
{
    public static Bartok S;
    public static Player CURRENT_PLAYER;
    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;
    public float handFanDegrees = 10f;
    public int numStartingCards = 7;
    public float drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck deck;   //Deck脚本同样可以用在Prospector项目中,在CardProspector类中
    public List<CardBartok> drawPile;
    public List<CardBartok> discardPile;
    public List<Player> players;
    public CardBartok targetCard;
    public TurnPhase phase = TurnPhase.idle;

    private BartokLayout layout;
    private Transform layoutAnchor;

    void Awake()
    {
        S = this;
    }

    void Start() 
    {
        deck = GetComponent<Deck>();   //获取此object的Deck脚本
        deck.InitDeck(deckXML.text);    //获取deckXML.xml中的信息
        Deck.Shuffle(ref deck.cards);   //ref关键字确保deck.cards本身被shuffle

        layout = GetComponent<BartokLayout>();
        layout.ReadLayout(layoutXML.text);

        drawPile = UpgradeCardsList(deck.cards);
        LayoutGame();
    }

    //把Card类中的所有卡转换成其子类CardBartok中的所有卡
    List<CardBartok> UpgradeCardsList(List<Card> lCD) {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach(Card tCD in lCD) {
            lCB.Add(tCD as CardBartok);
        }
        return(lCB);
    }

    //
    public void ArrangeDrawPile() {
        CardBartok tCB;

        for(int i=0; i<drawPile.Count; i++) {
            tCB = drawPile[i];
            tCB.transform.SetParent(layoutAnchor);
            tCB.transform.localPosition = layout.drawPile.pos;
            tCB.faceUp = false;
            tCB.SetSortingLayerName(layout.drawPile.layerName);
            tCB.SetSortOrder(-i*4);
            tCB.state = CBState.drawpile;
        }
    }

    void LayoutGame() {
        if(layoutAnchor == null) {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        ArrangeDrawPile();

        Player p1;
        players = new List<Player>();
        foreach(SlotDef tSD in layout.slotDefs) {
            p1 = new Player();
            p1.handSlotDef = tSD;
            players.Add(p1);
            p1.playerNum = tSD.player;
        }
        players[0].type = PlayerType.human;

        //设置每个玩家初始的手牌
        CardBartok tCB;
        for(int i=0; i<numStartingCards; i++) {
            for(int j=0; j<4; j++) {
                tCB = Draw();
                //延迟一段时间再抽下一张牌
                tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);
                players[(j+1)%4].AddCard(tCB);
            }
        }

        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards*4 + 4));
    }

    public void DrawFirstTarget() {
        //把第一张牌从牌堆里抽出,并翻为正面
        CardBartok tCB = MoveToTarget(Draw());
        tCB.reportFinishTo = this.gameObject;
    }

    public void CBCallback(CardBartok cb) {
        Utils.tr("Bartok:CBCallback()", cb.name);
        StartGame();
    }

    public void StartGame() {
        PassTurn(1);
    }

    public void PassTurn(int num = -1) {
        //如果没有num，轮换到下一个Player
        if(num == -1) {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1)%4;
        }
        int lastPlayerNum = -1;
        if(CURRENT_PLAYER != null) {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            if(CheckGameOver()) return;
        }
        CURRENT_PLAYER = players[num];
        phase = TurnPhase.pre;
        CURRENT_PLAYER.TakeTurn();

        //报告轮换
        Utils.tr("Bartok:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
    }

    public bool CheckGameOver() {
        //检查是否需要把discard pile中的牌重新洗入draw pile
        // if(drawPile.Count == 0) {
        //     List<Card> cards = new List<Card>();
        //     foreach(CardBartok cb in discardPile) {
        //         cards.Add(cb);
        //     }
        //     discardPile.Clear();
        //     Deck.Shuffle(ref cards);
        //     drawPile = UpgradeCardsList(cards);
        //     ArrangeDrawPile();
        // }

        //检查CURRENT_PLAYER是否赢了
        if(CURRENT_PLAYER.hand.Count == 0) {
            phase = TurnPhase.gameOver;    //状态转化为gameOver之后，UI中update显示
            Invoke("RestartGame", 10);  //UI显示时间为10s，之后重启游戏
            return true;
        }
        return false;
    }

    public void RestartGame() {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Bartok_Scene_0");
    }

    public bool ValidPlay(CardBartok cb) {
        if(cb.rank == targetCard.rank) return(true);
        if(cb.suit == targetCard.suit) return(true);
        return(false);
    }

    public CardBartok MoveToTarget(CardBartok tCB) {
        tCB.timeStart = 0;
        tCB.MoveTo(layout.discardPile.pos + Vector3.back);
        tCB.state = CBState.toTarget;
        tCB.faceUp = true;

        tCB.SetSortingLayerName("10");
        tCB.eventualSortLayer = layout.target.layerName;
        if(targetCard != null) {
            MoveToDiscard(targetCard);
        }

        targetCard = tCB;

        return(tCB);
    }

    public CardBartok MoveToDiscard(CardBartok tCB) {
        tCB.state = CBState.discard;
        discardPile.Add(tCB);
        tCB.SetSortingLayerName(layout.discardPile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardPile.pos + Vector3.back/2;
        return tCB;
    }

    public CardBartok Draw() {
        //如果待抽取的牌堆被抽空了，触发下列条件
        if(drawPile.Count == 0) {
            //给废弃的牌堆discardPile洗牌，然后重新放入抽牌堆中
            int ndx;
            while (discardPile.Count > 0) {
                ndx = Random.Range(0, discardPile.Count);
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }
            ArrangeDrawPile();
            //添加卡牌移动到新牌堆的动画
            float t = Time.time;
            foreach(CardBartok tCB in drawPile) {
                tCB.transform.localPosition = layout.discardPile.pos;
                tCB.callbackPlayer = null;
                tCB.MoveTo(layout.drawPile.pos);
                tCB.timeStart = t;
                t += 0.1f;
                tCB.state = CBState.toDrawpile;
                tCB.eventualSortLayer = "0";
            }
        }

        CardBartok cd = drawPile[0];  // null if drawPile is empty
        drawPile.RemoveAt(0);
        return cd;
    }

    public void CardClicked(CardBartok tCB) {
        if(CURRENT_PLAYER.type != PlayerType.human) return;
        if(phase == TurnPhase.waiting) return;

        switch(tCB.state) {
            case CBState.drawpile:
                CardBartok cb = CURRENT_PLAYER.AddCard(Draw());
                cb.callbackPlayer = CURRENT_PLAYER;
                Utils.tr("Bartok: CardClicked() ", "Draw ", cb.name);
                break;
            case CBState.hand:
                if(ValidPlay(tCB)) {
                    CURRENT_PLAYER.RemoveCard(tCB);
                    MoveToTarget(tCB);
                    tCB.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr("Bartok: CardClicked() ", "Play ", tCB.name, targetCard.name + " is target");
                    phase = TurnPhase.waiting;
                } else {
                    Utils.tr("Bartok: CardClicked() ", "Attempted to Play ", tCB.name, targetCard.name + " is target");
                }
                break;
        }
    }

    // 测试玩家1-4手牌中是否响应
    // void Update() {
    //     if(Input.GetKeyDown(KeyCode.Alpha1)) {
    //         players[0].AddCard(Draw());
    //     }
    //     if(Input.GetKeyDown(KeyCode.Alpha2)) {
    //         players[1].AddCard(Draw());
    //     }
    //     if(Input.GetKeyDown(KeyCode.Alpha3)) {
    //         players[2].AddCard(Draw());
    //     }
    //     if(Input.GetKeyDown(KeyCode.Alpha4)) {
    //         players[3].AddCard(Draw());
    //     }
    // }

}
