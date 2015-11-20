using LiveSplit.Model;
using LiveSplit.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class GraphComponent : IComponent
    {
        public float PaddingTop => 0f;
        public float PaddingLeft => 0f;
        public float PaddingBottom => 0f;
        public float PaddingRight => 0f;

        public List<TimeSpan?> Deltas { get; set; }
        public TimeSpan? FinalSplit { get; set; }
        public TimeSpan MaxDelta { get; set; }
        public TimeSpan MinDelta { get; set; }

        public bool IsBestSegment { get; set; }

        public GraphicsCache Cache { get; set; }

        public float VerticalHeight => Settings.GraphHeight;

        public float MinimumWidth => 20;

        public float HorizontalWidth => Settings.GraphWidth;

        public float MinimumHeight => 20;

        public IDictionary<string, Action> ContextMenuControls => null;

        public TimeSpan GraphEdgeValue { get; set; }
        public float GraphEdgeMin { get; set; }
        public GraphSettings Settings { get; set; }

        public GraphComponent(GraphSettings settings)
        {
            GraphEdgeValue = new TimeSpan(0, 0, 0, 0, 200);
            GraphEdgeMin = 5;
            Settings = settings;
            Cache = new GraphicsCache();
            Deltas = new List<TimeSpan?>();
            FinalSplit = TimeSpan.Zero;
            MaxDelta = TimeSpan.Zero;
            MinDelta = TimeSpan.Zero;
        }

        private void DrawGeneral(Graphics g, LiveSplitState state, float width, float height)
        {
            var oldMatrix = g.Transform;
            if (Settings.FlipGraph)
            {
                g.ScaleTransform(1, -1);
                g.TranslateTransform(0, -height);
            }
            DrawUnflipped(g, state, width, height);
            g.Transform = oldMatrix;
        }

        private void DrawUnflipped(Graphics g, LiveSplitState state, float width, float height)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;

            TimeSpan TotalDelta = MinDelta - MaxDelta;

            float graphEdge, graphHeight, middle;
            CalculateMiddleAndGraphEdge(height, TotalDelta, out graphEdge, out graphHeight, out middle);

            var brush = new SolidBrush(Settings.GraphColor);
            DrawGreenAndRedGraphPortions(g, width, graphHeight, middle, brush);

            double gridValueX, gridValueY;
            CalculateGridlines(state, width, TotalDelta, graphEdge, graphHeight, out gridValueX, out gridValueY);

            var pen = new Pen(Settings.GridlinesColor, 2.0f);
            DrawGridlines(g, width, graphHeight, middle, gridValueX, gridValueY, pen);

            try
            {
                DrawGraph(g, state, width, comparison, TotalDelta, graphEdge, graphHeight, middle, brush, pen);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void DrawGraph(Graphics g, LiveSplitState state, float width, string comparison, TimeSpan TotalDelta, float graphEdge, float graphHeight, float middle, SolidBrush brush, Pen pen)
        {
            pen.Width = 1.75f;
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            var circleList = new List<PointF>();
            if (Deltas.Count > 0)
            {
                float heightOne = graphHeight;
                if (TotalDelta != TimeSpan.Zero)
                    heightOne = (float)(((-MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                        * (graphHeight - graphEdge) * 2 + graphEdge);
                float heightTwo = 0;
                float widthOne = 0;
                float widthTwo = 0;
                int y = 0;

                var pointArray = new List<PointF>();
                pointArray.Add(new PointF(0, middle));
                circleList.Add(new PointF(widthOne, heightOne));

                while (y < Deltas.Count)
                {
                    while (Deltas[y] == null && y < Deltas.Count - 1)
                    {
                        y++;
                    }

                    if (Deltas[y] != null)
                    {
                        CalculateRightSideCoordinates(state, width, TotalDelta, graphEdge, graphHeight, ref heightTwo, ref widthTwo, y);
                        DrawFillBeneathGraph(g, TotalDelta, middle, brush, heightOne, heightTwo, widthOne, widthTwo, y, pointArray);
                        AddGraphNode(g, state, comparison, pen, circleList, heightOne, heightTwo, widthOne, widthTwo, y);
                        CalculateLeftSideCoordinates(state, width, TotalDelta, graphEdge, graphHeight, ref heightOne, ref widthOne, y);
                    }
                    else
                    {
                        DrawFinalPolygon(g, middle, brush, pointArray);
                    }

                    y++;
                }

                DrawCirclesAndLines(g, state, width, comparison, pen, brush, circleList);
            }
        }

        private void DrawCirclesAndLines(Graphics g, LiveSplitState state, float width, string comparison, Pen pen, SolidBrush brush, List<PointF> circleList)
        {
            int i = 0;
            var previousCircle = circleList.FirstOrDefault();
            if (previousCircle != null)
                circleList.RemoveAt(0);

            foreach (var circle in circleList)
            {
                //Skip i with the skipped splits, so that i is still correct
                i += Deltas.Skip(i).TakeWhile(delta => delta == null).Count();

                pen.Color = brush.Color = Settings.GraphColor;
                var finalDelta = circle.X == width && IsBestSegment;
                if (!finalDelta && CheckBestSegment(state, i, comparison, state.CurrentTimingMethod))
                    pen.Color = brush.Color = Settings.GraphGoldColor;

                DrawLineShadowed(g, pen, previousCircle.X, previousCircle.Y, circle.X, circle.Y, Settings.FlipGraph);
                if (!finalDelta)
                    DrawEllipseShadowed(g, brush, circle.X - 2.5f, circle.Y - 2.5f, 5, 5, Settings.FlipGraph);

                previousCircle = circle;
                i++;
            }
        }

        private void AddGraphNode(Graphics g, LiveSplitState state, string comparison, Pen pen, List<PointF> circleList, float heightOne, float heightTwo, float widthOne, float widthTwo, int y)
        {
            circleList.Add(new PointF(widthTwo, heightTwo));
        }

        private void CalculateLeftSideCoordinates(LiveSplitState state, float width, TimeSpan TotalDelta, float graphEdge, float GraphHeight, ref float heightOne, ref float widthOne, int y)
        {
            if (TotalDelta != TimeSpan.Zero)
                heightOne = (float)((Deltas[y].Value.TotalMilliseconds - MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                    * (GraphHeight - graphEdge) * 2 + graphEdge;
            else
                heightOne = GraphHeight;
            if (y != Deltas.Count - 1 && state.Run[y].SplitTime[state.CurrentTimingMethod] != null)
                widthOne = (float)((state.Run[y].SplitTime[state.CurrentTimingMethod].Value.TotalMilliseconds / FinalSplit.Value.TotalMilliseconds) * (width));
        }

        private void CalculateRightSideCoordinates(LiveSplitState state, float width, TimeSpan TotalDelta, float graphEdge, float GraphHeight, ref float heightTwo, ref float widthTwo, int y)
        {
            if (y == Deltas.Count - 1 && IsBestSegment)
                widthTwo = width;
            else if (state.Run[y].SplitTime[state.CurrentTimingMethod] != null)
                widthTwo = (float)((state.Run[y].SplitTime[state.CurrentTimingMethod].Value.TotalMilliseconds / FinalSplit.Value.TotalMilliseconds) * (width));
            if (TotalDelta != TimeSpan.Zero)
                heightTwo = (float)((Deltas[y].Value.TotalMilliseconds - MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds
                    * (GraphHeight - graphEdge) * 2 + graphEdge);
            else
                heightTwo = GraphHeight;
        }

        private void DrawFillBeneathGraph(Graphics g, TimeSpan TotalDelta, float Middle, SolidBrush brush, float heightOne, float heightTwo, float widthOne, float widthTwo, int y, List<PointF> pointArray)
        {
            if ((heightTwo - Middle) / (heightOne - Middle) > 0)
            {
                AddFillOneSide(g, Middle, brush, heightOne, heightTwo, widthOne, widthTwo, y, pointArray);
            }
            else
            {
                var ratio = (heightOne - Middle) / (heightOne - heightTwo);
                AddFillFirstHalf(g, TotalDelta, Middle, brush, heightOne, widthOne, widthTwo, y, pointArray, ratio);
                AddFillSecondHalf(g, TotalDelta, Middle, brush, heightTwo, widthOne, widthTwo, y, pointArray, ratio);
            }

            if (y == Deltas.Count - 1)
            {
                DrawFinalPolygon(g, Middle, brush, pointArray);
            }
        }

        private void DrawFinalPolygon(Graphics g, float Middle, SolidBrush brush, List<PointF> pointArray)
        {
            pointArray.Add(new PointF(pointArray.Last().X, Middle));
            if (pointArray.Count > 1)
            {
                brush.Color = pointArray[pointArray.Count - 2].Y > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                g.FillPolygon(brush, pointArray.ToArray());
            }
        }

        // Adds to the point array the second portion of the fill if the graph goes from ahead to behind or vice versa
        private void AddFillSecondHalf(Graphics g, TimeSpan TotalDelta, float Middle, SolidBrush brush, float heightTwo, float widthOne, float widthTwo, int y, List<PointF> pointArray, float ratio)
        {
            if (y == Deltas.Count - 1 && IsBestSegment)
            {
                brush.Color = heightTwo > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                if (TotalDelta != TimeSpan.Zero)
                {
                    g.FillPolygon(brush, new PointF[]
                    {
                        new PointF(widthOne+(widthTwo-widthOne)*ratio, Middle),
                        new PointF(widthTwo, heightTwo),
                        new PointF(widthTwo, Middle)
                    });
                }
            }
            else
            {
                brush.Color = heightTwo > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                pointArray.Clear();
                pointArray.Add(new PointF(widthOne + (widthTwo - widthOne) * ratio, Middle));
                pointArray.Add(new PointF(widthTwo, heightTwo));
            }
        }

        // Adds to the point array the first portion of the fill if the graph goes from ahead to behind or vice versa
        private void AddFillFirstHalf(Graphics g, TimeSpan TotalDelta, float Middle, SolidBrush brush, float heightOne, float widthOne, float widthTwo, int y, List<PointF> pointArray, float ratio)
        {
            if (y == Deltas.Count - 1 && IsBestSegment)
            {
                brush.Color = heightOne > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                if (TotalDelta != TimeSpan.Zero)
                {
                    g.FillPolygon(brush, new PointF[]
                    {
                        new PointF(widthOne, Middle),
                        new PointF(widthOne, heightOne),
                        new PointF(widthOne+(widthTwo-widthOne)*ratio, Middle)
                    });
                }
            }
            else
            {
                pointArray.Add(new PointF(widthOne + (widthTwo - widthOne) * ratio, Middle));
                brush.Color = heightOne > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                g.FillPolygon(brush, pointArray.ToArray());
                brush.Color = heightOne > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
            }
        }

        // Adds to the point array the fill under the graph if the current portion of the graph is either completely ahead or completely behind
        private void AddFillOneSide(Graphics g, float Middle, SolidBrush brush, float heightOne, float heightTwo, float widthOne, float widthTwo, int y, List<PointF> pointArray)
        {
            if (y == Deltas.Count - 1 && IsBestSegment)
            {
                brush.Color = heightTwo > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                g.FillPolygon(brush, new PointF[]
                {
                     new PointF(widthOne, Middle),
                     new PointF(widthOne, heightOne),
                     new PointF(widthTwo, heightTwo),
                     new PointF(widthTwo, Middle)
                });
            }
            else
            {
                pointArray.Add(new PointF(widthTwo, heightTwo));
            }
        }

        private static void DrawGridlines(Graphics g, float width, float GraphHeight, float Middle, double gridValueX, double gridValueY, Pen pen)
        {
            if (gridValueX > 0)
            {
                for (double x = gridValueX; x < width; x += gridValueX)
                {
                    g.DrawLine(pen, (float)x,
                            0, (float)x,
                            GraphHeight * 2);
                }
            }
            for (float y = Middle - 1; y > 0; y -= (float)gridValueY)
            {
                g.DrawLine(pen, 0, y, width, y);
                if (gridValueY < 0)
                    break;
            }
            for (float y = Middle; y < GraphHeight * 2; y += (float)gridValueY)
            {
                g.DrawLine(pen, 0, y, width, y);
                if (gridValueY < 0)
                    break;
            }
        }

        private void CalculateMiddleAndGraphEdge(float height, TimeSpan TotalDelta, out float graphEdge, out float graphHeight, out float middle)
        {
            graphEdge = 0;
            graphHeight = (height) / 2.0f;
            middle = graphHeight;
            if (TotalDelta != TimeSpan.Zero)
            {
                graphEdge = (float)((GraphEdgeValue.TotalMilliseconds / (-TotalDelta.TotalMilliseconds + GraphEdgeValue.TotalMilliseconds * 2)) * (graphHeight * 2 - GraphEdgeMin * 2));
                graphEdge += GraphEdgeMin;
                middle = (float)(-(MaxDelta.TotalMilliseconds / TotalDelta.TotalMilliseconds)
                        * (graphHeight - graphEdge) * 2 + graphEdge);
            }
        }

        private void CalculateGridlines(LiveSplitState state, float width, TimeSpan TotalDelta, float graphEdge, float graphHeight, out double gridValueX, out double gridValueY)
        {
            if (state.CurrentPhase != TimerPhase.NotRunning && FinalSplit > TimeSpan.Zero)
            {
                gridValueX = 1000;
                while (FinalSplit.Value.TotalMilliseconds / gridValueX > width / 20)
                {
                    gridValueX *= 6;
                }
                gridValueX = (gridValueX / FinalSplit.Value.TotalMilliseconds) * width;
            }
            else
            {
                gridValueX = -1;
            }
            if (state.CurrentPhase != TimerPhase.NotRunning && TotalDelta < TimeSpan.Zero)
            {
                gridValueY = 1000;
                while ((-TotalDelta.TotalMilliseconds) / gridValueY > (graphHeight - graphEdge) * 2 / 20)
                {
                    gridValueY *= 6;
                }
                gridValueY = (gridValueY / (-TotalDelta.TotalMilliseconds)) * (graphHeight - graphEdge) * 2;
            }
            else
            {
                gridValueY = -1;
            }
        }

        private void DrawGreenAndRedGraphPortions(Graphics g, float width, float GraphHeight, float Middle, SolidBrush brush)
        {
            brush.Color = Settings.BehindGraphColor;
            g.FillRectangle(brush, 0, 0, width, Middle);
            brush.Color = Settings.AheadGraphColor;
            g.FillRectangle(brush, 0, Middle, width, GraphHeight * 2 - Middle);
        }

        public bool CheckBestSegment(LiveSplitState state, int splitNumber, string comparison, TimingMethod method)
        {
            if (Settings.ShowBestSegments)
            {
                TimeSpan? curSegment;
                curSegment = LiveSplitStateHelper.GetPreviousSegmentTime(state, splitNumber, method);
                if (curSegment != null)
                {
                    if (state.Run[splitNumber].BestSegmentTime[method] == null || curSegment < state.Run[splitNumber].BestSegmentTime[method])
                        return true;
                }
            }
            return false;
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height);
        }

        private void DrawLineShadowed(Graphics g, Pen pen, float x1, float y1, float x2, float y2, bool flipShadow)
        {
            var shadowPen = (Pen)pen.Clone();
            shadowPen.Color = Settings.ShadowsColor;
            if (!flipShadow)
            {
                g.DrawLine(shadowPen, x1 + 1, y1 + 1, x2 + 1, y2 + 1);
                g.DrawLine(shadowPen, x1 + 1, y1 + 2, x2 + 1, y2 + 2);
                g.DrawLine(shadowPen, x1 + 1, y1 + 3, x2 + 1, y2 + 3);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            else
            {
                g.DrawLine(shadowPen, x1 + 1, y1 - 1, x2 + 1, y2 - 1);
                g.DrawLine(shadowPen, x1 + 1, y1 - 2, x2 + 1, y2 - 2);
                g.DrawLine(shadowPen, x1 + 1, y1 - 3, x2 + 1, y2 - 3);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void DrawEllipseShadowed(Graphics g, Brush brush, float x, float y, float width, float height, bool flipShadow)
        {
            var shadowBrush = new SolidBrush(Settings.ShadowsColor);

            if (!flipShadow)
            {
                g.FillEllipse(shadowBrush, x + 1, y + 1, width, height);
                g.FillEllipse(shadowBrush, x + 1, y + 2, width, height);
                g.FillEllipse(shadowBrush, x + 1, y + 3, width, height);
                g.FillEllipse(brush, x, y, width, height);
            }
            else
            {
                g.FillEllipse(shadowBrush, x + 1, y - 1, width, height);
                g.FillEllipse(shadowBrush, x + 1, y - 2, width, height);
                g.FillEllipse(shadowBrush, x + 1, y - 3, width, height);
                g.FillEllipse(brush, x, y, width, height);
            }
        }

        public string ComponentName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            throw new NotSupportedException();
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            throw new NotSupportedException();
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            throw new NotSupportedException();
        }

        protected void Calculate(LiveSplitState state)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;

            CalculateFinalSplit(state);
            CalculateDeltas(state, comparison);
            CheckLiveSegmentDelta(state, comparison);
        }

        private void CalculateFinalSplit(LiveSplitState state)
        {
            FinalSplit = TimeSpan.Zero;
            if (Settings.IsLiveGraph)
            {
                if (state.CurrentPhase != TimerPhase.NotRunning)
                    FinalSplit = state.CurrentTime[state.CurrentTimingMethod] ?? state.CurrentTime.RealTime;
            }
            else
            {
                foreach (var segment in state.Run)
                {
                    if (segment.SplitTime[state.CurrentTimingMethod] != null)
                        FinalSplit = segment.SplitTime[state.CurrentTimingMethod];
                }
            }
        }

        private void CalculateDeltas(LiveSplitState state, string comparison)
        {
            Deltas = new List<TimeSpan?>();
            MaxDelta = TimeSpan.Zero;
            MinDelta = TimeSpan.Zero;
            for (int x = 0; x < state.Run.Count; x++)
            {
                if (state.Run[x].SplitTime[state.CurrentTimingMethod] != null
                        && state.Run[x].Comparisons[comparison][state.CurrentTimingMethod] != null)
                {
                    TimeSpan time = state.Run[x].SplitTime[state.CurrentTimingMethod].Value
                            - state.Run[x].Comparisons[comparison][state.CurrentTimingMethod].Value;
                    if (time > MaxDelta)
                        MaxDelta = time;
                    if (time < MinDelta)
                        MinDelta = time;
                    Deltas.Add(time);
                }
                else
                {
                    Deltas.Add(null);
                }
            }
        }

        private void CheckLiveSegmentDelta(LiveSplitState state, string comparison)
        {
            IsBestSegment = false;
            if (Settings.IsLiveGraph)
            {
                if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
                {
                    TimeSpan? bestSeg = LiveSplitStateHelper.CheckLiveDelta(state, true, comparison, state.CurrentTimingMethod);
                    if (bestSeg == null
                            && (state.Run[state.CurrentSplitIndex].Comparisons[comparison][state.CurrentTimingMethod] != null &&
                                    state.CurrentTime[state.CurrentTimingMethod]
                                    - state.Run[state.CurrentSplitIndex].Comparisons[comparison][state.CurrentTimingMethod] > MinDelta))
                    {
                        bestSeg = state.CurrentTime[state.CurrentTimingMethod]
                                - state.Run[state.CurrentSplitIndex].Comparisons[comparison][state.CurrentTimingMethod];
                    }
                    if (bestSeg != null)
                    {
                        if (bestSeg > MaxDelta)
                            MaxDelta = bestSeg.Value;
                        if (bestSeg < MinDelta)
                            MinDelta = bestSeg.Value;
                        Deltas.Add(bestSeg);
                        IsBestSegment = true;
                    }
                }
            }
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Calculate(state);

            Cache.Restart();
            Cache["FinalSplit"] = FinalSplit.ToString();
            Cache["IsBestSegment"] = IsBestSegment;
            Cache["DeltasCount"] = Deltas.Count;
            for (var ind = 0; ind < Deltas.Count; ind++)
            {
                Cache["Deltas" + ind] = Deltas[ind] == null ? "null" : Deltas[ind].ToString();
            }

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
        }
    }
}
