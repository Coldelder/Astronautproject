using UnityEditor;
using UnityEngine;

public static class LaserDebugTool
{
    [MenuItem("Tools/Laser Debug Log")]
    public static void LogLaserState()
    {
        var laser = Object.FindObjectOfType<Laser2D>();
        if (laser == null)
        {
            Debug.Log("[LaserDebug] no Laser2D in scene");
            return;
        }

        var lr = laser.GetComponent<LineRenderer>();
        var curve = lr.widthCurve;
        string keys = "";
        foreach (var k in curve.keys) keys += $"(t={k.time:F2},v={k.value:F3}) ";

        var grad = lr.colorGradient;
        string gkeys = "";
        foreach (var k in grad.colorKeys) gkeys += $"(t={k.time:F2},rgb={k.color.r:F2}/{k.color.g:F2}/{k.color.b:F2}) ";
        string akeys = "";
        foreach (var k in grad.alphaKeys) akeys += $"(t={k.time:F2},a={k.alpha:F2}) ";

        Debug.Log($"[LaserDebug] width: beamWidth={laser.beamWidth:F3} mult={lr.widthMultiplier:F3} " +
                  $"start={lr.startWidth:F3} end={lr.endWidth:F3} keys={curve.length} {keys}");
        Debug.Log($"[LaserDebug] widthEval 0:{curve.Evaluate(0f):F3} 0.5:{curve.Evaluate(0.5f):F3} 1:{curve.Evaluate(1f):F3} positions={lr.positionCount}");
        Debug.Log($"[LaserDebug] colorKeys={grad.colorKeys.Length} {gkeys}");
        Debug.Log($"[LaserDebug] alphaKeys={grad.alphaKeys.Length} {akeys}");
        Debug.Log($"[LaserDebug] alphaEval 0:{grad.Evaluate(0f).a:F3} 0.5:{grad.Evaluate(0.5f).a:F3} 1:{grad.Evaluate(1f).a:F3}");
    }
}