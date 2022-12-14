using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map;

[ExecuteInEditMode]
public class MapWaypoint : MapAnotator
{
    public LayerMask lyrMask;
    public bool snapping = true;

    Vector3 lastMovePos;

    //UI related
    private static Color gizmoSurfaceColor = Color.green * new Color(1.0f, 1.0f, 1.0f, 0.3f);
    private static Color gizmoLineColor = Color.green * new Color(1.0f, 1.0f, 1.0f, 0.3f);
    private static Color gizmoSurfaceColor_highlight = Color.green * new Color(1.0f, 1.0f, 1.0f, 0.8f);
    private static Color gizmoLineColor_highlight = Color.green * new Color(1.0f, 1.0f, 1.0f, 1f);

    protected virtual Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected virtual Color GizmoLineColor { get { return gizmoLineColor; } }
    protected virtual Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected virtual Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    void Update ()
    {
        if (snapping)
        {
            Ray ray = new Ray(transform.position + Vector3.up * MapTool.PROXIMITY * 2, Vector3.down);
            RaycastHit hit = new RaycastHit();
            while (Physics.Raycast(ray, out hit, 1000.0f, lyrMask.value))
            {
                if (hit.collider.transform == transform)
                {
                    ray = new Ray(hit.point - Vector3.up * MapTool.PROXIMITY * 0.001f, Vector3.down);
                    continue;
                }

                if ((hit.point - lastMovePos).magnitude > 0.001f) //prevent self drifting
                {
                    transform.position = hit.point;
                    lastMovePos = hit.point;
                }

                break;
            }
        }      
    }

    private void Draw(bool highlight = false)
    {
        var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
        var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

        Map.Draw.Gizmos.DrawWaypoints(transform, new List<Vector3>() { Vector3.zero }, Map.MapTool.PROXIMITY * 0.5f, surfaceColor, lineColor);
    }

    protected virtual void OnDrawGizmos()
    {
        Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Draw(highlight: true);
    }
}
