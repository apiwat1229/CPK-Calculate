using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics;
using Windows.UI;

namespace CPK_Calculate
{
    public sealed partial class CPKResultWindow : Window
    {
        private CPKResultData? _data;

        public CPKResultWindow()
        {
            this.InitializeComponent();
            this.AppWindow.Resize(new SizeInt32(1100, 900));
            this.AppWindow.SetIcon("Assets/Square44x44Logo.scale-200.png");
        }

        public void LoadData(CPKResultData data)
        {
            _data = data;
            this.Title = string.IsNullOrEmpty(data.Title) ? "CPK Analysis Result" : data.Title;
            PopulateStatistics(data);
        }

        private void PopulateStatistics(CPKResultData data)
        {
            var r = data.Results;

            StatDateBadge.Text = data.Date;
            SpecInfo.Text = $"N = {data.Values.Count}  |  Mean = {r.Mean:F3}  |  LSL = {data.LSL:F2}  |  USL = {data.USL:F2}";

            // Within
            WithinStDev.Text = r.StdevWithin.ToString("F3");
            WithinCp.Text = r.Cp.ToString("F2");
            WithinCpk.Text = r.Cpk.ToString("F2");
            WithinPPM.Text = r.PpmWithin.ToString("F2");

            // Overall
            OverallStDev.Text = r.StdevOverall.ToString("F3");
            OverallPp.Text = r.Pp.ToString("F2");
            OverallPpk.Text = r.Ppk.ToString("F2");
            OverallCpm.Text = r.Cpm.ToString("F2");
            OverallPPM.Text = r.PpmOverall.ToString("F2");

            // Gauge bars (scale: max 2.0 = 100%)
            double cpkPct = Math.Clamp(r.Cpk / 2.0, 0, 1);
            double ppkPct = Math.Clamp(r.Ppk / 2.0, 0, 1);
            CpkBarValue.Text = r.Cpk.ToString("F2");
            PpkBarValue.Text = r.Ppk.ToString("F2");

            // Will be set properly after layout
            CpkBar.Tag = cpkPct;
            PpkBar.Tag = ppkPct;

            // Trigger bar width on loaded
            CpkBar.Loaded += (_, __) =>
            {
                if (CpkBar.Parent is Grid g)
                    CpkBar.Width = g.ActualWidth * cpkPct;
            };
            PpkBar.Loaded += (_, __) =>
            {
                if (PpkBar.Parent is Grid g)
                    PpkBar.Width = g.ActualWidth * ppkPct;
            };

            // X-bar labels
            double mean = r.Mean;
            double ucl = mean + 3 * r.StdevWithin;
            double lcl = mean - 3 * r.StdevWithin;
            XbarUCL.Text = $"UCL: {ucl:F3}";
            XbarCL.Text = $"CL: {mean:F3}";
            XbarLCL.Text = $"LCL: {lcl:F3}";
        }

