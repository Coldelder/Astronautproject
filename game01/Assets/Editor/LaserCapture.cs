using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Text;

public static class LaserCapture
{
    [MenuItem("Tools/Laser Capture")]
    public static void Capture() => CaptureTo("laser_capture.png", null);

    [MenuItem("Tools/Laser Capture UV")]
    public static void CaptureUV() => CaptureTo("laser_uv.png", "Debug/UV");

    static void CaptureTo(string fileName, string overrideShader)
    {
        var cam = Camera.main;
        if (cam == null) { Debug.Log("[LaserCapture] no Main Camera"); return; }

        var laser = Object.FindObjectOfType<Laser2D>();
        var lr = laser != null ? laser.GetComponent<LineRenderer>() : null;

        // 强制重算一次光线，避免拿到编辑模式下没重建的旧网格
        if (laser != null)
        {
            var m = typeof(Laser2D).GetMethod("FireLaser", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m != null) m.Invoke(laser, null);
        }

        Material original = null;
        Material temp = null;
        if (lr != null && overrideShader != null)
        {
            var sh = Shader.Find(overrideShader);
            if (sh == null) { Debug.Log($"[LaserCapture] shader {overrideShader} not found"); return; }
            original = lr.sharedMaterial;
            temp = new Material(sh);
            lr.sharedMaterial = temp;
        }

        if (lr != null)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lr.positionCount; i++)
                sb.Append(lr.GetPosition(i).ToString("F2")).Append("  ");
            Debug.Log($"[LaserCapture] ortho={cam.orthographic} size={cam.orthographicSize:F3} " +
                      $"width={lr.startWidth:F3}/{lr.endWidth:F3} positions({lr.positionCount}): {sb}");
        }

        const int w = 1024, h = 576;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;

        var path = Path.Combine(Application.dataPath, "..", fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);

        if (lr != null && temp != null)
        {
            lr.sharedMaterial = original;
            Object.DestroyImmediate(temp);
        }

        Debug.Log($"[LaserCapture] saved {path}");
    }
}
