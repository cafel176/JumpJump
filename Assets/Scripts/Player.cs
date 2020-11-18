using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

//================================================================================
//用于游戏的所有逻辑，当初图省事全写在一个里面了....
//
//1.玩家的操控
//玩家按下时获得一个时间，手指抬起时获得另一个时间，计算时间差值
//利用这个值乘一个系数作为小人的前进距离，就实现了长按前进
//
//2.小人的跳跃
//利用每一帧更新的Update和FixedUpdate，每一次都让小人前进一点点，旋转一点点，并控制
//最后结束状态的样子，这样连贯的播放就实现了一个跳跃动画的效果
//跳跃前的蓄力同理，让小人和砖块每次都下降一点点，连贯起来就实现了一个蓄力效果
//
//3.地砖生成
//利用随机数来控制在不同模式下地砖的随机生成，方向和颜色，大小全部可以用随机数操控
//移动的砖块就是普通砖块加一个循环播放的移动动画实现
//
//4.截图的效果
//在编辑器中设置两个相机，主相机为玩家视角，另一个相机用于截图
//截图相机可以看到玩家主相机看不到的拖尾和计分UI
//当游戏结束时，记录截图相机的画面并通过二进制流传送到服务器，由服务器生成图片文件
//
//
//
//================================================================================
public class Player : MonoBehaviour 
{ 
    //定义内部类box来构成一个链表保存地砖的引用
    private class Box
    {
        public GameObject thisBox;//当前的地砖
        public Box nextBox;//本节点指向的下一个box节点
    }
    public string url;//保存服务器的网址链接
    public int totalBox =30,testBox=5;
    //totalBox场景中存在的最大地砖数量，超过这个数字会自动清理之前的地砖
    //testBox是测试模式中完成测试需要跳过的地砖数量
    //这里的30和5是默认数据，最终会被编辑器中的设定数值替代掉
    public float ySpeed = 5.0f;
    //控制角色跳跃高度能力的数据

    // 小人跳跃时，决定远近的一个参数
    public float Factor;

    // 盒子随机最远的距离
    public float MaxDistance = 5;

    //地板的引用
    public GameObject ground;

    // 第一个盒子物体
    public GameObject Stage;

    //奖励地砖的材质
    public Material mat1,mat2;

    // 左上角总分的UI组件
    public Text TotalScoreText;
    public Text TotalScoreText2;
    public Text TotalScoreText3;
    public Text inputField;

    // 粒子效果
    public GameObject Particle;
    public GameObject Particle1;

    // 小人头部
    public Transform Head;

    // 小人身体
    public Transform Body;

    // 飘分的UI组件
    public Text SingleScoreText;
    public GameObject TextPanel;
    public Text allText;
    public Text failText;
    public Text perText;
    public Text testName;

    // 面板
    public GameObject RankPanel;
    public GameObject RankPanel2;
    public GameObject PausePanel;

    // 重新开始按钮
    public Button RestartButton;
    public Button RestartButton2;
    public Button backButton;

    //预设
    public GameObject boxPrefab;
    public GameObject boxPrefab2;
    public GameObject boxPrefab_move;
    public GameObject boxPrefab_move2;
    public GameObject money;

    //摄像机
    public Camera aa;

    //音效数组
    public AudioClip[] sounds;

    [HideInInspector]
    public bool PointDown = false;
    //判断是否在按下按钮
    private AudioSource _audioSource;
    //音效播放组件
    private string filename, temp;
    //文件名的字符串
    private Rigidbody _rigidbody;
    //角色刚体
    private float _startTime;
    //长按开始的时间
    private GameObject _currentStage;
    //当前的地砖引用
    private Vector3 _cameraRelativePosition;
    //主相机的位置偏移量
    private Vector3 _cameraRelativePosition2;
    //用于截图的相机偏移量
    private int _score;
    //计分用整数
    private bool _isUpdateScoreAnimation;
    //用于判断是否在播放飘分动画

    Vector3 _direction = new Vector3(-1, 0, 0);
    //决定地砖生成的方向

    private float _scoreAnimationStartTime;
    //计分动画开始时间
    private int _lastReward = 1;
    //最终的奖励得分
    private bool _enableInput = true,inAir=false, allowBack = true;
    //允许接收输入，角色是否在空中，是否允许调出暂停画面的开关
    private Box firstBox,lastBox;
    //链表结构头节点和尾节点
    private int BoxNum = 1;
    //记录跳过了多少个砖块

