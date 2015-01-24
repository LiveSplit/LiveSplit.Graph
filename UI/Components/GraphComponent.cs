using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class GraphComponent : IComponent
    {
        public float PaddingTop { get { return 0f; } }
        public float PaddingLeft { get { return 0f; } }
        public float PaddingBottom { get { return 0f; } }
        public float PaddingRight { get { return 0f; } }

        public List<TimeSpan?> Deltas { get; set; }
        public TimeSpan? FinalSplit { get; set; }
        public TimeSpan MaxDelta { get; set; }
        public TimeSpan MinDelta { get; set; }

        public bool IsBestSegment { get; set; }

        public GraphicsCache Cache { get; set; }

        public float VerticalHeight
        {
            get { return Settings.GraphHeight; }
        }

        public float MinimumWidth
        {
            get { return 20; }
        }

        public float HorizontalWidth
        {
            get { return Settings.GraphWidth; }
        }

        public float MinimumHeight
        {
            get { return 20; }
        }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }


        public TimeSpan GraphEdgeValue { get; set; }
        public float GraphEdgeMin { get; set; }
        public GraphSettings Settings { get; set; }

        public GraphComponent (GraphSettings settings)
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
            DrawGraph(g, state, width, height);
            g.Transform = oldMatrix;
        }
        private void DrawGraph(Graphics g, LiveSplitState state, float width, float height)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;

            TimeSpan TotalDelta = MinDelta - MaxDelta;
            //Calculate middle and graph edge
            float graphEdge = 0;
            float GraphHeight = (height) / 2.0f;
            float Middle = GraphHeight;
            if (TotalDelta != TimeSpan.Zero)
            {
                graphEdge = (float)((GraphEdgeValue.TotalMilliseconds / (-TotalDelta.TotalMilliseconds + GraphEdgeValue.TotalMilliseconds * 2)) * (GraphHeight * 2 - GraphEdgeMin * 2));
                graphEdge += GraphEdgeMin;
                Middle = (float)(-(MaxDelta.TotalMilliseconds / TotalDelta.TotalMilliseconds)
                        * (GraphHeight - graphEdge) * 2 + graphEdge);
            }
            // Draw Green and Red Graph Portions
            var brush = new SolidBrush(Settings.GraphColor);
            brush.Color = Settings.BehindGraphColor;
            g.FillRectangle(brush, 0, 0,
                    width, Middle);
            brush.Color = Settings.AheadGraphColor;
            g.FillRectangle(brush, 0, Middle,
                    width, GraphHeight * 2
                            - Middle);
            // Calculate Gridlines
            double gridValueX, gridValueY;
            if (state.CurrentPhase != TimerPhase.NotRunning && FinalSplit.Value > TimeSpan.Zero)
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
                while ((-TotalDelta.TotalMilliseconds) / gridValueY > (GraphHeight - graphEdge) * 2 / 20)
                {
                    gridValueY *= 6;
                }
                gridValueY = (gridValueY / (-TotalDelta.TotalMilliseconds)) * (GraphHeight - graphEdge) * 2;
            }
            else
            {
                gridValueY = -1;
            }


            // Draw Gridlines
            var g2 = g;
            Pen pen = new Pen(Settings.GridlinesColor, 2.0f);

            if (gridValueX > 0)
            {
                for (double x = gridValueX; x < width; x += gridValueX)
                {
                    g2.DrawLine(pen, (float)x,
                            0, (float)x,
                            GraphHeight * 2);
                }
            }
            for (float y = Middle - 1; y > 0; y -= (float)gridValueY)
            {
                g2.DrawLine(pen, 0, y, width, y);
                if (gridValueY < 0)
                    break;
            }
            for (float y = Middle; y < GraphHeight * 2; y += (float)gridValueY)
            {
                g2.DrawLine(pen, 0, y, width, y);
                if (gridValueY < 0)
                    break;
            }

            // Draw Graph
            try
            {
                pen.Width = 1.75f;
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                var circleList = new List<PointF>();
                if (Deltas.Count > 0)
                {
                    float heightOne = GraphHeight;
                    if (TotalDelta != TimeSpan.Zero)
                        heightOne = (float)(((-MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                            * (GraphHeight - graphEdge) * 2 + graphEdge);
                    float heightTwo = 0;
                    float widthOne = 0;
                    float widthTwo = 0;
                    int x = -1, y = -1;
                    var pointArray = new List<PointF>();
                    pointArray.Add(new PointF(0, Middle));
                    while (y < Deltas.Count - 1)
                    {
                        y++;
                        while (Deltas[y] == null && y < Deltas.Count - 1)
                        {
                            y++;
                        }
                        if (Deltas[y] != null)
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
                            // Draw fill beneath graph
                            if ((heightTwo - Middle) / (heightOne - Middle) > 0)
                            {
                                if (y == Deltas.Count - 1 && IsBestSegment)
                                {
                                    brush.Color = heightTwo > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                                    g2.FillPolygon(brush, new PointF[] 
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
                            else
                            {
                                var ratio = (heightOne - Middle) / (heightOne - heightTwo);
                                if (y == Deltas.Count - 1 && IsBestSegment)
                                {
                                    brush.Color = heightOne > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                                    if (TotalDelta != TimeSpan.Zero)
                                    {
                                        g2.FillPolygon(brush, new PointF[] 
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
                                    g2.FillPolygon(brush, pointArray.ToArray());
                                    brush.Color = heightOne > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                                }
                                if (y == Deltas.Count - 1 && IsBestSegment)
                                {
                                    brush.Color = heightTwo > Middle ? Settings.PartialFillColorAhead : Settings.PartialFillColorBehind;
                                    if (TotalDelta != TimeSpan.Zero)
                                    {
                                        g2.FillPolygon(brush, new PointF[] 
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

                            if (y == Deltas.Count - 1)
                            {
                                pointArray.Add(new PointF(pointArray.Last().X, Middle));
                                if (pointArray.Count > 1)
                                {
                                    brush.Color = pointArray[pointArray.Count - 2].Y > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                                    g2.FillPolygon(brush, pointArray.ToArray());
                                }
                            }

                            x = y;
                            if (TotalDelta != TimeSpan.Zero)
                                heightOne = (float)((Deltas[x].Value.TotalMilliseconds - MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                                    * (GraphHeight - graphEdge) * 2 + graphEdge;
                            else
                                heightOne = GraphHeight;
                            if (x != Deltas.Count - 1 && state.Run[x].SplitTime[state.CurrentTimingMethod] != null)
                                widthOne = (float)((state.Run[x].SplitTime[state.CurrentTimingMethod].Value.TotalMilliseconds / FinalSplit.Value.TotalMilliseconds) * (width));
                        }
                        else
                        {
                            pointArray.Add(new PointF(pointArray.Last().X, Middle));
                            if (pointArray.Count > 1)
                            {
                                brush.Color = pointArray[pointArray.Count - 2].Y > Middle ? Settings.CompleteFillColorAhead : Settings.CompleteFillColorBehind;
                                g2.FillPolygon(brush, pointArray.ToArray());
                            }
                        }
                    }
                    heightOne = GraphHeight;
                    if (TotalDelta != TimeSpan.Zero)
                        heightOne = (float)(((-MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                            * (GraphHeight - graphEdge) * 2 + graphEdge);
                    heightTwo = 0;
                    widthOne = 0;
                    widthTwo = 0;
                    x = -1;
                    y = -1;

                    while (y < Deltas.Count - 1)
                    {
                        y++;
                        while (Deltas[y] == null && y < Deltas.Count - 1)
                        {
                            y++;
                        }
                        if (Deltas[y] != null)
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

                            pen.Color = Settings.GraphColor;
                            if ((y != Deltas.Count - 1 || !IsBestSegment) && CheckBestSegment(state, y, comparison, state.CurrentTimingMethod)) pen.Color = Settings.GraphGoldColor;
                            // Draw graph line
                            DrawLineShadowed(g2, pen, widthOne, heightOne, widthTwo, heightTwo, Settings.FlipGraph);
                            // Add circles for later
                            circleList.Add(new PointF(widthTwo, heightTwo));
                            x = y;
                            if (TotalDelta != TimeSpan.Zero)
                                heightOne = (float)((Deltas[x].Value.TotalMilliseconds - MaxDelta.TotalMilliseconds) / TotalDelta.TotalMilliseconds)
                                    * (GraphHeight - graphEdge) * 2 + graphEdge;
                            else
                                heightOne = GraphHeight;
                            if (x != Deltas.Count - 1 && state.Run[x].SplitTime[state.CurrentTimingMethod] != null)
                                widthOne = (float)((state.Run[x].SplitTime[state.CurrentTimingMethod].Value.TotalMilliseconds / FinalSplit.Value.TotalMilliseconds) * (width));
                        }
                    }


                    int i = 0;
                    foreach (var circle in circleList)
                    {
                        brush.Color = Settings.GraphColor;
                        if (CheckBestSegment(state, i, comparison, state.CurrentTimingMethod)) brush.Color = Settings.GraphGoldColor;
                        if (circle.X != width || !IsBestSegment)
                        {
                            DrawEllipseShadowed(g2, brush, circle.X - 2.5f, circle.Y - 2.5f, 5, 5, Settings.FlipGraph);
                        }
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public bool CheckBestSegment(LiveSplitState state, int splitNumber, string comparison, TimingMethod method)
        {
            TimeSpan? curSegment;
            curSegment = LiveSplitStateHelper.GetPreviousSegment(state, splitNumber, false, true, comparison, method);
            if (curSegment != null)
            {
                if (state.Run[splitNumber].BestSegmentTime[method] == null || curSegment < state.Run[splitNumber].BestSegmentTime[method])
                    return true;
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


        

        public string UpdateName
        {
            get { throw new NotImplementedException(); }
        }

        public string XMLURL
        {
            get { throw new NotImplementedException(); }
        }

        public string UpdateURL
        {
            get { throw new NotImplementedException(); }
        }

        public Version Version
        {
            get { throw new NotImplementedException(); }
        }

        public string ComponentName
        {
            get { return "Graph Component"; }
        }


        public Control GetSettingsControl(LayoutMode mode)
        {
            throw new NotImplementedException();
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            throw new NotImplementedException();
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            throw new NotImplementedException();
        }

        protected void Calculate(LiveSplitState state)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;
            // Calculate Deltas for Graph
            Deltas = new List<TimeSpan?>();
            MaxDelta = TimeSpan.Zero;
            MinDelta = TimeSpan.Zero;

            FinalSplit = TimeSpan.Zero;
            if (Settings.IsLiveGraph)
            {
                if (state.CurrentPhase != TimerPhase.NotRunning)
                    FinalSplit = state.CurrentTime[state.CurrentTimingMethod];
            }
            else
            {
                foreach (var segment in state.Run)
                {
                    if (segment.SplitTime[state.CurrentTimingMethod] != null)
                        FinalSplit = segment.SplitTime[state.CurrentTimingMethod];
                }
            }
            int nonNullDeltas = 0;
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
                    nonNullDeltas++;
                }
                else
                {
                    Deltas.Add(null);
                }
            }

            IsBestSegment = false;
            if (Settings.IsLiveGraph)
            {
                if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
                {
                    TimeSpan? bestSeg = LiveSplitStateHelper.CheckLiveDelta(state, false, comparison, state.CurrentTimingMethod);
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
                        nonNullDeltas++;
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
