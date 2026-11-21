using UnityEngine;
using UnityEngine.UI; // 必须引入 UI 命名空间

public class TutorialPopup : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject popupPanel;       // 整个弹窗面板
    public Image pageImage;             // 显示图片的组件
    public Button actionButton;         // 右下角的按钮
    public Image buttonIcon;            // 按钮上的图标图片

    [Header("图片数据")]
    public Sprite[] pages;              // 存放你所有要展示的图片

    [Header("按钮图标")]
    public Sprite nextSprite;           // 翻页箭头图标
    public Sprite closeSprite;          // 关闭叉叉图标

    private int currentPage = 0;        // 当前页数索引
    private bool isClosed = false;      // 防止重复关闭

    void Start()
    {
        // 游戏开始时，确保弹窗是显示的
        popupPanel.SetActive(true);

        // 暂停游戏（可选）。如果你希望弹窗时玩家不能乱走，就把下面这行取消注释
        // Time.timeScale = 0f;

        if (pages.Length == 0)
        {
            Debug.LogError("没有设置任何图片！");
            return;
        }

        // 初始化第一页
        currentPage = 0;
        UpdateUI();

        // 给按钮添加点击事件
        actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    void OnActionButtonClicked()
    {
        // 如果还没到最后一页，就翻页
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            UpdateUI();
        }
        // 如果已经是最后一页（按钮变成了叉叉），点击就关闭
        else
        {
            ClosePopup();
        }
    }

    void UpdateUI()
    {
        // 1. 更新图片
        pageImage.sprite = pages[currentPage];

        // 2. 判断是不是最后一页，决定按钮显示什么图标
        if (currentPage == pages.Length - 1)
        {
            buttonIcon.sprite = closeSprite; // 变成了叉叉
        }
        else
        {
            buttonIcon.sprite = nextSprite; // 还是右箭头
        }
    }

    void ClosePopup()
    {
        if (isClosed) return;
        isClosed = true;

        // 隐藏弹窗
        popupPanel.SetActive(false);

        // 恢复游戏时间（如果第一步你暂停了游戏）
        // Time.timeScale = 1f;
    }
}