    //杂乱的变量
    private float uu, kkk,allTry=0,failTry=0;
    private Vector3 ij;
    public int mode = 0;
    private string myTxt = "";
    private int pushNum = 0,boxTxtNum=1;
    private bool tr = true;

    //start最先执行
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        //获取小人的刚体组件
        _rigidbody.centerOfMass = new Vector3(0, 0, 0);
        //设置重心
        mode = JumpGameManager.instance.mode;
        //获取在首页设置的游戏模式
        _currentStage = Stage;
        //人物所在当前砖块为第一个砖块
        lastBox = firstBox = new Box();
        firstBox.thisBox= Stage;
        //初始化链表结构
        SpawnStage();
        //生成第二个地砖
        _audioSource = GetComponent<AudioSource>();
        //获取音频播放组件

        _cameraRelativePosition = Camera.main.transform.position - transform.position;
        _cameraRelativePosition2 = aa.transform.position - transform.position;
        //设置两个相机的偏移量

        backButton.onClick.AddListener(() => { backScene(); });
        RestartButton.onClick.AddListener(() => { reStart(); });
        RestartButton2.onClick.AddListener(() => { SceneManager.LoadScene(1); allowBack = true; });
        //设置按钮的回调函数
        kkk = 2 * ySpeed / (Time.fixedDeltaTime * -Physics.gravity.y);
        //根据角色跳跃高度能力计算人物纵坐标改变量

