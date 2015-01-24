using Fetze.WinFormsColor;
using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public partial class GraphSettings : UserControl, ICloneable
    {
        public float GraphHeight { get; set; }
        public float GraphHeightScaled { get { return GraphHeight / 5; } set { GraphHeight = value * 5; } }
        public float GraphWidth { get; set; }
        public float GraphWidthScaled { get { return GraphWidth / 10; } set { GraphWidth = value * 10; } }

        public Color BehindGraphColor { get; set; }
        public Color AheadGraphColor { get; set; }
        public Color GridlinesColor { get; set; }
        public Color PartialFillColorBehind { get; set; }
        public Color CompleteFillColorBehind { get; set; }
        public Color PartialFillColorAhead { get; set; }
        public Color CompleteFillColorAhead { get; set; }
        public Color GraphColor { get; set; }
        public Color GraphGoldColor { get; set; }
        public Color ShadowsColor { get; set; }
        public Color GraphLinesColor { get; set; }

        public bool IsLiveGraph { get; set; }
        public bool FlipGraph { get; set; }
        public bool ShowBestSegments { get; set; }

        public LayoutMode Mode { get; set; }

        public String Comparison { get; set; }
        public LiveSplitState CurrentState { get; set; }

        public GraphSettings ()
        {
            InitializeComponent();
            GraphHeight = 120;
            GraphWidth = 180;
            BehindGraphColor = Color.FromArgb(115, 40, 40);
            AheadGraphColor = Color.FromArgb(40, 115, 52);
            GridlinesColor = Color.FromArgb(0x50, 0x0, 0x0, 0x0);
            PartialFillColorBehind = Color.FromArgb(25, 255, 255, 255);
            CompleteFillColorBehind = Color.FromArgb(50, 255, 255, 255);
            PartialFillColorAhead = Color.FromArgb(25, 255, 255, 255);
            CompleteFillColorAhead = Color.FromArgb(50, 255, 255, 255);
            GraphColor = Color.White;
            GraphGoldColor = Color.FromArgb(216, 175, 31);
            ShadowsColor = Color.FromArgb(0x38, 0x0, 0x0, 0x0);
            GraphLinesColor = Color.White;
            IsLiveGraph = true;
            FlipGraph = false;
            ShowBestSegments = false;
            Comparison = "Current Comparison";

            btnAheadColor.DataBindings.Add("BackColor", this, "AheadGraphColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnBehindColor.DataBindings.Add("BackColor", this, "BehindGraphColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnGridlinesColor.DataBindings.Add("BackColor", this, "GridlinesColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnPartialColorBehind.DataBindings.Add("BackColor", this, "PartialFillColorBehind", false, DataSourceUpdateMode.OnPropertyChanged);
            btnCompleteColorBehind.DataBindings.Add("BackColor", this, "CompleteFillColorBehind", false, DataSourceUpdateMode.OnPropertyChanged);
            btnPartialColorAhead.DataBindings.Add("BackColor", this, "PartialFillColorAhead", false, DataSourceUpdateMode.OnPropertyChanged);
            btnCompleteColorAhead.DataBindings.Add("BackColor", this, "CompleteFillColorAhead", false, DataSourceUpdateMode.OnPropertyChanged);
            btnGraphColor.DataBindings.Add("BackColor", this, "GraphColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnShadowsColor.DataBindings.Add("BackColor", this, "ShadowsColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnSeparatorsColor.DataBindings.Add("BackColor", this, "GraphLinesColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnBestSegmentColor.DataBindings.Add("BackColor", this, "GraphGoldColor", false, DataSourceUpdateMode.OnPropertyChanged);
            chkLiveGraph.DataBindings.Add("Checked", this, "IsLiveGraph", false, DataSourceUpdateMode.OnPropertyChanged);
            chkFlipGraph.DataBindings.Add("Checked", this, "FlipGraph", false, DataSourceUpdateMode.OnPropertyChanged);
            chkShowBestSegments.DataBindings.Add("Checked", this, "ShowBestSegments", false, DataSourceUpdateMode.OnPropertyChanged);
            chkShowBestSegments.CheckedChanged += chkShowBestSegments_CheckedChanged;
            cmbComparison.SelectedIndexChanged += cmbComparison_SelectedIndexChanged;
            cmbComparison.DataBindings.Add("SelectedItem", this, "Comparison", false, DataSourceUpdateMode.OnPropertyChanged);
            this.Load += GraphSettings_Load;
        }

        void chkShowBestSegments_CheckedChanged(object sender, EventArgs e)
        {
            btnBestSegmentColor.Enabled = lblBestSegmentColor.Enabled = chkShowBestSegments.Checked;
        }
        void cmbComparison_SelectedIndexChanged(object sender, EventArgs e)
        {
            Comparison = cmbComparison.SelectedItem.ToString();
        }
        void GraphSettings_Load(object sender, EventArgs e)
        {
            cmbComparison.Items.Clear();
            cmbComparison.Items.Add("Current Comparison");
            cmbComparison.Items.AddRange(CurrentState.Run.Comparisons.Where(x => x != BestSplitTimesComparisonGenerator.ComparisonName && x != NoneComparisonGenerator.ComparisonName).ToArray());
            if (!cmbComparison.Items.Contains(Comparison))
                cmbComparison.Items.Add(Comparison);
            if (Mode == LayoutMode.Vertical)
            {
                trkHeight.DataBindings.Clear();
                GraphHeightScaled = Math.Min(Math.Max(trkHeight.Minimum, GraphHeightScaled), trkHeight.Maximum);
                trkHeight.DataBindings.Add("Value", this, "GraphHeightScaled", false, DataSourceUpdateMode.OnPropertyChanged);
                heightLabel.Text = "Height:";
            }
            else
            {
                trkHeight.DataBindings.Clear();
                GraphHeightScaled = Math.Min(Math.Max(trkHeight.Minimum, GraphHeightScaled), trkHeight.Maximum);
                trkHeight.DataBindings.Add("Value", this, "GraphWidthScaled", false, DataSourceUpdateMode.OnPropertyChanged);
                heightLabel.Text = "Width:";
            }
        }

        public void SetSettings (XmlNode node)
        {
            var element = (XmlElement)node;
            Version version;
            if (element["Version"] != null)
                version = Version.Parse(element["Version"].InnerText);
            else
                version = new Version(1, 0, 0, 0);
            GraphHeight = Single.Parse(element["Height"].InnerText.Replace(',','.'), CultureInfo.InvariantCulture);
            GraphWidth = Single.Parse(element["Width"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture);
            BehindGraphColor = ParseColor(element["BehindGraphColor"]);
            AheadGraphColor = ParseColor(element["AheadGraphColor"]);
            GridlinesColor = ParseColor(element["GridlinesColor"]);
            if (version >= new Version(1, 2))
            {
                PartialFillColorBehind = ParseColor(element["PartialFillColorBehind"]);
                CompleteFillColorBehind = ParseColor(element["CompleteFillColorBehind"]);
                PartialFillColorAhead = ParseColor(element["PartialFillColorAhead"]);
                CompleteFillColorAhead = ParseColor(element["CompleteFillColorAhead"]);
                FlipGraph = Boolean.Parse(element["FlipGraph"].InnerText);
                Comparison = element["Comparison"].InnerText;
            }
            else
            {
                PartialFillColorAhead = ParseColor(element["PartialFillColor"]);
                PartialFillColorBehind = ParseColor(element["PartialFillColor"]);
                CompleteFillColorAhead = ParseColor(element["CompleteFillColor"]);
                CompleteFillColorBehind = ParseColor(element["CompleteFillColor"]);
                FlipGraph = false;
                Comparison = "Current Comparison";
            }
            if (version >= new Version(1, 5))
            {
                ShowBestSegments = Boolean.Parse(element["ShowBestSegments"].InnerText);
                GraphGoldColor = ParseColor(element["GraphGoldColor"]);
            }
            else
            {
                GraphGoldColor = Color.Gold;
                ShowBestSegments = false;
            }
            GraphColor = ParseColor(element["GraphColor"]);
            ShadowsColor = ParseColor(element["ShadowsColor"]);
            GraphLinesColor = ParseColor(element["GraphLinesColor"]);
            IsLiveGraph = Boolean.Parse(element["LiveGraph"].InnerText);
        }

        public XmlNode GetSettings (XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            parent.AppendChild(ToElement(document, "Version", "1.5"));
            parent.AppendChild(ToElement(document, "Height", GraphHeight));
            parent.AppendChild(ToElement(document, "Width", GraphWidth));
            parent.AppendChild(ToElement(document, BehindGraphColor, "BehindGraphColor"));
            parent.AppendChild(ToElement(document, AheadGraphColor, "AheadGraphColor"));
            parent.AppendChild(ToElement(document, GridlinesColor, "GridlinesColor"));
            parent.AppendChild(ToElement(document, PartialFillColorBehind, "PartialFillColorBehind"));
            parent.AppendChild(ToElement(document, CompleteFillColorBehind, "CompleteFillColorBehind"));
            parent.AppendChild(ToElement(document, PartialFillColorAhead, "PartialFillColorAhead"));
            parent.AppendChild(ToElement(document, CompleteFillColorAhead, "CompleteFillColorAhead"));
            parent.AppendChild(ToElement(document, GraphColor, "GraphColor"));
            parent.AppendChild(ToElement(document, ShadowsColor, "ShadowsColor"));
            parent.AppendChild(ToElement(document, GraphLinesColor, "GraphLinesColor"));
            parent.AppendChild(ToElement(document, "LiveGraph", IsLiveGraph));
            parent.AppendChild(ToElement(document, "FlipGraph", FlipGraph));
            parent.AppendChild(ToElement(document, "Comparison", Comparison));
            parent.AppendChild(ToElement(document, "ShowBestSegments", ShowBestSegments));
            parent.AppendChild(ToElement(document, GraphGoldColor, "GraphGoldColor"));
            return parent;
        }

        private Color ParseColor (XmlElement colorElement)
        {
            return Color.FromArgb(Int32.Parse(colorElement.InnerText,NumberStyles.HexNumber));
        }

        private XmlElement ToElement(XmlDocument document, Color color, string name)
        {
            var element = document.CreateElement(name);
            element.InnerText = color.ToArgb().ToString("X8");
            return element;
        }

        public object Clone()
        {
            return new GraphSettings()
            {
                GraphHeight = this.GraphHeight,
                BehindGraphColor = this.BehindGraphColor,
                AheadGraphColor = this.AheadGraphColor,
                GridlinesColor = this.GridlinesColor,
                PartialFillColorBehind = this.PartialFillColorBehind,
                CompleteFillColorBehind = this.CompleteFillColorBehind,
                PartialFillColorAhead = this.PartialFillColorAhead,
                CompleteFillColorAhead = this.CompleteFillColorAhead,
                GraphColor = this.GraphColor,
                ShadowsColor = this.ShadowsColor,
                GraphLinesColor = this.GraphLinesColor
            };
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var picker = new ColorPickerDialog();
            picker.SelectedColorChanged += (s, x) => button.BackColor = picker.SelectedColor;
            picker.SelectedColor = picker.OldColor = button.BackColor;            
            picker.ShowDialog(this);
            button.BackColor = picker.SelectedColor;
        }

        private XmlElement ToElement<T>(XmlDocument document, String name, T value)
        {
            var element = document.CreateElement(name);
            element.InnerText = value.ToString();
            return element;
        }

        private XmlElement ToElement(XmlDocument document, String name, float value)
        {
            var element = document.CreateElement(name);
            element.InnerText = value.ToString(CultureInfo.InvariantCulture);
            return element;
        }
    }
}
