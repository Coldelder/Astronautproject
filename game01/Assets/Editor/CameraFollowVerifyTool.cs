using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Flag-driven Play-mode verification for CameraFollow on Level4.
/// Trigger: create "camera_follow_verify_flag.txt" in the project root.
/// The tool enters Play mode, drives the Player transform through a scripted
/// sequence (dead zone, right follow, right clamp, left clamp), records the
/// camera position every frame, captures native screenshots and writes
/// "camera_follow_verify_report.txt" with pass/fail for every check.
/// Runtime changes are discarded on exit, so the player/prefab are not modified.
/// </summary>
[InitializeOnLoad]
public static class CameraFollowVerifyTool
{
    const string FlagName = "camera_follow_verify_flag.txt";
    const string ReportName = "camera_follow_verify_report.txt";
    const string ShotFolder = "verify_shots";
    const int FramesPerStep = 60;
    const float Tol = 0.02f;

    enum State { WaitFlag, EnterPlay, WaitReady, RunStep, Done }

    static bool consumed;
    static State state = State.WaitFlag;
    static int frame;
    static int step;
    static string root;
    static string shotDir;

    static Camera cam;
    static CameraFollow follow;
    static Rigidbody2D playerRb;
    static Tilemap floor;

    static float mapLeft, mapRight, half, deadHalf, minX, maxX;
    static float startCamX, startY, startZ;
    static float maxAbsY, maxAbsZ;
    static float stepStartCamX;
    static float targetPlayerX;
    static readonly List<string> checks = new List<string>();
    static readonly StringBuilder detail = new StringBuilder();

