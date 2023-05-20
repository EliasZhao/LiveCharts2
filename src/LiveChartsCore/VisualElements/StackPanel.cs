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

using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;

namespace LiveChartsCore.VisualElements;

/// <summary>
/// Defines the stack panel class.
/// </summary>
public class StackPanel<TBackgroundGeometry, TDrawingContext> : VisualElement<TDrawingContext>
    where TDrawingContext : DrawingContext
    where TBackgroundGeometry : ISizedGeometry<TDrawingContext>, new()
{
    private IPaint<TDrawingContext>? _backgroundPaint;
    private readonly TBackgroundGeometry _boundsGeometry = new();

    /// <summary>
    /// Gets the children collection.
    /// </summary>
    public HashSet<VisualElement<TDrawingContext>> Children { get; } = new();

    /// <summary>
    /// Gets or sets the panel orientation.
    /// </summary>
    public ContainerOrientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment.
    /// </summary>
    public Align VerticalAlignment { get; set; } = Align.Middle;

    /// <summary>
    /// Gets or sets the horizontal alignment.
    /// </summary>
    public Align HorizontalAlignment { get; set; } = Align.Middle;

    /// <summary>
    /// Gets or sets the padding.
    /// </summary>
    public Padding Padding { get; set; } = new();

    /// <summary>
    /// Gets or sets the background paint.
    /// </summary>
    public IPaint<TDrawingContext>? BackgroundPaint
    {
        get => _backgroundPaint;
        set => SetPaintProperty(ref _backgroundPaint, value);
    }

    /// <summary>
    /// Gets or sets the maximum width. When the maximum with is reached, a new row is created.
    /// </summary>
    public double MaxWidth { get; set; } = double.MaxValue;

    /// <summary>
    /// Gets or sets the maximum height. When the maximum height is reached, a new column is created.
    /// </summary>
    public double MaxHeight { get; set; } = double.MaxValue;

    internal override IPaint<TDrawingContext>?[] GetPaintTasks()
    {
        return new[] { _backgroundPaint };
    }

    internal override IAnimatable?[] GetDrawnGeometries()
    {
        return new IAnimatable?[] { _boundsGeometry };
    }

    /// <inheritdoc cref="VisualElement{TDrawingContext}.OnInvalidated(Chart{TDrawingContext})"/>
    protected internal override void OnInvalidated(Chart<TDrawingContext> chart)
    {
        var controlSize = Measure(chart);

        // NOTE #20231605
        // force the background to have at least an invisible geometry
        // we use this geometry in the motion canvas to track the position
        // of the stack panel as the time and animations elapse.
        BackgroundPaint ??= LiveCharts.DefaultSettings
                .GetProvider<TDrawingContext>()
                .GetSolidColorPaint(new LvcColor(0, 0, 0, 0));

        chart.Canvas.AddDrawableTask(BackgroundPaint);
        _boundsGeometry.X = (float)X;
        _boundsGeometry.Y = (float)Y;
        _boundsGeometry.Width = controlSize.Width;
        _boundsGeometry.Height = controlSize.Height;
        BackgroundPaint.AddGeometryToPaintTask(chart.Canvas, _boundsGeometry);
    }

    /// <inheritdoc cref="VisualElement{TDrawingContext}.SetParent(IGeometry{TDrawingContext})"/>
    protected internal override void SetParent(IGeometry<TDrawingContext> parent)
    {
        if (_boundsGeometry is null) return;
        _boundsGeometry.Parent = parent;
    }

    /// <inheritdoc cref="VisualElement{TDrawingContext}.Measure(Chart{TDrawingContext})"/>
    public override LvcSize Measure(Chart<TDrawingContext> chart)
    {
        var xl = Padding.Left;
        var yl = Padding.Top;
        var rowHeight = -1f;
        var columnWidth = -1f;
        var mx = 0f;
        var my = 0f;

        List<MeasureResult> line = new();

        LvcSize alignCurrentLine()
        {
            var mx = -1f;
            var my = -1f;

            foreach (var child in line)
            {
                if (Orientation == ContainerOrientation.Horizontal)
                {
                    child.Visual._y = VerticalAlignment switch
                    {
                        Align.Start => yl,
                        Align.Middle => yl + (rowHeight - child.Size.Height) / 2f,
                        Align.End => yl + rowHeight - child.Size.Height,
                        _ => throw new System.NotImplementedException()
                    };
                }
                else
                {
                    child.Visual._x = HorizontalAlignment switch
                    {
                        Align.Start => xl,
                        Align.Middle => xl + (columnWidth - child.Size.Width) / 2f,
                        Align.End => xl + columnWidth - child.Size.Width,
                        _ => throw new System.NotImplementedException()
                    };
                }

                child.Visual.OnInvalidated(chart);
                child.Visual.SetParent(_boundsGeometry);
                if (child.Size.Width > mx) mx = child.Size.Width;
                if (child.Size.Height > my) my = child.Size.Height;
            }

            line = new();
            return new LvcSize(mx, my);
        }

        foreach (var child in Children)
        {
            var childSize = child.Measure(chart);

            if (Orientation == ContainerOrientation.Horizontal)
            {
                if (xl + childSize.Width > MaxWidth)
                {
                    var lineSize = alignCurrentLine();
                    xl = Padding.Left;
                    yl += lineSize.Height;
                    rowHeight = -1f;
                }

                if (rowHeight < childSize.Height) rowHeight = childSize.Height;
                child._x = xl;

                xl += childSize.Width;
            }
            else
            {
                if (yl + childSize.Height > MaxHeight)
                {
                    var lineSize = alignCurrentLine();
                    yl = Padding.Top;
                    xl += lineSize.Width;
                    columnWidth = -1f;
                }

                if (columnWidth < childSize.Width) columnWidth = childSize.Width;
                child._y = yl;

                yl += childSize.Height;
            }

            if (xl > mx) mx = xl;
            if (yl > my) my = yl;
            line.Add(new MeasureResult(child, childSize));
        }

        if (line.Count > 0)
        {
            var lineSize = alignCurrentLine();

            if (Orientation == ContainerOrientation.Horizontal)
            {
                yl += lineSize.Height;
            }
            else
            {
                xl += lineSize.Width;
            }

            if (xl > mx) mx = xl;
            if (yl > my) my = yl;
        }

        return new LvcSize(
            Padding.Left + Padding.Right + mx,
            Padding.Top + Padding.Bottom + my);
    }

    /// <inheritdoc cref="ChartElement{TDrawingContext}.RemoveFromUI(Chart{TDrawingContext})"/>
    public override void RemoveFromUI(Chart<TDrawingContext> chart)
    {
        foreach (var child in Children)
        {
            child.RemoveFromUI(chart);
        }

        base.RemoveFromUI(chart);
    }

    private class MeasureResult
    {
        public MeasureResult(VisualElement<TDrawingContext> visual, LvcSize size)
        {
            Visual = visual;
            Size = size;
        }

        public VisualElement<TDrawingContext> Visual { get; set; }
        public LvcSize Size { get; set; }
    }
}
