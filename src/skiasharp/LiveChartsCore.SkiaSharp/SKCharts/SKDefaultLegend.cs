﻿// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.SKCharts;

public class SKDefaultLegend : IChartLegend<SkiaSharpDrawingContext>, IImageControl
{
    private static readonly int s_zIndex = 10050;
    private IPaint<SkiaSharpDrawingContext>? _backgroundPaint;
    private StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext> _stackPanel = new()
    {
        Padding = new Padding(15, 4),
        HorizontalAlignment = Align.Start,
        VerticalAlignment = Align.Middle
    };

    /// <summary>
    /// Gets or sets the legend font paint.
    /// </summary>
    public IPaint<SkiaSharpDrawingContext>? FontPaint { get; set; }

    /// <summary>
    /// Gets or sets the background paint.
    /// </summary>
    public IPaint<SkiaSharpDrawingContext>? BackgroundPaint
    {
        get => _backgroundPaint;
        set
        {
            _backgroundPaint = value;
            if (value is not null)
            {
                value.IsFill = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the fonts size.
    /// </summary>
    public double TextSize { get; set; } = 15;

    public LvcSize Size { get; private set; }

    public SKDefaultLegend()
    {
        FontPaint = new SolidColorPaint(new SKColor(30, 30, 30, 255));
    }

    public void Draw(Chart<SkiaSharpDrawingContext> chart)
    {
        Measure(chart);

        var actualChartSize = chart.ControlSize;

        if (chart.LegendPosition == LegendPosition.Top)
        {
            chart.Canvas.StartPoint = new LvcPoint(0, Size.Height);
            _stackPanel.X = actualChartSize.Width * 0.5f - Size.Width * 0.5f;
            _stackPanel.Y = -Size.Height;
        }
        if (chart.LegendPosition == LegendPosition.Bottom)
        {
            _stackPanel.X = actualChartSize.Width * 0.5f - Size.Width * 0.5f;
            _stackPanel.Y = actualChartSize.Height;
        }
        if (chart.LegendPosition == LegendPosition.Left)
        {
            chart.Canvas.StartPoint = new LvcPoint(Size.Width, 0);
            _stackPanel.X = -Size.Width;
            _stackPanel.Y = actualChartSize.Height * 0.5f - Size.Height * 0.5f;
        }
        if (chart.LegendPosition == LegendPosition.Right)
        {
            _stackPanel.X = actualChartSize.Width; //- iDontKnowWhyThis;
            _stackPanel.Y = actualChartSize.Height * 0.5f - Size.Height * 0.5f;
        }

        chart.AddVisual(_stackPanel);
        if (chart.LegendPosition == LegendPosition.Hidden) chart.RemoveVisual(_stackPanel);
    }

    public void Measure(IChart chart)
    {
        var c = (Chart<SkiaSharpDrawingContext>)chart;
        BuildLayout(c);
        Size = _stackPanel.Measure(c);
    }

    private void BuildLayout(Chart<SkiaSharpDrawingContext> chart)
    {
        if (chart.View.LegendBackgroundPaint is not null) BackgroundPaint = chart.View.LegendBackgroundPaint;
        if (chart.View.LegendTextPaint is not null) FontPaint = chart.View.LegendTextPaint;
        if (chart.View.LegendTextSize is not null) TextSize = chart.View.LegendTextSize.Value;

        if (BackgroundPaint is not null) BackgroundPaint.ZIndex = s_zIndex;
        if (FontPaint is not null) FontPaint.ZIndex = s_zIndex + 1;

        _stackPanel.Orientation = chart.LegendPosition is LegendPosition.Left or LegendPosition.Right
            ? ContainerOrientation.Vertical
            : ContainerOrientation.Horizontal;

        if (_stackPanel.Orientation == ContainerOrientation.Horizontal)
        {
            _stackPanel.MaxWidth = chart.ControlSize.Width;
            _stackPanel.MaxHeight = double.MaxValue;
        }
        else
        {
            _stackPanel.MaxWidth = double.MaxValue;
            _stackPanel.MaxHeight = chart.ControlSize.Height;
        }

        foreach (var visual in _stackPanel.Children.ToArray())
        {
            _ = _stackPanel.Children.Remove(visual);
            chart.RemoveVisual(visual);
        }

        foreach (var series in chart.ChartSeries)
        {
            _ = _stackPanel.Children.Add(new StackPanel<RectangleGeometry, SkiaSharpDrawingContext>
            {
                Padding = new Padding(12, 6),
                VerticalAlignment = Align.Middle,
                HorizontalAlignment = Align.Middle,
                Children =
                {
                    series.GetMiniatresSketch().AsDrawnControl(),
                    new LabelVisual
                    {
                        Text = series.Name ?? string.Empty,
                        Paint = FontPaint,
                        TextSize = TextSize,
                        Padding = new Padding(8, 0, 0, 0),
                        VerticalAlignment = Align.Start,
                        HorizontalAlignment = Align.Start
                    }
                }
            });
        }
    }
}
