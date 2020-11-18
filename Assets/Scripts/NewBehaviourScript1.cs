using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NewBehaviourScript1 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
//================================================================================
//===================用于游戏页对手指长按的检测脚本
//================================================================================
    public Player a;//保存角色脚本的引用

    //当手指按下时调用这个函数
    public void OnPointerDown(PointerEventData eventData)
    {
        a.input1();//调用角色的响应函数
        a.PointDown=true;//设置相关变量
    }

    // 当手指抬起时调用这个函数
    public void OnPointerUp(PointerEventData eventData)
    {
        a.PointDown = false;//设置相关变量
        a.input2();//调用角色的响应函数
    }
}