        // ==================== HISTOGRAM ====================
        private void HistogramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_data == null || _data.Values.Count < 2) return;
            DrawHistogram(HistogramCanvas, _data);
        }

        private static void DrawHistogram(Canvas canvas, CPKResultData data)
        {
            canvas.Children.Clear();
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            if (w < 50 || h < 50) return;

            var values = data.Values;
            double mean = data.Results.Mean;
            double sdWithin = data.Results.StdevWithin;
            double sdOverall = data.Results.StdevOverall;

            // Determine bins using Sturges' rule
            int binCount = Math.Max(5, (int)Math.Ceiling(1 + 3.322 * Math.Log10(values.Count)));
            double min = values.Min();
            double max = values.Max();
            double range = max - min;
            if (range == 0) range = 1;

            // Extend range for curves
            double plotMin = mean - 4 * Math.Max(sdWithin, sdOverall);
            double plotMax = mean + 4 * Math.Max(sdWithin, sdOverall);
            plotMin = Math.Min(plotMin, min - range * 0.1);
            plotMax = Math.Max(plotMax, max + range * 0.1);

            double binWidth = range / binCount;
            var bins = new int[binCount];
            foreach (var v in values)
            {
                int idx = (int)((v - min) / binWidth);
                if (idx >= binCount) idx = binCount - 1;
                if (idx < 0) idx = 0;
                bins[idx]++;
            }

            double maxBinCount = bins.Max();
            double leftMargin = 40;
            double bottomMargin = 30;
            double chartW = w - leftMargin - 10;
            double chartH = h - bottomMargin - 10;
            double barW = chartW / binCount;

            // Scale factor: align normal curve peak with histogram peak
            double normalPeakWithin = 1.0 / (sdWithin * Math.Sqrt(2 * Math.PI));
            double normalPeakOverall = 1.0 / (sdOverall * Math.Sqrt(2 * Math.PI));
            double normalPeak = Math.Max(normalPeakWithin, normalPeakOverall);
            double histDensityMax = maxBinCount / (values.Count * binWidth);
            double curveScale = (histDensityMax > 0 && normalPeak > 0) ? histDensityMax / normalPeak : 1;

            // Draw Y-axis ticks
            for (int i = 0; i <= 4; i++)
            {
                double yVal = maxBinCount * i / 4.0;
                double y = 10 + chartH - (yVal / maxBinCount * chartH);
                var tick = new TextBlock
                {
                    Text = ((int)yVal).ToString(),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                Canvas.SetLeft(tick, 2);
                Canvas.SetTop(tick, y - 7);
                canvas.Children.Add(tick);

                var line = new Line
                {
                    X1 = leftMargin, Y1 = y,
                    X2 = leftMargin + chartW, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(line);
            }

            // Draw bars
            for (int i = 0; i < binCount; i++)
            {
                double barH = maxBinCount > 0 ? (bins[i] / maxBinCount * chartH) : 0;
                var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = Math.Max(1, barW - 2),
                    Height = barH,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 26, 188, 156)),
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(rect, leftMargin + i * barW + 1);
                Canvas.SetTop(rect, 10 + chartH - barH);
                canvas.Children.Add(rect);
            }

            // Draw X-axis labels
            for (int i = 0; i <= binCount; i += Math.Max(1, binCount / 6))
            {
                double val = min + i * binWidth;
                var label = new TextBlock
                {
                    Text = val.ToString("F1"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                Canvas.SetLeft(label, leftMargin + i * barW - 12);
                Canvas.SetTop(label, 10 + chartH + 4);
                canvas.Children.Add(label);
            }

            // Draw normal curves
            DrawNormalCurve(canvas, mean, sdOverall, plotMin, plotMax, min, binWidth, binCount,
                values.Count, maxBinCount, leftMargin, chartW, chartH,
                Color.FromArgb(255, 231, 76, 60), false); // Overall - red solid

            DrawNormalCurve(canvas, mean, sdWithin, plotMin, plotMax, min, binWidth, binCount,
                values.Count, maxBinCount, leftMargin, chartW, chartH,
                Color.FromArgb(255, 44, 62, 80), true); // Within - dark dashed
        }

        private static void DrawNormalCurve(Canvas canvas, double mean, double sd,
            double plotMin, double plotMax, double dataMin, double binWidth, int binCount,
            int totalCount, double maxBinCount, double leftMargin, double chartW, double chartH,
            Color color, bool isDashed)
        {
            if (sd <= 0) return;

            var points = new List<Windows.Foundation.Point>();
            int segments = 120;

            // Normal curve in histogram-count scale
            double normalScale = totalCount * binWidth;

            for (int i = 0; i <= segments; i++)
            {
                double x = dataMin + (binCount * binWidth) * i / segments;
                double z = (x - mean) / sd;
                double pdf = (1.0 / (sd * Math.Sqrt(2 * Math.PI))) * Math.Exp(-0.5 * z * z);
                double yVal = pdf * normalScale;

                double px = leftMargin + (x - dataMin) / (binCount * binWidth) * chartW;
                double py = 10 + chartH - (maxBinCount > 0 ? yVal / maxBinCount * chartH : 0);

                py = Math.Max(5, Math.Min(10 + chartH, py));
                points.Add(new Windows.Foundation.Point(px, py));
            }

            if (points.Count < 2) return;

            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };

            if (isDashed)
            {
                polyline.StrokeDashArray = new DoubleCollection { 4, 3 };
            }

            foreach (var pt in points)
                polyline.Points.Add(pt);

            canvas.Children.Add(polyline);
        }

        // ==================== X-BAR CHART ====================
        private void XbarCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_data == null || _data.Values.Count < 2) return;
            DrawXbarChart(XbarCanvas, _data);
        }

        private static void DrawXbarChart(Canvas canvas, CPKResultData data)
        {
            canvas.Children.Clear();
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            if (w < 50 || h < 50) return;

            var values = data.Values;
            double mean = data.Results.Mean;
            double sdWithin = data.Results.StdevWithin;
            double ucl = mean + 3 * sdWithin;
            double lcl = mean - 3 * sdWithin;

            // Compute subgroup means for x-bar chart
            int subSize = Math.Max(1, data.SubgroupSize);
            var subgroupMeans = new List<double>();
            for (int i = 0; i < values.Count; i += subSize)
            {
                var group = values.Skip(i).Take(subSize).ToList();
                subgroupMeans.Add(group.Average());
            }

            if (subgroupMeans.Count == 0) return;

            double dataMin = Math.Min(subgroupMeans.Min(), lcl);
            double dataMax = Math.Max(subgroupMeans.Max(), ucl);
            double range = dataMax - dataMin;
            if (range == 0) range = 1;
            double plotMin = dataMin - range * 0.15;
            double plotMax = dataMax + range * 0.15;

            double leftMargin = 50;
            double rightMargin = 10;
            double topMargin = 10;
            double bottomMargin = 30;
            double chartW = w - leftMargin - rightMargin;
            double chartH = h - topMargin - bottomMargin;

            double MapY(double val) => topMargin + chartH - ((val - plotMin) / (plotMax - plotMin) * chartH);
            double MapX(int idx) => leftMargin + (subgroupMeans.Count > 1
                ? (double)idx / (subgroupMeans.Count - 1) * chartW
                : chartW / 2);

            // Y-axis labels
            int yTicks = 5;
            for (int i = 0; i <= yTicks; i++)
            {
                double val = plotMin + (plotMax - plotMin) * i / yTicks;
                double y = MapY(val);
                var label = new TextBlock
                {
                    Text = val.ToString("F1"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, y - 7);
                canvas.Children.Add(label);

                var gridLine = new Line
                {
                    X1 = leftMargin, Y1 = y,
                    X2 = leftMargin + chartW, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);
            }

            // UCL line (red)
            var uclLine = new Line
            {
                X1 = leftMargin, Y1 = MapY(ucl),
                X2 = leftMargin + chartW, Y2 = MapY(ucl),
                Stroke = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)),
                StrokeThickness = 1.5
            };
            canvas.Children.Add(uclLine);

            // LCL line (red)
            var lclLine = new Line
            {
                X1 = leftMargin, Y1 = MapY(lcl),
                X2 = leftMargin + chartW, Y2 = MapY(lcl),
                Stroke = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)),
                StrokeThickness = 1.5
            };
            canvas.Children.Add(lclLine);

            // CL line (green)
            var clLine = new Line
            {
                X1 = leftMargin, Y1 = MapY(mean),
                X2 = leftMargin + chartW, Y2 = MapY(mean),
                Stroke = new SolidColorBrush(Color.FromArgb(255, 26, 188, 156)),
                StrokeThickness = 1.5
            };
            canvas.Children.Add(clLine);

            // Data polyline
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 26, 188, 156)),
                StrokeThickness = 1.5
            };

            for (int i = 0; i < subgroupMeans.Count; i++)
            {
                double px = MapX(i);
                double py = MapY(subgroupMeans[i]);
                polyline.Points.Add(new Windows.Foundation.Point(px, py));
            }
            canvas.Children.Add(polyline);

            // Data points (dots)
            for (int i = 0; i < subgroupMeans.Count; i++)
            {
                double px = MapX(i);
                double py = MapY(subgroupMeans[i]);

                bool outOfControl = subgroupMeans[i] > ucl || subgroupMeans[i] < lcl;
                var dot = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = outOfControl
                        ? new SolidColorBrush(Color.FromArgb(255, 231, 76, 60))
                        : new SolidColorBrush(Color.FromArgb(255, 26, 188, 156))
                };
                Canvas.SetLeft(dot, px - 3);
                Canvas.SetTop(dot, py - 3);
                canvas.Children.Add(dot);
            }

            // X-axis labels
            int labelStep = Math.Max(1, subgroupMeans.Count / 15);
            for (int i = 0; i < subgroupMeans.Count; i += labelStep)
            {
                double px = MapX(i);
                var label = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                Canvas.SetLeft(label, px - 5);
                Canvas.SetTop(label, topMargin + chartH + 4);
                canvas.Children.Add(label);
            }
        }
    }
}
