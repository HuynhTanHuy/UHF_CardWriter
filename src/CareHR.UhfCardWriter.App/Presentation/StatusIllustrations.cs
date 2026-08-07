namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>
/// Generates simple state illustrations (PictureBox swap) — CardWritter-inspired, not copied.
/// </summary>
internal static class StatusIllustrations
{
    private static readonly Dictionary<UiState, Image> Cache = new();

    public static Image ForState(UiState state)
    {
        if (Cache.TryGetValue(state, out var cached))
            return cached;

        var image = state switch
        {
            UiState.WaitingForCard or UiState.Scanning => DrawPlaceCard(),
            UiState.Writing => DrawWriting(),
            UiState.Verifying => DrawVerifying(),
            UiState.Registering => DrawRegistering(),
            UiState.Success or UiState.Done => DrawSuccess(),
            UiState.Failed => DrawError(),
            UiState.Busy => DrawBusy(),
            _ => DrawReady(),
        };

        Cache[state] = image;
        return image;
    }

    private static Image DrawReady()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.CareHrBlue);
        DrawHintCard(g, w, h, soft: true);
        DrawCaption(g, w, h, "Ready");
        g.Dispose();
        return bmp;
    }

    private static Image DrawPlaceCard()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.CareHrBlue);
        DrawHintCard(g, w, h, soft: false);
        // Down arrow cue
        using var pen = new Pen(UiColors.CareHrBlue, 3f);
        var cx = w / 2f;
        var ay = h * 0.22f;
        g.DrawLine(pen, cx, ay, cx, ay + 28);
        g.DrawLine(pen, cx - 10, ay + 18, cx, ay + 28);
        g.DrawLine(pen, cx + 10, ay + 18, cx, ay + 28);
        DrawCaption(g, w, h, "Place card on reader");
        g.Dispose();
        return bmp;
    }

    private static Image DrawWriting()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.Warning);
        DrawHintCard(g, w, h, soft: false);
        DrawPulseRings(g, w, h, UiColors.Warning);
        DrawCaption(g, w, h, "Writing data");
        g.Dispose();
        return bmp;
    }

    private static Image DrawVerifying()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.CareHrBlue);
        DrawHintCard(g, w, h, soft: false);
        DrawCheckBadge(g, w * 0.62f, h * 0.42f, UiColors.CareHrBlue, small: true);
        DrawCaption(g, w, h, "Verifying");
        g.Dispose();
        return bmp;
    }

    private static Image DrawRegistering()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.CareHrBlue);
        DrawHintCard(g, w, h, soft: false);
        using var pen = new Pen(UiColors.CareHrBlue, 2.5f);
        var cx = w * 0.68f;
        var cy = h * 0.40f;
        g.DrawEllipse(pen, cx - 14, cy - 14, 28, 28);
        g.DrawLine(pen, cx + 10, cy - 10, cx + 22, cy - 22);
        DrawCaption(g, w, h, "Registering");
        g.Dispose();
        return bmp;
    }

    private static Image DrawSuccess()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawSoftDisk(g, w / 2f, h * 0.42f, 58, Color.FromArgb(40, 46, 125, 50));
        DrawCheckBadge(g, w / 2f, h * 0.42f, UiColors.Success, small: false);
        DrawCaption(g, w, h, "Completed");
        g.Dispose();
        return bmp;
    }

    private static Image DrawError()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawSoftDisk(g, w / 2f, h * 0.42f, 58, Color.FromArgb(40, 198, 40, 40));
        using var brush = new SolidBrush(UiColors.Error);
        var cx = w / 2f;
        var cy = h * 0.42f;
        g.FillEllipse(brush, cx - 42, cy - 42, 84, 84);
        using var pen = new Pen(Color.White, 6f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        g.DrawLine(pen, cx - 18, cy - 18, cx + 18, cy + 18);
        g.DrawLine(pen, cx + 18, cy - 18, cx - 18, cy + 18);
        DrawCaption(g, w, h, "Error");
        g.Dispose();
        return bmp;
    }

    private static Image DrawBusy()
    {
        var bmp = BaseCanvas(out var g, out var w, out var h);
        DrawReaderPad(g, w, h, UiColors.Warning);
        DrawPulseRings(g, w, h, UiColors.Warning);
        DrawCaption(g, w, h, "Working…");
        g.Dispose();
        return bmp;
    }

    private static Bitmap BaseCanvas(out Graphics g, out int w, out int h)
    {
        w = 360;
        h = 220;
        var bmp = new Bitmap(w, h);
        g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.White);
        // Soft ground wash
        using var wash = new SolidBrush(Color.FromArgb(248, 250, 251));
        g.FillRectangle(wash, 0, 0, w, h);
        return bmp;
    }

    private static void DrawReaderPad(Graphics g, int w, int h, Color accent)
    {
        var padW = 160;
        var padH = 100;
        var x = (w - padW) / 2f;
        var y = h * 0.28f;
        using var shadow = new SolidBrush(Color.FromArgb(28, 0, 0, 0));
        g.FillRoundedRect(shadow, x + 4, y + 6, padW, padH, 14);
        using var fill = new SolidBrush(Color.FromArgb(250, 252, 253));
        using var border = new Pen(Color.FromArgb(210, 218, 222), 2f);
        g.FillRoundedRect(fill, x, y, padW, padH, 14);
        g.DrawRoundedRect(border, x, y, padW, padH, 14);
        // Antenna ring
        using var ring = new Pen(Color.FromArgb(120, accent), 2.5f);
        g.DrawEllipse(ring, x + 28, y + 18, padW - 56, padH - 36);
        using var ring2 = new Pen(Color.FromArgb(70, accent), 2f);
        g.DrawEllipse(ring2, x + 42, y + 28, padW - 84, padH - 56);
        using var core = new SolidBrush(accent);
        g.FillEllipse(core, x + padW / 2f - 6, y + padH / 2f - 6, 12, 12);
    }

    private static void DrawHintCard(Graphics g, int w, int h, bool soft)
    {
        var cardW = 88;
        var cardH = 54;
        var x = (w - cardW) / 2f;
        var y = h * 0.34f;
        var alpha = soft ? 90 : 220;
        using var fill = new SolidBrush(Color.FromArgb(alpha, UiColors.CareHrBlue));
        using var border = new Pen(Color.FromArgb(Math.Min(255, alpha + 20), UiColors.CareHrBlue), 1.5f);
        g.FillRoundedRect(fill, x, y, cardW, cardH, 8);
        g.DrawRoundedRect(border, x, y, cardW, cardH, 8);
        using var chip = new SolidBrush(Color.FromArgb(soft ? 60 : 180, Color.White));
        g.FillRectangle(chip, x + 12, y + 14, 18, 14);
    }

    private static void DrawPulseRings(Graphics g, int w, int h, Color color)
    {
        var cx = w / 2f;
        var cy = h * 0.42f;
        using var p1 = new Pen(Color.FromArgb(90, color), 2f);
        using var p2 = new Pen(Color.FromArgb(50, color), 2f);
        g.DrawEllipse(p1, cx - 70, cy - 50, 140, 100);
        g.DrawEllipse(p2, cx - 95, cy - 68, 190, 136);
    }

    private static void DrawCheckBadge(Graphics g, float cx, float cy, Color color, bool small)
    {
        var r = small ? 22 : 42;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
        using var pen = new Pen(Color.White, small ? 4f : 7f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };
        var s = small ? 0.55f : 1f;
        g.DrawLines(pen, new[]
        {
            new PointF(cx - 16 * s, cy),
            new PointF(cx - 4 * s, cy + 12 * s),
            new PointF(cx + 18 * s, cy - 14 * s),
        });
    }

    private static void DrawSoftDisk(Graphics g, float cx, float cy, float r, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
    }

    private static void DrawCaption(Graphics g, int w, int h, string text)
    {
        using var font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        using var brush = new SolidBrush(UiColors.TextSecondary);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, new RectangleF(12, h - 42, w - 24, 28), sf);
    }
}

internal static class GraphicsRoundExtensions
{
    public static void FillRoundedRect(this Graphics g, Brush brush, float x, float y, float w, float h, float r)
    {
        using var path = Rounded(x, y, w, h, r);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRect(this Graphics g, Pen pen, float x, float y, float w, float h, float r)
    {
        using var path = Rounded(x, y, w, h, r);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath Rounded(float x, float y, float w, float h, float r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = r * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
