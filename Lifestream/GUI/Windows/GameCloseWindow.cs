using NightmareUI.ImGuiElements;

namespace Lifestream.GUI.Windows;
public class GameCloseWindow : Window
{
    public int World = 0;
    private WorldSelector WorldSelector = new()
    {
        EmptyName = "停用",
    };
    public GameCloseWindow() : base("Lifestream 排程器", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize)
    {
        RespectCloseHotkey = false;
        ShowCloseButton = false;
    }

    public override void Draw()
    {
        if(World == 0)
        {
            ImGuiEx.Text("未啟用，請選擇目標伺服器");
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "執行中");
        }
        ImGuiEx.Text($"抵達下列伺服器後關閉遊戲：");
        ImGui.SetNextItemWidth(200f.Scale());
        WorldSelector.Draw(ref World);
    }
}
