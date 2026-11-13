using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RBX_Alt_Manager
{
    public partial class AccountManager
    {
        // The background is the original VEXOR mockup uploaded to the repository.
        // Transparent hit areas preserve its appearance while connecting to real actions.
        private sealed class VxHotspot : Control
        {
            public VxHotspot()
            {
                SetStyle(ControlStyles.SupportsTransparentBackColor |
                    ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;
            }
            protected override void OnPaint(PaintEventArgs e) { }
        }

        private Panel vxCanvas, vxRowsPanel;
        private Label vxCountLabel, vxSelectedLabel, vxRunningLabel, vxStatusLabel;
        private bool vxSelectionAll;
        private TextBox vxSearch;
        private Timer vxTimer;
        private Account[] vxVisible = new Account[0];
        private bool vxRefreshing;
        private readonly System.Collections.Generic.HashSet<Account> vxChecked =
            new System.Collections.Generic.HashSet<Account>();
        private const int RefW = 1648, RefH = 928;

        private void InitializeVexorDashboard()
        {
            Text = "VEXOR | Roblox Account Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1150, 690);
            ClientSize = new Size(RefW, RefH);
            Shown += (s,e) => { if (vxCanvas == null) BuildVexorImageUi(); };
        }

        private void BuildVexorImageUi()
        {
            vxCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(9,7,19) };
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VEXOR-design.png");
            if (File.Exists(file))
            {
                using (var bmp = Image.FromFile(file))
                    vxCanvas.BackgroundImage = new Bitmap(bmp);
                vxCanvas.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
                MessageBox.Show("VEXOR-design.png was not found next to VEXOR.exe.", "VEXOR");
            Controls.Add(vxCanvas);
            vxCanvas.BringToFront();

            // The artwork remains unchanged. These invisible click regions correspond
            // to the exact button coordinates in the original design.
            Hit("Dashboard", 21,138,244,48, (s,e)=>RefreshVexor());
            Hit("Select All",344,416,130,40,(s,e)=>ToggleAllVexor());
            Hit("Accounts", 21,199,244,46, (s,e)=>vxSearch.Focus());
            Hit("Launcher",21,259,244,46,(s,e)=>LaunchChecked());
            Hit("Sessions",21,321,244,46,(s,e)=>MessageBox.Show("Session telemetry is not implemented yet.", "VEXOR"));
            Hit("VM / RDP",21,380,244,46,(s,e)=>MessageBox.Show("VM / RDP integration is not implemented yet.", "VEXOR"));
            Hit("Settings",21,442,244,46,(s,e)=>ConfigButton_Click(s,e));
            Hit("Tools",21,502,244,46,(s,e)=>{ UtilsForm.Show(); UtilsForm.BringToFront(); });
            Hit("Add Account",694,386,131,45,(s,e)=>Add.PerformClick());
            Hit("Delete",1073,386,74,45,(s,e)=>RemoveChecked());
            Hit("Launch Selected",1207,207,403,56,(s,e)=>LaunchChecked());
            Hit("Launch All",1207,276,403,56,(s,e)=>MessageBox.Show("Launching 10 simultaneous Roblox clients is not supported in this version. Select one account and use Launch Selected.", "VEXOR"));
            Hit("Import",834,386,96,45,(s,e)=>byCookieToolStripMenuItem_Click(s,e));
            Hit("Export",936,386,127,45,(s,e)=>ExportVexorNames());

            // The reference contains example accounts. Mask only that region and show
            // real accounts from the existing encrypted account store instead.
            vxRowsPanel = new Panel { BackColor = Color.FromArgb(12, 9, 26) };
            vxCanvas.Controls.Add(vxRowsPanel);
            vxRowsPanel.Paint += DrawVexorRows;
            vxRowsPanel.MouseClick += RowsClick;
            vxRowsPanel.MouseMove += (s,e)=>vxRowsPanel.Cursor = Cursors.Hand;

            vxSearch = new TextBox { BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24,17,42), ForeColor = Color.White,
                Font = new Font("Segoe UI", 11), Text = "" };
            vxCanvas.Controls.Add(vxSearch);
            // Disable the picture's decorative multi-instance controls until those
            // features exist; don't present fake working security/patch switches.
            var unavailable = new Label {
                Text = "Standard client  •  Saved account sessions\nMulti-instance and VM features: not yet available",
                ForeColor = Color.FromArgb(202, 188, 226),
                BackColor = Color.FromArgb(19, 13, 37),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Tag = new Rectangle(1206, 367, 407, 143)
            };
            vxCanvas.Controls.Add(unavailable);
            vxSearch.TextChanged += (s,e)=>RefreshVexor();

            vxCanvas.Paint += DrawVexorMetrics;
            vxCountLabel = LiveLabel();
            vxRunningLabel = LiveLabel();
            vxStatusLabel = LiveLabel();
            vxSelectedLabel = LiveLabel();
            vxCanvas.Resize += (s,e)=>LayoutVexor();
            LayoutVexor();
            vxTimer = new Timer { Interval = 2000 };
            vxTimer.Tick += (s,e)=>RefreshVexor();
            vxTimer.Start();
            FormClosed += (s,e)=>{vxTimer.Stop();vxTimer.Dispose();};
            RefreshVexor();
        }

        private Label LiveLabel()
        {
            var l = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 17, FontStyle.Bold) };
            vxCanvas.Controls.Add(l);
            return l;
        }

        private void Hit(string name, int x,int y,int w,int h, EventHandler click)
        {
            var c = new VxHotspot { Name = name, Tag = new Rectangle(x,y,w,h) };
            c.Click += click;
            vxCanvas.Controls.Add(c);
            c.BringToFront();
        }

        private Rectangle ScaleRect(int x,int y,int w,int h)
        {
            double sx = (double)vxCanvas.ClientSize.Width / RefW;
            double sy = (double)vxCanvas.ClientSize.Height / RefH;
            return new Rectangle((int)(x*sx),(int)(y*sy),
                Math.Max(1,(int)(w*sx)),Math.Max(1,(int)(h*sy)));
        }

        private void LayoutVexor()
        {
            if (vxCanvas == null) return;
            foreach(Control c in vxCanvas.Controls)
                if (c.Tag is Rectangle)
                {
                    var r=(Rectangle)c.Tag;
                    c.Bounds=ScaleRect(r.X,r.Y,r.Width,r.Height);
                }
            vxRowsPanel.Bounds=ScaleRect(335,472,811,365);
            vxSearch.Bounds=ScaleRect(864,339,238,26);
            vxCountLabel.Bounds=ScaleRect(402,268,128,32);
            vxRunningLabel.Bounds=ScaleRect(625,268,100,32);
            vxStatusLabel.Bounds=ScaleRect(1032,268,122,32);
            vxSelectedLabel.Bounds=ScaleRect(1494,187,110,27);
            vxRowsPanel.Invalidate();
        }

        private void RefreshVexor()
        {
            if (vxRefreshing || vxCanvas==null || AccountsList==null) return;
            vxRefreshing=true;
            try
            {
                string q=vxSearch.Text.Trim();
                vxVisible=AccountsList.Where(a=>a!=null &&
                    (string.IsNullOrEmpty(q) ||
                    (a.Username??"").IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0 ||
                    (a.Alias??"").IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
                vxChecked.RemoveWhere(a=>!AccountsList.Contains(a));
                vxCountLabel.Text=AccountsList.Count+" / 10";
                vxRunningLabel.Text="—";
                vxStatusLabel.Text="Ready";
                vxSelectedLabel.Text=vxChecked.Count.ToString();
                vxRowsPanel.Invalidate();
            }
            finally {vxRefreshing=false;}
        }

        private void DrawVexorMetrics(object sender, PaintEventArgs e) { }

        private void ToggleAllVexor()
        {
            if(vxVisible.Length == 0) return;
            bool all = vxVisible.All(a=>vxChecked.Contains(a));
            foreach(var a in vxVisible) { if(all) vxChecked.Remove(a); else vxChecked.Add(a); }
            RefreshVexor();
        }

        private void ExportVexorNames()
        {
            using(var dialog = new SaveFileDialog { Filter = "CSV files|*.csv", FileName = "VEXOR-accounts.csv" })
            {
                if(dialog.ShowDialog()!=DialogResult.OK)return;
                var lines = new System.Collections.Generic.List<string> { "Alias,Username" };
                foreach(var a in AccountsList)
                {
                    Func<string,string> esc = x => "\\\"" + (x??"").Replace("\\\"","\\\"\\\"") + "\\\"";
                    lines.Add(esc(a.Alias)+","+esc(a.Username));
                }
                File.WriteAllLines(dialog.FileName,lines.ToArray());
                MessageBox.Show("Exported account names only. No passwords or tokens were included.","VEXOR");
            }
        }

        private void DrawVexorRows(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(12,9,26));
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            float sx=vxRowsPanel.Width/811f, sy=vxRowsPanel.Height/365f;
            using(var text=new SolidBrush(Color.FromArgb(237,229,255)))
            using(var muted=new SolidBrush(Color.FromArgb(171,160,193)))
            using(var purple=new SolidBrush(Color.FromArgb(130,28,231)))
            using(var line=new Pen(Color.FromArgb(42,34,65)))
            using(var font=new Font("Segoe UI",10))
            using(var bold=new Font("Segoe UI",10,FontStyle.Bold))
            {
                int n=Math.Min(vxVisible.Length,10);
                for(int i=0;i<n;i++)
                {
                    int y=(int)(i*36*sy);
                    var a=vxVisible[i];
                    if(i%2==0)
                        using(var b=new SolidBrush(Color.FromArgb(19,14,36)))
                            e.Graphics.FillRectangle(b,0,y,vxRowsPanel.Width,(int)(35*sy));
                    e.Graphics.DrawRectangle(line,(int)(12*sx),y+(int)(11*sy),(int)(16*sx),(int)(16*sy));
                    if(vxChecked.Contains(a))
                        e.Graphics.FillRectangle(purple,(int)(15*sx),y+(int)(14*sy),(int)(11*sx),(int)(11*sy));
                    e.Graphics.DrawString((i+1).ToString(),font,muted,44*sx,y+8*sy);
                    string alias=string.IsNullOrWhiteSpace(a.Alias)?(a.Username??"Account"):a.Alias;
                    e.Graphics.DrawString(alias,bold,text,90*sx,y+7*sy);
                    e.Graphics.DrawString(a.Username??"",font,text,282*sx,y+7*sy);
                    e.Graphics.DrawString("●  Idle",font,muted,495*sx,y+7*sy);
                    using(var btn=new SolidBrush(Color.FromArgb(93,17,184)))
                        e.Graphics.FillRectangle(btn,659*sx,y+5*sy,43*sx,27*sy);
                    e.Graphics.DrawString("▶",bold,text,672*sx,y+7*sy);
                    e.Graphics.DrawLine(line,0,y+(int)(35*sy),vxRowsPanel.Width,y+(int)(35*sy));
                }
            }
        }

        private void RowsClick(object sender, MouseEventArgs e)
        {
            int row=(int)(e.Y/(36.0*vxRowsPanel.Height/365));
            if(row<0||row>=vxVisible.Length||row>=10)return;
            var a=vxVisible[row];
            if(e.X>vxRowsPanel.Width*0.80)
            {
                vxChecked.Clear();
                vxChecked.Add(a);
                LaunchChecked();
                return;
            }
            if(!vxChecked.Add(a))vxChecked.Remove(a);
            RefreshVexor();
        }

        private void LaunchChecked()
        {
            if(vxChecked.Count!=1)
            {
                MessageBox.Show("Select exactly one account to launch. Multi-instance support is not yet verified.", "VEXOR");
                return;
            }
            AccountsView.SelectObject(vxChecked.First());
            JoinServer.PerformClick();
        }

        private void RemoveChecked()
        {
            if(vxChecked.Count!=1)
            {
                MessageBox.Show("Select exactly one account to delete.", "VEXOR");
                return;
            }
            AccountsView.SelectObject(vxChecked.First());
            Remove.PerformClick();
            vxChecked.Clear();
            RefreshVexor();
        }
    }
}