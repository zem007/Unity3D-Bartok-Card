using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PlayerType {
    human,
    ai
}

[System.Serializable]
public class Player
{
    public PlayerType type = PlayerType.ai;
    public int playerNum;
    public SlotDef handSlotDef;
    public List<CardBartok> hand;   //玩家手中的所有牌

    public CardBartok AddCard(CardBartok eCB) {
        if(hand == null) hand = new List<CardBartok>();
        hand.Add(eCB);  //把抽到的卡放到手里
        //给当前手中的所有卡牌按大小排序，用Linq
        if(type== PlayerType.human) {
            CardBartok[] cards = hand.ToArray();
            cards = cards.OrderBy(cd => cd.rank).ToArray();
            hand = new List<CardBartok>(cards);
        }
        eCB.SetSortingLayerName("10");   //放置在最上层
        eCB.eventualSortLayer = handSlotDef.layerName;
        FanHand();    //把抽到的卡，用扇形的样子展示在桌面上
        return eCB;
    }

    public CardBartok RemoveCard(CardBartok cb) {
        if(hand == null || !hand.Contains(cb)) return null;
        hand.Remove(cb);
        FanHand();
        return cb;
    }

    public void FanHand() {
        //设定第一张卡的旋转角度(根据牌的数量而改变)
        float startRot = 0;
        startRot = handSlotDef.rot;
        if(hand.Count > 1) {
            startRot += Bartok.S.handFanDegrees * (hand.Count - 1) / 2;
        }
        //把所有的卡放到新的位置上
        Vector3 pos;
        float rot;
        Quaternion rotQ;
        for(int i=0; i<hand.Count; i++) {
            rot = startRot - Bartok.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0,0,rot);
            pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
            pos = rotQ * pos;     // 得到一个按照rotQ的四元数旋转的新向量: pos
            pos += handSlotDef.pos;   //加上手中卡的基础世界坐标位置
            pos.z = -0.5f * i;

            if(Bartok.S.phase != TurnPhase.idle) {
                hand[i].timeStart = 0;
            }
            hand[i].MoveTo(pos, rotQ);
            hand[i].state = CBState.toHand;

            // hand[i].transform.localPosition = pos;
            // hand[i].transform.rotation = rotQ;
            // hand[i].state = CBState.hand;
            hand[i].faceUp = (type == PlayerType.human);
            hand[i].eventualSortOrder = i * 4;
            // hand[i].SetSortOrder(i * 4);   //卡片覆盖
        }

    }

    //简单的AI控制其他出手牌
    public void TakeTurn() {
        Utils.tr("Player.TakeTurn");
        if(type == PlayerType.human) return;
        Bartok.S.phase = TurnPhase.waiting;
        CardBartok cb;
        List<CardBartok> validCards = new List<CardBartok>();

        foreach(CardBartok tCB in hand) {
            if(Bartok.S.ValidPlay(tCB)) {
                validCards.Add(tCB);
            }
        }
        if(validCards.Count == 0) {
            cb = AddCard(Bartok.S.Draw());
            cb.callbackPlayer = this;
            return;
        }
        cb = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cb);
        Bartok.S.MoveToTarget(cb);
        cb.callbackPlayer = this;
    }

    public void CBCallback(CardBartok tCB) {
        Utils.tr("Player.CBCallback()", tCB.name, "Player " + playerNum);
        //这张卡已经到位
        Bartok.S.PassTurn();
    }
    
}
