using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CBState {
    toDrawpile,
    drawpile,
    toHand,
    hand,
    toTarget,
    target,
    discard,
    to,
    idle
}

public class CardBartok : Card
{
    public static float MOVE_DURATION = 0.5f;
    public static string MOVE_EASING = Easing.InOut;    
    public static float CARD_HEIGHT = 3.5f;    
    public static float CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardBartok")]    
    public CBState state = CBState.drawpile;
    public List<Vector3> bezierPts;
    public List<Quaternion> bezierRots;
    public float timeStart, timeDuration;
    //当卡片移动的动画结束之后，调用reportFinishTo.SendMessage()函数
    public GameObject reportFinishTo = null;
    [System.NonSerialized]
    public Player callbackPlayer = null;
    public int eventualSortOrder;
    public string eventualSortLayer;

    //设定移动的终止点和旋转角度
    public void MoveTo(Vector3 ePos, Quaternion eRot) {
        bezierPts = new List<Vector3>();
        bezierPts.Add(transform.localPosition);
        bezierPts.Add(ePos);

        bezierRots = new List<Quaternion>();
        bezierRots.Add(transform.rotation);
        bezierRots.Add(eRot);

        if(timeStart == 0) {
            timeStart = Time.time;
        }
        //移动动画的初始持续时间总是等于MOVE_DURATION，初始之后会被覆盖
        timeDuration = MOVE_DURATION;
        state = CBState.to;   //通过state来每帧update卡片移动动画
    }

    //这里的MoveTo不需要输入旋转信息
    public void MoveTo(Vector3 ePos) {
        MoveTo(ePos, Quaternion.identity);
    }

    void Update() 
    {
        //根据卡片的状态来每帧更新，每帧调用MoveTo函数进行插值平滑来移动和旋转
        switch (state) {
            case CBState.toHand:
            case CBState.toTarget:
            case CBState.toDrawpile:
            case CBState.to:
                float u = (Time.time - timeStart)/timeDuration;
                float uC = Easing.Ease(u, MOVE_EASING);
                if(u < 0) {  //移动还没开始，放在初始位置
                    transform.localPosition = bezierPts[0];
                    transform.rotation = bezierRots[0];
                    return;
                } else if (u>1) {
                    //说明移动到位了
                    uC = 1;
                    if(state == CBState.toHand) state = CBState.hand;
                    if(state == CBState.toTarget) state = CBState.target;
                    if(state == CBState.toDrawpile) state = CBState.drawpile;
                    if(state == CBState.to) state = CBState.idle;
                    //移动到终点
                    transform.localPosition = bezierPts[bezierPts.Count - 1];
                    transform.rotation = bezierRots[bezierRots.Count - 1];
                    //重置动画时间
                    timeStart = 0;

                    if(reportFinishTo != null) {
                        reportFinishTo.SendMessage("CBCallback", this);
                        reportFinishTo = null;
                    } else if(callbackPlayer != null) {
                        callbackPlayer.CBCallback(this);
                        callbackPlayer = null;
                    } else {
                        //do nothing
                    }
                } else {   // 0<u<1
                    //按照贝塞尔曲线轨迹，移动卡片,其中u是方程轨迹的系数
                    Vector3 pos = Utils.Bezier(uC, bezierPts);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, bezierRots);
                    transform.rotation = rotQ;

                    //为了解决发牌过程中，human手牌中不同层的手牌显示的问题
                    if(u>0.5f) {
                        SpriteRenderer sRend = spriteRenderers[0];
                        if(sRend.sortingOrder != eventualSortOrder) {
                            SetSortOrder(eventualSortOrder);
                        }
                        if(sRend.sortingLayerName != eventualSortLayer) {
                            SetSortingLayerName(eventualSortLayer);
                        }
                    }
                }
                break;
        }
    }

    public override void OnMouseUpAsButton() {
        Bartok.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }

}
