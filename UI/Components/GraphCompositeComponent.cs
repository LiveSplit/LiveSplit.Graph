using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class GraphCompositeComponent : IComponent
    {
        protected GraphSettings Settings { get; set; }
        public ComponentRendererComponent InternalComponent { get; protected set; }

        public float PaddingTop { get { return InternalComponent.PaddingTop; } }
        public float PaddingLeft { get { return InternalComponent.PaddingLeft; } }
        public float PaddingBottom { get { return InternalComponent.PaddingBottom; } }
        public float PaddingRight { get { return InternalComponent.PaddingRight; } }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public GraphCompositeComponent(LiveSplitState state)
        {
            Settings = new GraphSettings()
            {
                CurrentState = state
            };
            InternalComponent = new ComponentRendererComponent();
            var components = new List<IComponent>();
            components.Add(new GraphSeparatorComponent(Settings) { LockToBottom = true });
            components.Add(new GraphComponent(Settings));
            components.Add(new GraphSeparatorComponent(Settings) { LockToBottom = false });
            InternalComponent.VisibleComponents = components;
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }


        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public string ComponentName
        {
            get { return "Graph" + (Settings.Comparison == "Current Comparison" ? "" : " (" + CompositeComparisons.GetShortComparisonName(Settings.Comparison) + ")"); }
        }

        public float HorizontalWidth
        {
            get { return InternalComponent.HorizontalWidth; }
        }

        public float MinimumHeight
        {
            get { return InternalComponent.MinimumHeight; }
        }

        public void DrawHorizontal(System.Drawing.Graphics g, Model.LiveSplitState state, float height, Region clipRegion)
        {
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public float VerticalHeight
        {
            get { return InternalComponent.VerticalHeight; }
        }

        public float MinimumWidth
        {
            get { return InternalComponent.MinimumWidth; }
        }

        public void DrawVertical(System.Drawing.Graphics g, Model.LiveSplitState state, float width, Region clipRegion)
        {
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (invalidator != null)
                InternalComponent.Update(invalidator, state, width, height, mode);
        }

        public void Dispose()
        {
        }
    }
}
