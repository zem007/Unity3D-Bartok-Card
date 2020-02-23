using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // using uGUI classes

public class GameOverUI : MonoBehaviour
{
    private Text txt; //调用Text脚本组件

    void Awake()
    {
        txt = GetComponent<Text>();
        txt.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if(Bartok.S.phase != TurnPhase.gameOver) {
            txt.text = "";
            return;
        }
        if(Bartok.CURRENT_PLAYER == null) return;   //游戏一开始为null
        if(Bartok.CURRENT_PLAYER.type == PlayerType.human) {
            txt.text = "You won!";
        } else {
            txt.text = "Game Over";
        }
    }
}