        switch (mode)
        {
            case 11:myTxt = "不变化" + System.Environment.NewLine+" ";break;
            case 12: myTxt = "瞬间变" + System.Environment.NewLine + " "; break;
            case 13: myTxt = "逐渐变" + System.Environment.NewLine + " "; break;
            case 21: myTxt = "直线" + System.Environment.NewLine + " "; break;
            case 32: myTxt = "移动" + System.Environment.NewLine + " "; break;
            case 33: myTxt = "移动且有奖励" + System.Environment.NewLine + " "; break;
            case 41: myTxt = "变小" + System.Environment.NewLine + " "; break;
            default:break;
        }
        //通过不同的模式决定输出到txt中的文本
        //System.Environment.NewLine是实现换行的，有时可能会莫名失灵....
    }

    //FixedUpdate在start之后执行，之后每隔指定的时间都会执行一次
    private void FixedUpdate()
    {
        if (inAir)
        {//如果小人离开地面，即在跳跃中
            transform.Rotate(new Vector3(_direction.x == 0 ? -360.0f / kkk : 0, 0, _direction.z == 0 ? 360.0f / kkk : 0));
            //则小人按一定角度旋转来实现翻转效果
        }
    }

    //update在FixedUpdate之后执行，之后每一帧都会执行
    void Update()
    {
        if (_enableInput&&PointDown)
        {//如果允许接收输入并且用户按下了手指
            if (mode == 12)
            {
                Body.transform.localScale = new Vector3(Body.transform.localScale.x, 0.15f, Body.transform.localScale.z);
                Head.transform.localPosition = new Vector3(Head.transform.localPosition.x, 0.32f, Head.transform.localPosition.z);
                _currentStage.transform.localScale = new Vector3(_currentStage.transform.localScale.x, uu, _currentStage.transform.localScale.z);
                _currentStage.transform.localPosition = new Vector3(_currentStage.transform.localPosition.x, -0.35f, _currentStage.transform.localPosition.z);
                //瞬间变，则小人直接变成完成的样子
            }
            else if (mode != 11)
            {
                //逐渐变，则小人每次发生一点点变化，直到达到最后的样子停止
                if (Body.transform.localScale.y >= 0.15)
                {
                    Body.transform.localScale += new Vector3(0, -1, 0) * 0.05f * Time.deltaTime;
                    Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;
                }
                //身体下压头下沉

                if (_currentStage.transform.localScale.y >= uu)
                {
                    _currentStage.transform.localScale += new Vector3(0, -1, 0) * (_currentStage.tag == "box1" ? 0.14f : 0.07f) * Time.deltaTime;
                    _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                }
                //砖块也被压扁
                
            }

        }
        if (allowBack&&Input.GetKeyDown(KeyCode.Escape))
        {
            //如果允许调出暂停菜单并且用户按下了手机返回键
            PausePanel.SetActive(true);
            //则调出暂停菜单
        }
        if (_isUpdateScoreAnimation)
            UpdateScoreAnimation();// 显示飘分效果
    }

    //以下为功能函数
    //当用户按下手指时调用
    public void input1()
    {
        if (_enableInput)
        {//如果允许接收输入
            _startTime = Time.time;
            //记录长按开始的时间
            Particle.SetActive(true);
            //播放蓄力的粒子动画
            _audioSource.loop = true;
            //音效循环
            _audioSource.clip = sounds[0];
            _audioSource.Play();
            //播放对应音效
            uu = 0.8f * _currentStage.transform.localScale.y;
            //设定当前地砖所能压缩的最大程度

            pushNum++;
            //记录按压次数
            if(!tr)//如果本次跳跃没有到达下一个砖块则输出N
                myTxt += "N" + System.Environment.NewLine+" ";
            myTxt += pushNum + "     " + boxTxtNum + "     ";
            //输出按压次数和当前盒子数
            tr = false;
            //重置为false用于下一次检测
        }
    }

    //当用户抬起手指时调用
    public void input2()
    {
        if (_enableInput)
        {//如果允许接收输入
            _audioSource.Stop();
            _audioSource.loop = false;
            //停止播放音效
            float elapse = Time.time - _startTime;
            // 计算总共按下空格的时长
            myTxt += elapse + "     " + Time.time + "     ";
            //记录跳跃距离和按压时间

            OnJump(elapse);
            //角色开始跳跃
            Particle.SetActive(false);
            //停止粒子播放

            //还原小人的形状
            Body.transform.DOScaleY(0.165f, 0.2f);
            Head.transform.DOLocalMoveY(0.35f, 0.2f);

            //还原盒子的形状
            _currentStage.transform.DOLocalMoveY(-0.25f, 0.2f);
            _currentStage.transform.DOScaleY(_currentStage.tag == "box1" ? 0.5f : 0.25f, 0.2f);

            _enableInput = false;
            //禁止输入
        }
    }

            /// <summary>
            /// 跳跃
            /// </summary>
            /// <param name="elapse"></param>
    void OnJump(float elapse)
    {
        allTry++;//更新总的尝试次数
        _rigidbody.velocity = _direction * elapse * Factor+ new Vector3(0, ySpeed, 0);
        //为人物施加速度使其运动
        _audioSource.clip = sounds[1];
        _audioSource.Play();
        //播放弹跳音效
    }

    /// <summary>
    /// 生成盒子
    /// </summary>
    void SpawnStage()
    {
        GameObject stage;
        float minDistance=1.5f;//设定最小距离
        if (mode == 0){
            //在游戏模式
            int g = Random.Range(0, 8);//通过随机数来决定生成什么样的地砖
            if (BoxNum==2||g > 5)
            {//生成移动地砖
                minDistance = 3.0f;
                stage = Instantiate(g == 7 ? boxPrefab_move : boxPrefab_move2);
                //生成地砖
                stage.GetComponent<Animator>().SetBool("X", _direction.x == 0 ? true : false);
                //设置移动方向
                stage.transform.position = _currentStage.transform.position + _direction * Random.Range(minDistance, MaxDistance);
                //设置地砖位置
                stage = stage.transform.GetChild(0).gameObject;
            }
            else
            {//生成普通地砖
                minDistance = 1.5f;
                stage = Instantiate(g > 2 ? boxPrefab : boxPrefab2);
                stage.transform.position = _currentStage.transform.position + _direction * Random.Range(minDistance, MaxDistance);
            }
        }
        else if ( mode == 32 || mode == 33)
        {//存在移动地砖的模式
            if (BoxNum%4==0)
            {//固定频率生成移动地砖
                minDistance = 3.0f;
                stage = Instantiate(BoxNum % 3 == 0 ? boxPrefab_move : boxPrefab_move2);
                stage.GetComponent<Animator>().SetBool("X", _direction.x == 0 ? true : false);
                stage.transform.position = _currentStage.transform.position + _direction * (minDistance+BoxNum%2);
                stage = stage.transform.GetChild(0).gameObject;
            }
            else
            {
                minDistance = 1.5f;
                stage = Instantiate(BoxNum % 3 == 0 ? boxPrefab : boxPrefab2);
                stage.transform.position = _currentStage.transform.position + _direction * (minDistance + BoxNum %2);
            }
        }
        else
        {//不存在移动地砖的模式
            minDistance = 1.5f;
            int g = Random.Range(0, 2);
            stage = Instantiate(g > 0 ? boxPrefab : boxPrefab2);
            stage.transform.position = _currentStage.transform.position + _direction * (minDistance + BoxNum %2);
        }
        stage.GetComponent<Renderer>().material.color =
    new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1));
        //随机确定地砖的颜色

        if (mode == 0)
        {//如果是游戏模式
            int k = Random.Range(1, 11);
            if (BoxNum == 4 || k > 8)
            {//随机生成红包
                var tt = Instantiate(money, _currentStage.transform.position + _direction * Random.Range(minDistance, MaxDistance - 1.0f) + new Vector3(0, 1, 0), money.transform.rotation);
                stage.GetComponent<MeshRenderer>().material = (k == 9 ? mat1 : mat2);
                //随机确定生成哪种店的红包
            }
        }
        else if (mode == 33)
        {
            int k = Random.Range(0, 2);
            if (BoxNum%6==0)
            {//固定频率生成红包
                var tt = Instantiate(money, _currentStage.transform.position + _direction * minDistance + new Vector3(0, 1, 0), money.transform.rotation);
                stage.GetComponent<MeshRenderer>().material = (k == 0 ? mat1 : mat2);
            }
        }
        float randomScale;
        if(mode == 41)
        {//地砖逐渐变小的模式
            randomScale = _currentStage.transform.localScale.x>0.2f? _currentStage.transform.localScale.x-0.02f:0.2f;
        }
        else if(mode==0)
        {//游戏模式
            randomScale = Random.Range(0.7f, 1);
        }
        else
            randomScale = 1.0f;//地砖大小不变的模式
        stage.transform.localScale = new Vector3(randomScale, stage.tag=="box1" ? 0.5f:0.25f, randomScale);

        //完善链表结构
        Box t = new Box();
        t.thisBox = stage;
        lastBox.nextBox = t;
        lastBox = t;
        BoxNum++;
        if (BoxNum > totalBox)
        {//如果数量超过了指定数量，则销毁之前生成的砖块
            Destroy(firstBox.thisBox);
            firstBox = firstBox.nextBox;
            BoxNum--;
        }else if (mode!=0&&BoxNum>testBox)
        {
            OnGameOver();
            //如果是测试模式，达到了指定转快数，游戏结束
        }
    }

    //当小人从砖块上起跳时调用此方法
    void OnCollisionExit(Collision collision)
    {
        _enableInput = false;//禁止检测输入
        inAir = true;//小人在跳跃中
    }

    //当小人留在砖块上时
    void OnCollisionStay(Collision collision)
    {
        if (collision.transform.parent != null)
        {
            transform.position = collision.transform.position + ij;
            //小人的位置会随着砖块变动，主要针对移动砖块
        }
    }

    /// <summary>
    /// 小人刚体与其他物体发生碰撞时自动调用，碰撞可能是砖块和地面
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        inAir = false;//跳跃过程结束
        transform.rotation = Quaternion.Euler(0, 0, 0);
        //小人旋转角复位

        if (collision.gameObject.name == "Ground")
        {//如果小人落地
            myTxt += "N" + System.Environment.NewLine+" ";
            //输出N并换行
            tr = true;
            if(mode==0)
                OnGameOver();//如果是游戏模式，直接结束游戏
            else
            {//如果是测试模式，在之前砖块复活
                failTry++;
                _rigidbody.velocity = new Vector3(0, 0, 0);
                //让人物静止下来
                transform.position = new Vector3(_currentStage.transform.position.x, 0.1f, _currentStage.transform.position.z);
                //让人物移动到之前砖块的中心位置
                transform.rotation = Quaternion.Euler(0, 0, 0);
                //小人旋转角复位
            }
        }
        else
        {//如果未落地，则说明与砖块碰撞
            if (collision.transform.parent != null)
            {//如果是移动砖块，则记录小人相对于砖块的偏移量
                Vector3 vec = transform.position - collision.transform.position;
                if (Math.Abs(vec.x) <= 0.45f && Math.Abs(vec.z) <= 0.45f)
                    ij = vec;
            }

            if (_currentStage != collision.gameObject&&transform.position.y>0.1f)
            {   //如果碰撞砖块不是记录的当前砖块，并且高度达到一定值
                //则说明到达了一个新的砖块并且确实能够站在砖块顶上
                _rigidbody.velocity = new Vector3(0, 0, 0);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                var contacts = collision.contacts;
                _currentStage = collision.gameObject;
                //刷新当前砖块
                AddScore(contacts);
                //加分
                RandomDirection();
                //随机决定下一个砖块方向
                SpawnStage();
                //生成下一个砖块
                MoveCamera();
                //移动相机
                boxTxtNum++;
                myTxt += "Y"+System.Environment.NewLine + " ";
                //输出Y并换行
                tr = true;
            }

            _enableInput = true;
            //允许检测输入
        }
    }

    //当小人吃到红包时调用此方法
    private void OnTriggerEnter(Collider other)
    {
        _audioSource.clip = sounds[2];
        _audioSource.Play();
        //播放音效
        Destroy(other.gameObject);
        //红包消失
    }

    /// <summary>
    /// 加分，准确度高的分数成倍增加
    /// </summary>
    /// <param name="contacts">小人与盒子的碰撞点</param>
    private void AddScore(ContactPoint[] contacts)
    {
        if (contacts.Length > 0)
        {//如果碰撞点数量大于0，则确有碰撞
            var hitPoint = contacts[0].point;
            hitPoint.y = 0;

            var stagePos = _currentStage.transform.position;
            stagePos.y = 0;

            var precision = Vector3.Distance(hitPoint, stagePos);
            //比较碰撞点和砖块中心位置的xz坐标
            if (precision < 0.1)
            {//如果这两个点距离小于0.1则可以认为是站在中心了
                _lastReward *= 2;
                //奖励X2
                Particle1.SetActive(true);
                //播放奖励粒子效果
            }
            else
                _lastReward = 1;//否则得分只有1

            _score += _lastReward;//计分
            TotalScoreText.text = _score.ToString();
            TotalScoreText2.text = _score.ToString();
            TotalScoreText3.text = _score.ToString();
            //更新各种分数显示UI
            ShowScoreAnimation();
            //播放飘分动画
        }
    }

    //游戏结束
    private void OnGameOver()
    {
        allowBack = false;
        //禁止调出暂停菜单
        if(mode==0)
            RankPanel2.SetActive(true);
        else
            RankPanel.SetActive(true);
        TotalScoreText.gameObject.SetActive(false);
        //进行UI操作
    }

    /// <summary>
    /// 显示飘分动画
    /// </summary>
    private void ShowScoreAnimation()
    {
        _isUpdateScoreAnimation = true;//在播放飘分动画
        _scoreAnimationStartTime = Time.time;//获取飘分动画开始时间
        SingleScoreText.text = "+" + _lastReward;
    }

    /// <summary>
    /// 更新飘分动画
    /// </summary>
    void UpdateScoreAnimation()
    {
        if (Time.time - _scoreAnimationStartTime > 1)
            _isUpdateScoreAnimation = false;//时间超过一秒，则关闭开关

        var playerScreenPos =
            RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);
        //获取小人在屏幕上的坐标
        SingleScoreText.transform.position = playerScreenPos +
                                             Vector2.Lerp(Vector2.zero, new Vector2(0, 200),
                                                 Time.time - _scoreAnimationStartTime);
        //在小人位置生成飘分动画并随着时间向上飘

        SingleScoreText.color = Color.Lerp(Color.black, new Color(0, 0, 0, 0), Time.time - _scoreAnimationStartTime);
        //文字颜色也略有改变
    }

    /// <summary>
    /// 随机方向
    /// </summary>
    void RandomDirection()
    {
        if(mode==0)
        {//如果是游戏模式
            var seed = Random.Range(0, 2);
            _direction = seed == 0 ? new Vector3(-1, 0, 0) : new Vector3(0, 0, -1);
            //随机数决定是X还是Z方向
        }
        else if (mode != 21)
        {//测试模式
            _direction = BoxNum%6>2 ? new Vector3(-1, 0, 0) : new Vector3(0, 0, -1);
            //按照一定规律改变XZ方向来自保证不同的玩家是一样的路线
        }
    }

    /// <summary>
    /// 移动摄像机
    /// </summary>
    void MoveCamera()
    {
        Camera.main.transform.DOMove(transform.position + _cameraRelativePosition, 1);
        aa.transform.DOMove(transform.position + _cameraRelativePosition2, 1);
        //让两个相机跟随小人位置移动
    }

    //截图并发送到服务器
    public void CaptureScreen(Camera c, Rect r)
    {
        string shot_Number= GetTimeStamp().ToString();

        //以下针对不同平台采用不同策略
#if UNITY_EDITOR //编辑器中 
        temp = "/ScreenShot/";
        //保存位置
        string path = string.Format("{0:D4}{1:D2}.png", temp, shot_Number);
        //文件名设置
        filename = Application.dataPath + path; 
#else
 
 
#if UNITY_ANDROID//安卓
        temp = "/";  
        string path  = string.Format ("{0:D4}{1:D2}.png", temp,shot_Number);  
        filename = Application.persistentDataPath + path;  
 
#endif
 
#if UNITY_IPHONE
        temp = "/";  
        string path  = string.Format ("{0:D4}{1:D2}.png", temp,shot_Number);  
        filename = Application.temporaryCachePath + path;  
 
#endif
#endif

        RenderTexture rt = new RenderTexture((int)r.width, (int)r.height, 24);
        //设置图片纹理的长宽

        c.targetTexture = rt;
        c.Render();

        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false);
        //设置截图的长宽
        screenShot.ReadPixels(r, 0, 0);
        screenShot.Apply();

        c.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        //将图片纹理转化为png图片
        byte[] bytes2 = System.Text.Encoding.Default.GetBytes(myTxt);
        //将字符串转化为txt
        //System.IO.File.WriteAllBytes(filename, bytes);
        WWWForm form = new WWWForm();
        form.AddField("Name", shot_Number);
        form.AddBinaryData("post", bytes);//把图片流上传 
        form.AddBinaryData("txt", bytes2);//把文字流上传

        int ui = Random.Range(0, 5);
        //随机数选择0-4中的php文件发送
        //因为图片文件较大，多人使用时可能会出现服务器php被占用导致数据丢失
        //用这种简单的随机数方法可以降低数据丢失率
        WWW www = new WWW(url+ui.ToString()+".php", form);
        StartCoroutine(PostData(www));//启动协程  
        Destroy(screenShot);//销毁 
    }

    IEnumerator<WWW> PostData(WWW www)
    {
        yield return www;
        Debug.Log(www.text);//输出服务器返回结果。  
    }

    //获取独一无二的时间戳用作文件名，避免重名文件相互覆盖
    public long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        long ret = Convert.ToInt64(ts.TotalSeconds);
        return ret;
    }

    //暂停界面按钮回调事件
    public void forBtn(bool test)
    {
        if (test)
            Application.Quit();
        else
            PausePanel.SetActive(false);
    }

    //重新开始
    public void reStart()
    {
        TextPanel.SetActive(true);
        allText.text = allTry.ToString();
        failText.text = failTry.ToString();
        perText.text = (100 * failTry / allTry).ToString("0.000") + " %";
        testName.text = inputField.text;
        //更新各种UI的界面

        CaptureScreen(aa, new Rect(0, 0, aa.pixelWidth, aa.pixelHeight));
        //截图并上传

        switch (mode)
        {
            case 11: JumpGameManager.instance.changeDos(1); break;
            case 12: JumpGameManager.instance.changeDos(1); break;
            case 13: JumpGameManager.instance.changeDos(0); break;
            case 21: JumpGameManager.instance.changeDos(0); break;
            case 32: JumpGameManager.instance.changeDos(3); break;
            case 33: JumpGameManager.instance.changeDos(0); break;
            case 41: JumpGameManager.instance.changeDos(0); break;
            default: break;
        }
        //设置管理脚本的dos值
        Destroy(JumpGameManager.instance.gameObject);
        //摧毁旧的管理脚本实例
        SceneManager.LoadScene(0);
        //返回到首页
        allowBack = true;
        //允许调出暂停界面
    }

    //回到首页
    public void backScene()
    {
        JumpGameManager.instance.changeDos(-1);
        Destroy(JumpGameManager.instance.gameObject);
        SceneManager.LoadScene(0);
    }
}