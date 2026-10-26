using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class BigBallIcon : MaskableGraphic {
    [SerializeField] bool activeMode;
    public void SetActiveMode(bool value) { if (activeMode == value) return; activeMode = value; SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh) {
        mesh.Clear();
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .43f;
        Disc(mesh, center, radius, new Color(1,.3f,.2f,activeMode ? .32f : .16f), 64);
        Color outline = activeMode ? new Color(1,.78f,.22f,1) : new Color(1,1,1,.8f);
        for (int i = 0; i < 24; i++) {
            float angle = i * Mathf.PI * 2 / 24;
            Disc(mesh, center + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius, 1.2f, outline, 8);
        }
        Disc(mesh, center, radius / 5f, new Color(.96f,.2f,.12f,1), 24);
    }
    static void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint, int segments) {
        int start = mesh.currentVertCount;
        mesh.AddVert(center,tint,Vector2.zero);
        for (int i=0;i<=segments;i++) {
            float angle=i*Mathf.PI*2/segments;
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);
        }
        for(int i=0;i<segments;i++)mesh.AddTriangle(start,start+i+1,start+i+2);
    }
}
