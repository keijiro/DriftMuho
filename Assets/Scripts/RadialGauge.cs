using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class RadialGauge : VisualElement
{
    private float m_Value = 0f; // 0.0 to 1.0
    private float m_LineWidth = 12f;
    private Color m_GaugeColor = new Color(1f, 0.24f, 0f, 1f);     // Default red-orange for energy
    private Color m_BgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);    // Inactive track

    [UxmlAttribute]
    public float Value
    {
        get => m_Value;
        set { m_Value = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float LineWidth
    {
        get => m_LineWidth;
        set { m_LineWidth = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public Color GaugeColor
    {
        get => m_GaugeColor;
        set { m_GaugeColor = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public Color BgColor
    {
        get => m_BgColor;
        set { m_BgColor = value; MarkDirtyRepaint(); }
    }

    public RadialGauge()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = contentRect.width;
        float h = contentRect.height;
        if (w < 1f || h < 1f) return;

        var painter = ctx.painter2D;
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float radius = Mathf.Min(w, h) * 0.5f - m_LineWidth * 0.5f;
        if (radius < 1f) return;

        // Gauge arc: Start at 135 degrees (bottom-left) and sweep to 405 degrees (bottom-right)
        float startAngle = 135f;
        float endAngle = 405f;
        float currentEndAngle = Mathf.Lerp(startAngle, endAngle, m_Value);

        // 1. Draw Background track
        painter.lineWidth = m_LineWidth;
        painter.lineCap = LineCap.Round;
        painter.strokeColor = m_BgColor;

        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(startAngle), Angle.Degrees(endAngle), ArcDirection.Clockwise);
        painter.Stroke();

        // 2. Draw Gauge foreground fill
        if (m_Value > 0.005f)
        {
            painter.strokeColor = m_GaugeColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(startAngle), Angle.Degrees(currentEndAngle), ArcDirection.Clockwise);
            painter.Stroke();
        }
    }
}
