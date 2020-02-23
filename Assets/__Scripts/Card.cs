using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Decorator 
{
    //储存了DeckXML中所有的字母和图案信息
    public string type;  
    public Vector3 loc;
    public bool flip = false;
    public float scale = 1f;
}

[System.Serializable]
public class CardDefinition 
{
    //储存了1-13的所有纸牌的信息
    public string face;  
    public int rank;   // 1-13
    public List<Decorator> pips = new List<Decorator>();   //1-10中的每张牌中间的图案合集
}

public class Card : MonoBehaviour
{
    [Header("Set Dynamically")]
    public string suit;   //C=Club, D=Diamond, H=Heart, S=Spade
    public int rank;  //1-13
    public Color color = Color.black;  //Color to tint pips
    public string colS = "Black"; //or "r=Red"
    public List<GameObject> decoGOs = new List<GameObject>();   //所有的Decorator
    public List<GameObject> pipGOs = new List<GameObject>();   //所有的Pip
    public GameObject back;   //back of the card
    public CardDefinition def;  //调用上面的CarDefinition

    public SpriteRenderer[] spriteRenderers;

    void Start() 
    {
        SetSortOrder(0);
    }

    //如果spriteRenderers没有定义，在这里定义
    public void PopulateSpriteRenderers() {
        if(spriteRenderers == null || spriteRenderers.Length == 0) {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();  //this.GameObject and its children' SpriteRenderer
        }
    }

    public void SetSortingLayerName(string tSLN) {
        PopulateSpriteRenderers();
        foreach( SpriteRenderer tSR in spriteRenderers) {
            tSR.sortingLayerName = tSLN;
        }
    }

    //为每个SpriteRenderer组件赋值layername
    public void SetSortOrder(int sOrd) 
    {
        PopulateSpriteRenderers();

        foreach(SpriteRenderer tSR in spriteRenderers) {
            if(tSR.gameObject == this.gameObject) {
                tSR.sortingOrder = sOrd;
                continue;
            }
            switch (tSR.gameObject.name) {
                case "back":
                    tSR.sortingOrder = sOrd + 2;
                    break;
                case "face":
                default: 
                tSR.sortingOrder = sOrd + 1;
                break;
            }
        }
    }

    public bool faceUp {
        get{
            return (!back.activeSelf);
        }
        set{
            back.SetActive(!value);
        }
    }

    virtual public void OnMouseUpAsButton() {
        print(name);
    }

}

