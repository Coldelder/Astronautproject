/// <summary>
/// 任何能「跳过正在播放的过场」的组件都实现这个接口。
///
/// 这样跳过按钮不用关心场景里挂的是 CinematicPlayer、CinematicPlayer1
/// 还是以后新增的其它过场播放器，只要类型实现了本接口就能被找出来。
/// </summary>
public interface ISkippableCinematic
{
    void Skip();
}
