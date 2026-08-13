namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>
/// Segoe MDL2 Assets / Fluent glyph codes for operator UI icons.
/// Falls back to Segoe UI Symbol when MDL2 is unavailable.
/// </summary>
internal static class UiGlyphs
{
    public const string Settings = "\uE713";
    public const string Reader = "\uE968";
    public const string Hospital = "\uE80F";
    public const string CardType = "\uE8C7";
    public const string Batch = "\uE8FD";
    public const string Start = "\uE8FB";
    public const string End = "\uE8FC";
    public const string Current = "\uE8CB";
    public const string FactoryCard = "\uE8C7";
    public const string TargetCard = "\uE8A1";
    public const string Written = "\uE70F";
    public const string Success = "\uE73E";
    public const string Failed = "\uE711";
    public const string Remaining = "\uE81C";
    public const string Elapsed = "\uE823";
    public const string Log = "\uE8A5";
    public const string Connect = "\uE968";
    public const string Play = "\uE768";
    public const string Stop = "\uE71A";
    public const string PlaceCard = "\uE8C7";
    public const string Scanning = "\uE721";
    public const string Writing = "\uE70F";
    public const string Verifying = "\uE73E";
    public const string Registering = "\uE8F1";
    public const string Ready = "\uE73E";
    public const string Error = "\uE783";
    public const string ArrowDown = "\uE74B";

    private static Font? _iconFont;
    private static bool _resolved;

    public static Font IconFont(float sizeEm = 12f)
    {
        EnsureFont();
        return new Font(_iconFont!.FontFamily, sizeEm, FontStyle.Regular, GraphicsUnit.Point);
    }

    /// <summary>True when Segoe MDL2 / Fluent Icons are available.</summary>
    public static bool HasFluentIcons
    {
        get
        {
            EnsureFont();
            var name = _iconFont!.FontFamily.Name;
            return name.Contains("MDL2", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("Fluent", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static Label CreateIconLabel(string glyph, float sizeEm = 12f, Color? color = null) => new()
    {
        Text = glyph,
        Font = IconFont(sizeEm),
        ForeColor = color ?? UiColors.IconIdle,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0),
    };

    public static Image CreateGearImage(int sizePx, Color color)
    {
        var bmp = new Bitmap(sizePx, sizePx);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        // Opaque background — WinForms Button + PrintWindow often drop alpha glyphs.
        g.Clear(Color.White);
        var cx = sizePx / 2f;
        var cy = sizePx / 2f;
        using var pen = new Pen(color, Math.Max(2f, sizePx * 0.1f))
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };
        using var brush = new SolidBrush(color);
        var r = sizePx * 0.28f;
        g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
        // eight gear teeth as short radial ticks
        for (var i = 0; i < 8; i++)
        {
            var a = i * Math.PI / 4;
            var x1 = cx + (float)(Math.Cos(a) * r);
            var y1 = cy + (float)(Math.Sin(a) * r);
            var x2 = cx + (float)(Math.Cos(a) * (r + sizePx * 0.16f));
            var y2 = cy + (float)(Math.Sin(a) * (r + sizePx * 0.16f));
            g.DrawLine(pen, x1, y1, x2, y2);
        }
        var hole = sizePx * 0.1f;
        g.FillEllipse(brush, cx - hole, cy - hole, hole * 2, hole * 2);
        return bmp;
    }

    /// <summary>
    /// Vector speaker icon (Volume2 / VolumeX style) — same GDI+ approach as <see cref="CreateGearImage"/>.
    /// Opaque <paramref name="backColor"/> so WinForms Button renders reliably.
    /// </summary>
    public static Image CreateSpeakerImage(int sizePx, Color color, bool muted, Color? backColor = null)
    {
        var bmp = new Bitmap(sizePx, sizePx);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.Clear(backColor ?? Color.White);

        var pad = sizePx * 0.10f;
        var left = pad;
        var right = sizePx - pad;
        var midY = sizePx / 2f;
        var span = right - left;

        var bodyLeft = left;
        var bodyRight = left + span * 0.26f;
        var coneRight = left + span * 0.52f;
        var bodyHalf = sizePx * 0.14f;
        var coneHalf = sizePx * 0.32f;

        using var brush = new SolidBrush(color);
        using var body = new System.Drawing.Drawing2D.GraphicsPath();
        body.AddPolygon(
        [
            new PointF(bodyLeft, midY - bodyHalf),
            new PointF(bodyRight, midY - bodyHalf),
            new PointF(coneRight, midY - coneHalf),
            new PointF(coneRight, midY + coneHalf),
            new PointF(bodyRight, midY + bodyHalf),
            new PointF(bodyLeft, midY + bodyHalf),
        ]);
        g.FillPath(brush, body);

        var stroke = Math.Max(1.6f, sizePx * 0.11f);
        using var pen = new Pen(color, stroke)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };

        var waveOriginX = coneRight - sizePx * 0.02f;
        if (muted)
        {
            var x0 = waveOriginX + span * 0.12f;
            var x1 = right - pad * 0.2f;
            var y0 = midY - sizePx * 0.22f;
            var y1 = midY + sizePx * 0.22f;
            g.DrawLine(pen, x0, y0, x1, y1);
            g.DrawLine(pen, x0, y1, x1, y0);
        }
        else
        {
            // Two sound-wave arcs (Volume2).
            DrawSpeakerArc(g, pen, waveOriginX, midY, sizePx * 0.22f);
            DrawSpeakerArc(g, pen, waveOriginX, midY, sizePx * 0.36f);
        }

        return bmp;
    }

    private static void DrawSpeakerArc(Graphics g, Pen pen, float originX, float midY, float radius)
    {
        var rect = new RectangleF(originX - radius, midY - radius, radius * 2f, radius * 2f);
        // Open arc facing right (approx. -50° to +50°).
        g.DrawArc(pen, rect, -52f, 104f);
    }

    public static Image? CreateGlyphImage(string glyph, int sizePx, Color color)
    {
        try
        {
            EnsureFont();
            var bmp = new Bitmap(sizePx, sizePx);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var font = new Font(_iconFont!.FontFamily, sizePx * 0.72f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString(glyph, font, brush, new RectangleF(0, 0, sizePx, sizePx), sf);
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureFont()
    {
        if (_resolved)
            return;

        _resolved = true;
        try
        {
            _iconFont = new Font("Segoe MDL2 Assets", 12f);
            // Force glyph measure so missing font throws early.
            _ = TextRenderer.MeasureText(Settings, _iconFont);
        }
        catch
        {
            try
            {
                _iconFont = new Font("Segoe Fluent Icons", 12f);
            }
            catch
            {
                _iconFont = new Font("Segoe UI Symbol", 12f);
            }
        }
    }
}
