using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Minimal SVG rasterizer drawn with GDI+ (no external packages).
    /// Supports flat artwork: rect/circle/ellipse/line/polyline/polygon/path
    /// (all commands incl. arcs), nested groups, translate/scale/rotate/matrix
    /// transforms, fill/stroke/opacity styling via attributes or style="".
    /// </summary>
    public sealed class SvgImage
    {
        private readonly XElement _root;
        private readonly double _vbX, _vbY, _vbW, _vbH;

        public double Width { get { return _vbW; } }
        public double Height { get { return _vbH; } }

        private SvgImage(XElement root)
        {
            _root = root;
            _vbW = 300; _vbH = 150;
            string vb = Att(root, "viewBox");
            if (vb != null)
            {
                var p = SplitDoubles(vb);
                if (p.Count >= 4)
                {
                    _vbX = p[0]; _vbY = p[1]; _vbW = p[2]; _vbH = p[3];
                    if (_vbW <= 0 || _vbH <= 0) { _vbW = 300; _vbH = 150; }
                    return;
                }
            }
            double w, h;
            if (TryDouble(Att(root, "width"), out w)) _vbW = w;
            if (TryDouble(Att(root, "height"), out h)) _vbH = h;
        }

        public static SvgImage Parse(string xml)
        {
            return new SvgImage(XDocument.Parse(xml).Root);
        }

        public static SvgImage FromFile(string path)
        {
            return Parse(File.ReadAllText(path));
        }

        public static SvgImage FromEmbeddedResource(string manifestName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(manifestName))
            {
                if (s == null)
                    throw new ArgumentException("Embedded SVG not found: " + manifestName);
                using (var r = new StreamReader(s))
                    return Parse(r.ReadToEnd());
            }
        }

        /// <summary>Renders preserving the aspect ratio (like xMidYMid meet).</summary>
        public Bitmap Render(int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                double scale = Math.Min(width / _vbW, height / _vbH);
                float tx = (float)((width - _vbW * scale) / 2.0 - _vbX * scale);
                float ty = (float)((height - _vbH * scale) / 2.0 - _vbY * scale);
                using (var page = new Matrix((float)scale, 0f, 0f, (float)scale, tx, ty))
                {
                    g.Transform = page;
                    using (var identity = new Matrix())
                        Walk(_root, g, identity, Style.Default());
                }
            }
            return bmp;
        }

        /// <summary>Renders at the given width, height derived from the aspect ratio.</summary>
        public Bitmap Render(int width)
        {
            int h = (int)Math.Round(width * _vbH / _vbW);
            return Render(width, h);
        }

        // ===== document walk =====

        private static void Walk(XElement el, Graphics g, Matrix xform, Style parent)
        {
            Style st = ApplyStyle(el, parent);
            if (!st.Visible) return;
            using (Matrix tf = CombineTransform(el, xform))
            {
                switch (el.Name.LocalName)
                {
                    case "svg":
                    case "g":
                    case "a":
                        foreach (XElement child in el.Elements())
                            Walk(child, g, tf, st);
                        break;
                    case "rect":
                    case "circle":
                    case "ellipse":
                    case "line":
                    case "polyline":
                    case "polygon":
                    case "path":
                        DrawShape(el, g, tf, st);
                        break;
                    // title/desc/defs/metadata etc. are skipped
                }
            }
        }

        private static void DrawShape(XElement el, Graphics g, Matrix tf, Style st)
        {
            var fillMode = st.EvenOdd ? FillMode.Alternate : FillMode.Winding;
            using (var gp = new GraphicsPath(fillMode))
            {
                switch (el.Name.LocalName)
                {
                    case "rect": AddRect(gp, el); break;
                    case "circle": AddCircle(gp, el); break;
                    case "ellipse": AddEllipse(gp, el); break;
                    case "line": AddLine(gp, el); break;
                    case "polyline":
                    case "polygon": AddPoly(gp, el, el.Name.LocalName == "polygon"); break;
                    case "path": AddPathData(gp, Att(el, "d")); break;
                }
                if (gp.PointCount == 0) return;
                gp.Transform(tf);

                if (st.Fill.HasValue)
                {
                    using (var b = new SolidBrush(ApplyAlpha(st.Fill.Value, st.Opacity * st.FillOpacity)))
                        g.FillPath(b, gp);
                }
                if (st.Stroke.HasValue && st.StrokeWidth > 0)
                {
                    // stroke-width lives in user units: compensate for the element transform
                    float scale = UserScale(tf);
                    using (var pen = new Pen(ApplyAlpha(st.Stroke.Value, st.Opacity * st.StrokeOpacity),
                                            (float)(st.StrokeWidth * scale)))
                    {
                        pen.StartCap = pen.EndCap = ToLineCap(st.LineCap);
                        pen.LineJoin = ToLineJoin(st.LineJoin);
                        g.DrawPath(pen, gp);
                    }
                }
            }
        }

        // ===== shape builders (local coordinates) =====

        private static void AddRect(GraphicsPath gp, XElement el)
        {
            double x = D(Att(el, "x")), y = D(Att(el, "y"));
            double w = D(Att(el, "width")), h = D(Att(el, "height"));
            if (w <= 0 || h <= 0) return;
            double rx = Att(el, "rx") != null ? D(Att(el, "rx")) : D(Att(el, "ry"));
            double ry = Att(el, "ry") != null ? D(Att(el, "ry")) : rx;
            if (rx <= 0 || ry <= 0) { gp.AddRectangle(new RectangleF((float)x, (float)y, (float)w, (float)h)); return; }
            if (rx > w / 2) rx = w / 2;
            if (ry > h / 2) ry = h / 2;
            gp.AddArc((float)x, (float)y, (float)(2 * rx), (float)(2 * ry), 180, 90);
            gp.AddArc((float)(x + w - 2 * rx), (float)y, (float)(2 * rx), (float)(2 * ry), 270, 90);
            gp.AddArc((float)(x + w - 2 * rx), (float)(y + h - 2 * ry), (float)(2 * rx), (float)(2 * ry), 0, 90);
            gp.AddArc((float)x, (float)(y + h - 2 * ry), (float)(2 * rx), (float)(2 * ry), 90, 90);
            gp.CloseFigure();
        }

        private static void AddCircle(GraphicsPath gp, XElement el)
        {
            double r = D(Att(el, "r"));
            if (r <= 0) return;
            AddEllipseXY(gp, D(Att(el, "cx")) - r, D(Att(el, "cy")) - r, 2 * r, 2 * r);
        }

        private static void AddEllipse(GraphicsPath gp, XElement el)
        {
            double rx = D(Att(el, "rx")), ry = D(Att(el, "ry"));
            if (rx <= 0 || ry <= 0) return;
            AddEllipseXY(gp, D(Att(el, "cx")) - rx, D(Att(el, "cy")) - ry, 2 * rx, 2 * ry);
        }

        private static void AddEllipseXY(GraphicsPath gp, double x, double y, double w, double h)
        {
            gp.AddEllipse((float)x, (float)y, (float)w, (float)h);
            gp.CloseFigure();
        }

        private static void AddLine(GraphicsPath gp, XElement el)
        {
            gp.AddLine((float)D(Att(el, "x1")), (float)D(Att(el, "y1")),
                       (float)D(Att(el, "x2")), (float)D(Att(el, "y2")));
        }

        private static void AddPoly(GraphicsPath gp, XElement el, bool close)
        {
            var p = SplitDoubles(Att(el, "points"));
            if (p.Count < 4 || p.Count % 2 != 0) return;
            var pts = new PointF[p.Count / 2];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new PointF((float)p[2 * i], (float)p[2 * i + 1]);
            gp.AddLines(pts);
            if (close) gp.CloseFigure();
        }

        // ===== path data =====

        private static void AddPathData(GraphicsPath gp, string d)
        {
            if (string.IsNullOrEmpty(d)) return;
            var sc = new Scanner(d);
            double cx = 0, cy = 0;          // current point
            double sx = 0, sy = 0;          // subpath start
            double lastCx = 0, lastCy = 0;  // previous cubic control (for S)
            double lastQx = 0, lastQy = 0;  // previous quad control (for T)
            bool prevCubic = false, prevQuad = false;
            char cmd = '\0';

            while (sc.HasMore)
            {
                char? next = sc.PeekCommand();
                if (next.HasValue) { cmd = next.Value; sc.TakeCommand(); }
                else if (cmd == 'M') cmd = 'L';
                else if (cmd == 'm') cmd = 'l';

                switch (char.ToUpperInvariant(cmd))
                {
                    case 'M':
                        {
                            bool rel = cmd == 'm';
                            double x = sc.Number() + (rel ? cx : 0);
                            double y = sc.Number() + (rel ? cy : 0);
                            gp.StartFigure();
                            cx = sx = x; cy = sy = y;
                            prevCubic = prevQuad = false;
                            break;
                        }
                    case 'L':
                        {
                            bool rel = cmd == 'l';
                            double x = sc.Number() + (rel ? cx : 0);
                            double y = sc.Number() + (rel ? cy : 0);
                            gp.AddLine((float)cx, (float)cy, (float)x, (float)y);
                            cx = x; cy = y;
                            prevCubic = prevQuad = false;
                            break;
                        }
                    case 'H':
                        {
                            double x = sc.Number() + (cmd == 'h' ? cx : 0);
                            gp.AddLine((float)cx, (float)cy, (float)x, (float)cy);
                            cx = x;
                            prevCubic = prevQuad = false;
                            break;
                        }
                    case 'V':
                        {
                            double y = sc.Number() + (cmd == 'v' ? cy : 0);
                            gp.AddLine((float)cx, (float)cy, (float)cx, (float)y);
                            cy = y;
                            prevCubic = prevQuad = false;
                            break;
                        }
                    case 'C':
                        {
                            bool rel = cmd == 'c';
                            double x1 = sc.Number() + (rel ? cx : 0), y1 = sc.Number() + (rel ? cy : 0);
                            double x2 = sc.Number() + (rel ? cx : 0), y2 = sc.Number() + (rel ? cy : 0);
                            double x = sc.Number() + (rel ? cx : 0), y = sc.Number() + (rel ? cy : 0);
                            gp.AddBezier((float)cx, (float)cy, (float)x1, (float)y1, (float)x2, (float)y2, (float)x, (float)y);
                            lastCx = x2; lastCy = y2;
                            cx = x; cy = y;
                            prevCubic = true; prevQuad = false;
                            break;
                        }
                    case 'S':
                        {
                            bool rel = cmd == 's';
                            double x2 = sc.Number() + (rel ? cx : 0), y2 = sc.Number() + (rel ? cy : 0);
                            double x = sc.Number() + (rel ? cx : 0), y = sc.Number() + (rel ? cy : 0);
                            double x1 = prevCubic ? 2 * cx - lastCx : cx;
                            double y1 = prevCubic ? 2 * cy - lastCy : cy;
                            gp.AddBezier((float)cx, (float)cy, (float)x1, (float)y1, (float)x2, (float)y2, (float)x, (float)y);
                            lastCx = x2; lastCy = y2;
                            cx = x; cy = y;
                            prevCubic = true; prevQuad = false;
                            break;
                        }
                    case 'Q':
                        {
                            bool rel = cmd == 'q';
                            double x1 = sc.Number() + (rel ? cx : 0), y1 = sc.Number() + (rel ? cy : 0);
                            double x = sc.Number() + (rel ? cx : 0), y = sc.Number() + (rel ? cy : 0);
                            gp.AddBezier((float)cx, (float)cy,
                                         (float)(cx + 2.0 / 3.0 * (x1 - cx)), (float)(cy + 2.0 / 3.0 * (y1 - cy)),
                                         (float)(x + 2.0 / 3.0 * (x1 - x)), (float)(y + 2.0 / 3.0 * (y1 - y)),
                                         (float)x, (float)y);
                            lastQx = x1; lastQy = y1;
                            cx = x; cy = y;
                            prevQuad = true; prevCubic = false;
                            break;
                        }
                    case 'T':
                        {
                            bool rel = cmd == 't';
                            double x = sc.Number() + (rel ? cx : 0), y = sc.Number() + (rel ? cy : 0);
                            double x1 = prevQuad ? 2 * cx - lastQx : cx;
                            double y1 = prevQuad ? 2 * cy - lastQy : cy;
                            gp.AddBezier((float)cx, (float)cy,
                                         (float)(cx + 2.0 / 3.0 * (x1 - cx)), (float)(cy + 2.0 / 3.0 * (y1 - cy)),
                                         (float)(x + 2.0 / 3.0 * (x1 - x)), (float)(y + 2.0 / 3.0 * (y1 - y)),
                                         (float)x, (float)y);
                            lastQx = x1; lastQy = y1;
                            cx = x; cy = y;
                            prevQuad = true; prevCubic = false;
                            break;
                        }
                    case 'A':
                        {
                            bool rel = cmd == 'a';
                            double rx = sc.Number(), ry = sc.Number();
                            double angle = sc.Number();
                            bool large = sc.Flag(), sweep = sc.Flag();
                            double x = sc.Number() + (rel ? cx : 0);
                            double y = sc.Number() + (rel ? cy : 0);
                            AddArcAsBeziers(gp, cx, cy, rx, ry, angle, large, sweep, x, y);
                            cx = x; cy = y;
                            prevCubic = prevQuad = false;
                            break;
                        }
                    case 'Z':
                        gp.CloseFigure();
                        cx = sx; cy = sy;
                        prevCubic = prevQuad = false;
                        break;
                    default:
                        return; // unknown command: stop rather than mis-parse
                }
            }
        }

        /// <summary>SVG endpoint-parameterized arc approximated with cubic beziers.</summary>
        private static void AddArcAsBeziers(GraphicsPath gp, double x1, double y1,
            double rx, double ry, double angleDeg, bool large, bool sweep, double x2, double y2)
        {
            if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2))
            {
                gp.AddLine((float)x1, (float)y1, (float)x2, (float)y2);
                return;
            }
            rx = Math.Abs(rx); ry = Math.Abs(ry);
            double phi = angleDeg * Math.PI / 180.0;
            double cosP = Math.Cos(phi), sinP = Math.Sin(phi);
            double dx2 = (x1 - x2) / 2.0, dy2 = (y1 - y2) / 2.0;
            double x1p = cosP * dx2 + sinP * dy2;
            double y1p = -sinP * dx2 + cosP * dy2;

            double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
            if (lambda > 1)
            {
                double s = Math.Sqrt(lambda);
                rx *= s; ry *= s;
            }

            double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
            double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
            double co = Math.Sqrt(Math.Max(0.0, num / den));
            if (large == sweep) co = -co;
            double vxp = co * rx * y1p / ry;
            double vyp = -co * ry * x1p / rx;
            double cxp = cosP * vxp - sinP * vyp + (x1 + x2) / 2.0;
            double cyp = sinP * vxp + cosP * vyp + (y1 + y2) / 2.0;

            double ux = (x1p - vxp) / rx, uy = (y1p - vyp) / ry;
            double vx = (-x1p - vxp) / rx, vy = (-y1p - vyp) / ry;
            double theta1 = Angle(1, 0, ux, uy);
            double dTheta = Angle(ux, uy, vx, vy);
            if (!sweep && dTheta > 0) dTheta -= 2 * Math.PI;
            if (sweep && dTheta < 0) dTheta += 2 * Math.PI;

            int segments = (int)Math.Ceiling(Math.Abs(dTheta) / (Math.PI / 2.0));
            if (segments < 1) segments = 1;
            double delta = dTheta / segments;
            double t = 4.0 / 3.0 * Math.Tan(delta / 4.0);

            double th = theta1;
            double px = cxp + rx * Math.Cos(th) * cosP - ry * Math.Sin(th) * sinP;
            double py = cyp + rx * Math.Cos(th) * sinP + ry * Math.Sin(th) * cosP;

            for (int i = 0; i < segments; i++)
            {
                double th2 = th + delta;
                double cosT1 = Math.Cos(th), sinT1 = Math.Sin(th);
                double cosT2 = Math.Cos(th2), sinT2 = Math.Sin(th2);

                // derivative endpoints in ellipse space, then rotate by phi
                double d1x = -rx * sinT1, d1y = ry * cosT1;
                double d2x = -rx * sinT2, d2y = ry * cosT2;
                double c1x = cxp + rx * cosT1 * cosP - ry * sinT1 * sinP + t * (d1x * cosP - d1y * sinP);
                double c1y = cyp + rx * cosT1 * sinP + ry * sinT1 * cosP + t * (d1x * sinP + d1y * cosP);
                double c2x = cxp + rx * cosT2 * cosP - ry * sinT2 * sinP - t * (d2x * cosP - d2y * sinP);
                double c2y = cyp + rx * cosT2 * sinP + ry * sinT2 * cosP - t * (d2x * sinP + d2y * cosP);
                double ex = cxp + rx * cosT2 * cosP - ry * sinT2 * sinP;
                double ey = cyp + rx * cosT2 * sinP + ry * sinT2 * cosP;

                gp.AddBezier((float)px, (float)py, (float)c1x, (float)c1y,
                             (float)c2x, (float)c2y, (float)ex, (float)ey);
                th = th2; px = ex; py = ey;
            }
        }

        private static double Angle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            double a = Math.Acos(Math.Min(1, Math.Max(-1, dot / len)));
            if (ux * vy - uy * vx < 0) a = -a;
            return a;
        }

        // ===== transforms =====

        private static Matrix CombineTransform(XElement el, Matrix parent)
        {
            Matrix m = parent.IsIdentity ? new Matrix() : parent.Clone();
            string t = Att(el, "transform");
            if (t != null)
            {
                using (Matrix local = ParseTransform(t))
                    m.Multiply(local, MatrixOrder.Append);
            }
            return m;
        }

        private static Matrix ParseTransform(string value)
        {
            var m = new Matrix();
            int i = 0;
            while (i < value.Length)
            {
                int open = value.IndexOf('(', i);
                if (open < 0) break;
                string name = value.Substring(i, open - i).Trim();
                int close = value.IndexOf(')', open);
                if (close < 0) break;
                var a = SplitDoubles(value.Substring(open + 1, close - open - 1));

                Matrix step = null;
                switch (name)
                {
                    case "translate":
                        {
                            float tx = (float)(a.Count > 0 ? a[0] : 0);
                            float ty = (float)(a.Count > 1 ? a[1] : 0);
                            step = new Matrix(1f, 0f, 0f, 1f, tx, ty);
                            break;
                        }
                    case "scale":
                        {
                            float sx = (float)(a.Count > 0 ? a[0] : 1);
                            float sy = (float)(a.Count > 1 ? a[1] : sx);
                            step = new Matrix(sx, 0f, 0f, sy, 0f, 0f);
                            break;
                        }
                    case "rotate":
                        {
                            float deg = (float)(a.Count > 0 ? a[0] : 0);
                            if (a.Count >= 3)
                            {
                                var t1 = new Matrix(1f, 0f, 0f, 1f, (float)a[1], (float)a[2]);
                                var r = new Matrix(); r.Rotate(deg);
                                var t2 = new Matrix(1f, 0f, 0f, 1f, -(float)a[1], -(float)a[2]);
                                t1.Multiply(r, MatrixOrder.Append);
                                t1.Multiply(t2, MatrixOrder.Append);
                                step = t1;
                                r.Dispose();
                                t2.Dispose();
                            }
                            else
                            {
                                step = new Matrix();
                                step.Rotate(deg);
                            }
                            break;
                        }
                    case "matrix":
                        if (a.Count >= 6)
                            step = new Matrix((float)a[0], (float)a[1], (float)a[2],
                                              (float)a[3], (float)a[4], (float)a[5]);
                        break;
                }
                if (step != null)
                {
                    m.Multiply(step, MatrixOrder.Append);
                    step.Dispose();
                }
                i = close + 1;
            }
            return m;
        }

        // ===== styling =====

        private sealed class Style
        {
            public Color? Fill;
            public Color? Stroke;
            public double StrokeWidth = 1;
            public double Opacity = 1, FillOpacity = 1, StrokeOpacity = 1;
            public string LineCap = "butt";
            public string LineJoin = "miter";
            public bool EvenOdd;
            public bool Visible = true;

            public static Style Default()
            {
                return new Style { Fill = Color.Black };
            }

            public Style Clone()
            {
                return (Style)MemberwiseClone();
            }
        }

        private static Style ApplyStyle(XElement el, Style parent)
        {
            Style st = parent.Clone();
            var map = new Dictionary<string, string>();
            foreach (XAttribute a in el.Attributes())
                map[a.Name.LocalName] = a.Value;
            string css = Att(el, "style");
            if (css != null)
            {
                foreach (string pair in css.Split(';'))
                {
                    int c = pair.IndexOf(':');
                    if (c > 0)
                        map[pair.Substring(0, c).Trim()] = pair.Substring(c + 1).Trim();
                }
            }

            string v;
            if (map.TryGetValue("fill", out v)) st.Fill = ParsePaint(v);
            if (map.TryGetValue("stroke", out v)) st.Stroke = ParsePaint(v);
            if (map.TryGetValue("stroke-width", out v)) st.StrokeWidth = D(v);
            if (map.TryGetValue("opacity", out v)) st.Opacity = Clamp01(D(v));
            if (map.TryGetValue("fill-opacity", out v)) st.FillOpacity = Clamp01(D(v));
            if (map.TryGetValue("stroke-opacity", out v)) st.StrokeOpacity = Clamp01(D(v));
            if (map.TryGetValue("stroke-linecap", out v)) st.LineCap = v;
            if (map.TryGetValue("stroke-linejoin", out v)) st.LineJoin = v;
            if (map.TryGetValue("fill-rule", out v)) st.EvenOdd = v == "evenodd";
            if (map.TryGetValue("display", out v) && v == "none") st.Visible = false;
            if (map.TryGetValue("visibility", out v) && v == "hidden") st.Visible = false;
            return st;
        }

        private static Color? ParsePaint(string value)
        {
            if (value == null) return null;
            value = value.Trim();
            if (value.Length == 0 || value == "none") return null;
            if (value.StartsWith("#") && value.Length == 9) // #RRGGBBAA
            {
                int r = Hex(value, 1), g = Hex(value, 3), b = Hex(value, 5), a = Hex(value, 7);
                return Color.FromArgb(a, r, g, b);
            }
            if (value.StartsWith("rgb(") && value.EndsWith(")"))
            {
                var p = value.Substring(4, value.Length - 5).Split(',');
                if (p.Length >= 3)
                    return Color.FromArgb(int.Parse(p[0].Trim()), int.Parse(p[1].Trim()), int.Parse(p[2].Trim()));
            }
            return ColorTranslator.FromHtml(value);
        }

        // ===== helpers =====

        private static string Att(XElement el, string name)
        {
            XAttribute a = el.Attribute(name);
            if (a != null) return a.Value;
            XAttribute ns = el.Attribute(XName.Get(name, "http://www.w3.org/2000/svg"));
            return ns != null ? ns.Value : null;
        }

        private static Color ApplyAlpha(Color c, double opacity)
        {
            if (opacity >= 1) return c;
            return Color.FromArgb((int)Math.Round(c.A * Clamp01(opacity)), c.R, c.G, c.B);
        }

        private static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }

        private static LineCap ToLineCap(string v)
        {
            switch (v)
            {
                case "round": return LineCap.Round;
                case "square": return LineCap.Square;
                default: return LineCap.Flat;
            }
        }

        private static LineJoin ToLineJoin(string v)
        {
            switch (v)
            {
                case "round": return LineJoin.Round;
                case "bevel": return LineJoin.Bevel;
                default: return LineJoin.Miter;
            }
        }

        private static float UserScale(Matrix m)
        {
            float[] e = m.Elements;
            double det = Math.Abs(e[0] * e[3] - e[1] * e[2]);
            return (float)Math.Sqrt(det);
        }

        private static double D(string value)
        {
            double d;
            return TryDouble(value, out d) ? d : 0;
        }

        private static bool TryDouble(string value, out double d)
        {
            d = 0;
            if (string.IsNullOrEmpty(value)) return false;
            return double.TryParse(value.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out d);
        }

        private static int Hex(string s, int start)
        {
            return Convert.ToInt32(s.Substring(start, 2), 16);
        }

        private static List<double> SplitDoubles(string value)
        {
            var list = new List<double>();
            if (value == null) return list;
            var sc = new Scanner(value);
            while (sc.HasMore)
                list.Add(sc.Number());
            return list;
        }

        /// <summary>Tokenizes numbers and letters from SVG geometry/transform lists.</summary>
        private sealed class Scanner
        {
            private readonly string _s;
            private int _i;

            public Scanner(string s) { _s = s; }

            public bool HasMore
            {
                get
                {
                    Skip();
                    return _i < _s.Length;
                }
            }

            public char? PeekCommand()
            {
                Skip();
                if (_i < _s.Length && char.IsLetter(_s[_i])) return _s[_i];
                return null;
            }

            public char TakeCommand()
            {
                Skip();
                return _s[_i++];
            }

            /// <summary>Reads an arc flag: a single 0/1 digit (no sign/decimal).</summary>
            public bool Flag()
            {
                Skip();
                char c = _s[_i++];
                return c == '1';
            }

            public double Number()
            {
                Skip();
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
                bool dot = false;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (char.IsDigit(c)) { _i++; }
                    else if (c == '.' && !dot) { dot = true; _i++; }
                    else if ((c == 'e' || c == 'E') && _i + 1 < _s.Length &&
                             (char.IsDigit(_s[_i + 1]) || _s[_i + 1] == '-' || _s[_i + 1] == '+'))
                    {
                        _i++;
                        if (_s[_i] == '-' || _s[_i] == '+') _i++;
                    }
                    else break;
                }
                double d;
                if (!double.TryParse(_s.Substring(start, _i - start), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out d))
                    throw new FormatException("Invalid number at '" + _s.Substring(start) + "'");
                return d;
            }

            private void Skip()
            {
                while (_i < _s.Length && (char.IsWhiteSpace(_s[_i]) || _s[_i] == ','))
                    _i++;
            }
        }
    }
}
