using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//================================================================================
//===================用于app首页UI的脚本文件
//================================================================================
public class JumpGameManager : MonoBehaviour {
    //变量声明
    public static JumpGameManager instance;
    //建立本脚本的静态实例，来保证可以通过类名JumpGameManager直接调用脚本中的各种方法
    public int mode = 0;
    //通过一个整数来保存玩家选择的不同模式
    public GameObject choosepanel;
    //场景中选择面板的引用
    public GameObject panel0,panel1,panel2,panel3, panel4;
    //场景中各种子选择面板的引用
    public static int dos = -2;
    //通过一个静态整数变量实现从另一个场景切换回首页后显示哪个UI，默认-2

    //首先执行的是awake
    private void Awake()
    {
        JumpGameManager.instance = this;
        //将本脚本引用传给instance
        DontDestroyOnLoad(JumpGameManager.instance.gameObject);
        //通过这个方法来让这个管理脚本在切换场景时不会被销毁
    }

    //start在awake之后执行
    private void Start()
    {
        if (dos != -2)
        {//首先判断dos变量是不是默认的-2，不是则说明是从其他场景切换回来的
            showPanel();
            switch (dos)
            {
                case 0: panel0.SetActive(true); break;
                case 1: panel1.SetActive(true);  break;
                case 2: panel2.SetActive(true);  break;
                case 3: panel3.SetActive(true);  break;
                case 4: panel4.SetActive(true);  break;
                default:break;
            }
            //通过dos不同的值来决定显示哪个panel
        }
    }

    //以下为功能函数，只会在被人工调用时执行，不像上面两个会自动执行
    public void showPanel()
    {
        choosepanel.SetActive(true);
        //此方法用于显示choosepanel
    }

    //按钮回调函数，每个按钮会传一个不同的整数i
    public void forBtn(int i)
    {
        switch (i)
        {
            //通过判断i的值来判断用户按了哪个按钮，来做不同的事情
            case 0: panel0.SetActive(true); break;
            case 1: panel1.SetActive(true); panel0.SetActive(false); break;
            case 2: panel2.SetActive(true); panel0.SetActive(false); break;
            case 3: panel3.SetActive(true); panel0.SetActive(false); break;
            case 4: panel4.SetActive(true); panel0.SetActive(false); break;
            default: mode = i; startGame(); break;
        }

    }

    //此方法用于隐藏各种panel
    public void hidePanel(int i)
    {
        switch (i)
        {
            case 0: panel0.SetActive(false); break;
            case 1: panel1.SetActive(false); break;
            case 2: panel2.SetActive(false); break;
            case 3: panel3.SetActive(false); break;
            case 4: panel4.SetActive(false); break;
        }
    }

    //开始游戏，切换到游戏场景
    public void startGame()
    {
        SceneManager.LoadScene(1);
    }

    //通过这个函数来改变dos的值
    public void changeDos(int num)
    {
        dos = num;
    }
}