    static CameraFollowVerifyTool()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        switch (state)
        {
            case State.WaitFlag:
                if (consumed) return;
                root = Directory.GetParent(Application.dataPath).FullName;
                if (!File.Exists(Path.Combine(root, FlagName))) return;
                if (Application.isPlaying) { EditorApplication.ExitPlaymode(); return; }
                consumed = true;
                File.Delete(Path.Combine(root, FlagName));
                state = State.EnterPlay;
                shotDir = Path.Combine(root, ShotFolder);
                if (Directory.Exists(shotDir)) Directory.Delete(shotDir, true);
                Directory.CreateDirectory(shotDir);
                EditorApplication.EnterPlaymode();
                break;

            case State.EnterPlay:
                if (Application.isPlaying) state = State.WaitReady;
                break;

            case State.WaitReady:
                follow = Object.FindObjectOfType<CameraFollow>();
                if (follow != null && follow.target != null)
                {
                    cam = follow.GetComponent<Camera>();
                    playerRb = follow.target.GetComponent<Rigidbody2D>();
                    floor = FindFloor();

                    // Freeze the player so the scripted positions are deterministic.
                    playerRb.bodyType = RigidbodyType2D.Kinematic;
                    playerRb.gravityScale = 0f;
                    playerRb.velocity = Vector2.zero;

                    // Reference values (same computation as the runtime script).
                    floor.CompressBounds();
                    BoundsInt cell = floor.cellBounds;
                    mapLeft = floor.CellToWorld(cell.min).x;
                    mapRight = floor.CellToWorld(cell.max).x;
                    half = cam.orthographicSize * cam.aspect;
                    deadHalf = half * follow.deadZoneRatio;
                    minX = mapLeft + half;
                    maxX = mapRight - half;

                    startCamX = cam.transform.position.x;
                    startY = cam.transform.position.y;
                    startZ = cam.transform.position.z;
                    maxAbsY = 0f; maxAbsZ = 0f;

                    detail.AppendLine("Reference values:");
                    detail.AppendLine("  mapLeft=" + mapLeft.ToString("F3") + " mapRight=" + mapRight.ToString("F3"));
                    detail.AppendLine("  orthoSize=" + cam.orthographicSize + " aspect=" + cam.aspect.ToString("F4"));
                    detail.AppendLine("  worldHalfWidth=" + half.ToString("F3") + " deadHalfWidth=" + deadHalf.ToString("F3"));
                    detail.AppendLine("  cameraMinX=" + minX.ToString("F3") + " cameraMaxX=" + maxX.ToString("F3"));
                    detail.AppendLine("  startCamX=" + startCamX.ToString("F3") + " y=" + startY + " z=" + startZ);

                    Capture("shot_start.png");
                    step = 0;
                    state = State.RunStep;
                    frame = 0;
                    BeginStep();
                }
                break;

            case State.RunStep:
                TrackAxes();
                // Hold the player at the scripted position (in case anything nudges it).
                playerRb.position = new Vector2(targetPlayerX, playerRb.position.y);
                frame++;
                if (frame >= FramesPerStep)
                {
                    EvaluateStep();
                    step++;
                    if (step >= 4)
                    {
                        Finalize();
                        state = State.Done;
                        EditorApplication.ExitPlaymode();
                    }
                    else
                    {
                        frame = 0;
                        BeginStep();
                    }
                }
                break;

            case State.Done:
                // Wait for play mode to fully exit, then write the report.
                if (!Application.isPlaying)
                {
                    File.WriteAllText(Path.Combine(root, ReportName), BuildReport());
                    state = State.WaitFlag;
                    consumed = false;
                }
                break;
        }
    }

    static void BeginStep()
    {
        float camX = cam.transform.position.x;
        stepStartCamX = camX;
        switch (step)
        {
            case 0: // Inside dead zone (halfway to the right edge of the dead zone).
                targetPlayerX = camX + deadHalf * 0.5f;
                break;
            case 1: // Beyond the right dead edge.
                targetPlayerX = camX + deadHalf + 3f;
                break;
            case 2: // Far beyond the right map edge -> clamp.
                targetPlayerX = mapRight + 50f;
                break;
            case 3: // Far beyond the left map edge -> clamp.
                targetPlayerX = mapLeft - 50f;
                break;
        }
        playerRb.position = new Vector2(targetPlayerX, playerRb.position.y);
    }

    static void EvaluateStep()
    {
        float camX = cam.transform.position.x;
        switch (step)
        {
            case 0:
                Add("Dead zone keeps camera still",
                    Mathf.Abs(camX - stepStartCamX) < Tol,
                    "delta=" + (camX - stepStartCamX).ToString("F4"));
                Capture("shot_deadzone.png");
                break;
            case 1:
                float edgeOffset = targetPlayerX - camX;
                bool moved = camX > stepStartCamX + Tol;
                bool onEdge = Mathf.Abs(edgeOffset - deadHalf) < Tol;
                Add("Right follow: camera moves right and keeps target on dead edge",
                    moved && onEdge,
                    "moved=" + (camX - stepStartCamX).ToString("F3") + " targetOffset=" + edgeOffset.ToString("F3"));
                break;
            case 2:
                bool rightClamp = Mathf.Abs(camX - maxX) < Tol;
                bool rightAlign = Mathf.Abs((camX + half) - mapRight) < Tol;
                Add("Right bound: camera clamps at maxX and right edge aligns with tiles",
                    rightClamp && rightAlign,
                    "camX=" + camX.ToString("F3") + " maxX=" + maxX.ToString("F3") +
                    " rightEdge=" + (camX + half).ToString("F3") + " mapRight=" + mapRight.ToString("F3"));
                Capture("shot_right.png");
                break;
            case 3:
                bool leftClamp = Mathf.Abs(camX - minX) < Tol;
                bool leftAlign = Mathf.Abs((camX - half) - mapLeft) < Tol;
                Add("Left bound: camera clamps at minX and left edge aligns with tiles",
                    leftClamp && leftAlign,
                    "camX=" + camX.ToString("F3") + " minX=" + minX.ToString("F3") +
                    " leftEdge=" + (camX - half).ToString("F3") + " mapLeft=" + mapLeft.ToString("F3"));
                Capture("shot_left.png");
                break;
        }
    }

    static void TrackAxes()
    {
        float dy = Mathf.Abs(cam.transform.position.y - startY);
        float dz = Mathf.Abs(cam.transform.position.z - startZ);
        if (dy > maxAbsY) maxAbsY = dy;
        if (dz > maxAbsZ) maxAbsZ = dz;
    }

    static void Finalize()
    {
        Add("Y axis never changes", maxAbsY < Tol, "maxAbsDeltaY=" + maxAbsY.ToString("F4"));
        Add("Z axis never changes", maxAbsZ < Tol, "maxAbsDeltaZ=" + maxAbsZ.ToString("F4"));
    }

    static void Add(string name, bool pass, string data)
    {
        checks.Add((pass ? "PASS" : "FAIL") + " - " + name + " (" + data + ")");
    }

    static string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("CameraFollow Play-mode verification");
        sb.AppendLine(detail.ToString());
        foreach (var c in checks) sb.AppendLine(c);
        int fails = checks.FindAll(c => c.StartsWith("FAIL")).Count;
        sb.AppendLine("TOTAL: " + (checks.Count - fails) + "/" + checks.Count + " passed");
        sb.AppendLine(fails == 0 ? "VERIFY_ALL_PASS" : "VERIFY_HAS_FAILURES");
        return sb.ToString();
    }

    static void Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, name));
    }

    static Tilemap FindFloor()
    {
        var all = Object.FindObjectsOfType<Tilemap>();
        foreach (var t in all) if (t.name == "floor") return t;
        return all.Length > 0 ? all[0] : null;
    }
}
