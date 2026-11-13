using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace RBX_Alt_Manager
{
    public partial class AccountManager
    {
        private sealed class VxCard : Panel
        {
            public Color BorderColor = Color.FromArgb(101, 42, 153);
            public int Corner = 16;
            public VxCard() { DoubleBuffered = true; BackColor = Color.FromArgb(18, 13, 34); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                {
                    int r = Corner * 2;
                    var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
                    path.AddArc(rect.Left, rect.Top, r, r, 180, 90);
                    path.AddArc(rect.Right-r, rect.Top, r, r, 270, 90);
                    path.AddArc(rect.Right-r, rect.Bottom-r, r, r, 0, 90);
                    path.AddArc(rect.Left, rect.Bottom-r, r, r, 90, 90);
                    path.CloseFigure();
                    using (var pen = new Pen(BorderColor, 1.4f)) e.Graphics.DrawPath(pen, path);
                }
            }
        }
        private sealed class VxHero : Panel
        {
            public VxHero() { DoubleBuffered = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(ClientRectangle,
                    Color.FromArgb(23, 10, 46), Color.FromArgb(9, 7, 22), 0f))
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                var center = Width * .71f;
                PointF[] left = { new PointF(center-90,0), new PointF(center-38,0),
                    new PointF(center+10,Height-24), new PointF(center-18,Height-24) };
                PointF[] right = { new PointF(center+88,0), new PointF(center+42,0),
                    new PointF(center+10,Height-24), new PointF(center-18,Height-24) };
                using (var b = new LinearGradientBrush(new Point(0,0),new Point(0,Height),
                    Color.FromArgb(110, 21, 208), Color.FromArgb(216, 62, 255)))
                { e.Graphics.FillPolygon(b,left); e.Graphics.FillPolygon(b,right); }
                using (var p = new Pen(Color.FromArgb(75, 132, 22, 216), 1f))
                    for (int i=0;i<12;i++) e.Graphics.DrawArc(p,
                        (int)center-190+i*12,-150+i*8,420,330,180,115);
            }
        }

        private readonly Color vxBg = Color.FromArgb(10, 8, 20);
        private readonly Color vxPanel = Color.FromArgb(20, 14, 36);
        private readonly Color vxBorder = Color.FromArgb(86, 35, 137);
        private readonly Color vxAccent = Color.FromArgb(148, 20, 240);
        private readonly Color vxText = Color.FromArgb(244, 237, 255);
        private readonly Color vxMuted = Color.FromArgb(172, 161, 195);
        private Panel vxRoot, vxContent, vxSidebar, vxMain, vxRight;
        private Label vxCount, vxSelected, vxEmpty, vxStatus;
        private ListView vxAccounts;
        private TextBox vxSearch;
        private Timer vxTimer;
        private bool vxBusy;

        private void InitializeVexorDashboard()
        {
            Text = "VEXOR | Roblox Account Manager";
            MinimumSize = new Size(1180, 700);
            Size = new Size(1450, 850);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = vxBg;
            Shown += (s, e) => {
                if (vxRoot == null) BuildVexorUi();
                UpdateVexorUi();
            };
        }

        private Label VxLabel(string text, int size, bool bold, Color color)
        {
            return new Label { Text = text, ForeColor = color, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                AutoSize = true };
        }

        private Button VxButton(string text, bool primary, EventHandler handler)
        {
            var b = new Button { Text = text, Height = 42, Dock = DockStyle.Top,
                BackColor = primary ? vxAccent : Color.FromArgb(38, 28, 58),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = primary ? vxAccent : vxBorder;
            b.Click += handler;
            return b;
        }

        private Panel VxPanel()
        {
            return new VxCard { BackColor = vxPanel, Padding = new Padding(17) };
        }

        private void BuildVexorUi()
        {
            vxRoot = new Panel { Dock = DockStyle.Fill, BackColor = vxBg };
            Controls.Add(vxRoot);
            vxRoot.BringToFront();

            vxSidebar = new Panel { Dock = DockStyle.Left, Width = 245, BackColor = Color.FromArgb(13, 9, 26), Padding = new Padding(18) };
            vxRoot.Controls.Add(vxSidebar);
            var brand = VxLabel("◢  VEXOR", 25, true, vxText);
            brand.Location = new Point(22, 24);
            vxSidebar.Controls.Add(brand);
            var subtitle = VxLabel("Roblox Account Manager", 9, false, vxMuted);
            subtitle.Location = new Point(38, 78);
            vxSidebar.Controls.Add(subtitle);

            string[] tabs = { "⌂   Dashboard", "♧   Accounts", "▷   Launcher", "▤   Sessions", "▣   VM / RDP", "⚙   Settings", "⚒   Tools" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var b = new Button { Text = tabs[i], TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 12, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = i == 0 ? Color.White : vxMuted, BackColor = i == 0 ? Color.FromArgb(92, 19, 160) : vxSidebar.BackColor,
                    FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(14, 140 + i * 59, 212, 49), Cursor = Cursors.Hand };
                b.FlatAppearance.BorderSize = i == 0 ? 1 : 0;
                b.FlatAppearance.BorderColor = vxAccent;
                b.Click += (s, e) => {
                    if (index == 1) vxSearch.Focus();
                    else if (index == 2) LaunchVexorSelection();
                    else if (index == 5) { MessageBox.Show("Settings are available through the original app controls; a VEXOR settings page is coming soon.", "VEXOR"); }
                    else if (index == 6) { MessageBox.Show("The VEXOR tools page is coming soon.", "VEXOR"); }
                    else if (index == 4) MessageBox.Show("VM / RDP support is planned for a future build.", "VEXOR");
                    else if (index == 3) MessageBox.Show("Session monitoring is planned for a future VEXOR build.", "VEXOR");
                };
                vxSidebar.Controls.Add(b);
            }
            var online = VxLabel("●  VEXOR READY", 11, true, Color.FromArgb(45, 224, 134));
            online.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            online.Location = new Point(25, 650);
            vxSidebar.Controls.Add(online);
            vxSidebar.Resize += (s,e) => online.Top = vxSidebar.Height - 55;

            vxContent = new Panel { Dock = DockStyle.Fill, BackColor = vxBg, Padding = new Padding(25) };
            vxRoot.Controls.Add(vxContent);
            vxContent.BringToFront();

            var heading = new VxHero { Dock = DockStyle.Top, Height = 142 };
            vxContent.Controls.Add(heading);
            var welcome = VxLabel("Welcome back", 25, true, vxText);
            welcome.Location = new Point(16, 26);
            heading.Controls.Add(welcome);
            var intro = VxLabel("Manage, launch, and organize your Roblox accounts.", 11, false, vxMuted);
            intro.Location = new Point(18, 79);
            heading.Controls.Add(intro);

            var stats = new TableLayoutPanel { Dock = DockStyle.Top, Height = 126, ColumnCount = 4, Padding = new Padding(0, 0, 0, 16) };
            for (int i=0; i<4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
            vxContent.Controls.Add(stats);
            vxContent.Controls.SetChildIndex(stats, 0);
            string[] titles = { "♧  Accounts", "▷  Running", "▣  Max Instances", "ϟ  Status" };
            string[] values = { "0 / 10", "—", "10", "Ready" };
            for (int i=0; i<4; i++)
            {
                var p=VxPanel(); p.Dock=DockStyle.Fill; p.Margin=new Padding(7,5,7,5);
                var t=VxLabel(titles[i], 10, false, vxMuted); t.Location=new Point(16,14); p.Controls.Add(t);
                var v=VxLabel(values[i], 22, true, i==3 ? Color.FromArgb(47,224,137) : vxText);
                v.Location=new Point(16,43); p.Controls.Add(v);
                if(i==0) vxCount=v;
                if(i==3) vxStatus=v;
                stats.Controls.Add(p,i,0);
            }

            var columns = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, Padding=new Padding(0,18,0,0) };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,66));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,34));
            vxContent.Controls.Add(columns);
            columns.BringToFront();

            vxMain=VxPanel(); vxMain.Dock=DockStyle.Fill; vxMain.Margin=new Padding(5,0,10,0);
            columns.Controls.Add(vxMain,0,0);
            var accountsTitle=VxLabel("♧  Account Manager",17,true,vxText);
            accountsTitle.Dock=DockStyle.Top; accountsTitle.Height=55; accountsTitle.AutoSize=false;
            vxMain.Controls.Add(accountsTitle);
            vxSearch=new TextBox { Dock=DockStyle.Top, Height=35, BackColor=Color.FromArgb(33,25,52), ForeColor=vxText,
                BorderStyle=BorderStyle.FixedSingle, Font=new Font("Segoe UI",11) };
            vxSearch.TextChanged += (s,e)=>UpdateVexorUi();
            vxMain.Controls.Add(vxSearch);
            vxSearch.BringToFront();
            accountsTitle.BringToFront();

            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top, Height=66, FlowDirection=FlowDirection.LeftToRight, Padding=new Padding(0,8,0,4) };
            vxMain.Controls.Add(toolbar);
            Button add=VxButton("+ Add Account",true,(s,e)=>Add.PerformClick()); add.Dock=DockStyle.None; add.Width=142;
            Button refresh=VxButton("↻ Refresh",false,(s,e)=>UpdateVexorUi()); refresh.Dock=DockStyle.None; refresh.Width=115;
            Button remove=VxButton("Delete Selected",false,(s,e)=>RemoveSelectedVexor()); remove.Dock=DockStyle.None; remove.Width=150;
            toolbar.Controls.Add(add); toolbar.Controls.Add(refresh); toolbar.Controls.Add(remove);
            toolbar.BringToFront(); accountsTitle.BringToFront();

            vxAccounts=new ListView { Dock=DockStyle.Fill, View=View.Details, FullRowSelect=true, MultiSelect=true,
                HideSelection=false, CheckBoxes=true, BorderStyle=BorderStyle.None,
                BackColor=Color.FromArgb(16,12,29), ForeColor=vxText, Font=new Font("Segoe UI",10) };
            vxAccounts.OwnerDraw = true;
            vxAccounts.DrawColumnHeader += (s,e) => { using(var b=new SolidBrush(Color.FromArgb(31,24,52))) e.Graphics.FillRectangle(b,e.Bounds); TextRenderer.DrawText(e.Graphics,e.Header.Text,new Font("Segoe UI",9,FontStyle.Bold),e.Bounds,vxMuted,TextFormatFlags.VerticalCenter|TextFormatFlags.Left); };
            vxAccounts.DrawItem += (s,e) => e.DrawDefault = true;
            vxAccounts.DrawSubItem += (s,e) => e.DrawDefault = true;
            vxAccounts.Columns.Add("#",40);
            vxAccounts.Columns.Add("Account Name",170);
            vxAccounts.Columns.Add("Username",170);
            vxAccounts.Columns.Add("Status",90);
            vxAccounts.SelectedIndexChanged += (s,e)=>UpdateSelectionCount();
            vxAccounts.ItemChecked += (s,e)=>UpdateSelectionCount();
            vxMain.Controls.Add(vxAccounts);
            vxAccounts.BringToFront();

            vxRight=VxPanel(); vxRight.Dock=DockStyle.Fill; vxRight.Margin=new Padding(10,0,5,0);
            columns.Controls.Add(vxRight,1,0);
            var launchTitle=VxLabel("▷  Launch Control",17,true,vxText);
            launchTitle.Dock=DockStyle.Top; launchTitle.AutoSize=false; launchTitle.Height=65;
            vxRight.Controls.Add(launchTitle);
            vxSelected=VxLabel("Selected Accounts: 0",10,false,vxMuted);
            vxSelected.Dock=DockStyle.Top; vxSelected.AutoSize=false; vxSelected.Height=34;
            vxRight.Controls.Add(vxSelected);
            var launch=VxButton("▷  Launch Selected",true,(s,e)=>LaunchVexorSelection());
            vxRight.Controls.Add(launch);
            var launchAll=VxButton("Launch All (selected sequentially)",false,(s,e)=>MessageBox.Show("Select accounts in the list to launch them using the existing launcher. Multiple simultaneous clients are not yet verified.", "VEXOR"));
            vxRight.Controls.Add(launchAll);
            var notice=VxLabel("Launch Mode\nStandard Roblox client",10,false,vxMuted);
            notice.Dock=DockStyle.Top; notice.AutoSize=false; notice.Height=62;
            vxRight.Controls.Add(notice);
            var session=VxLabel("▤  Session Manager",16,true,vxText);
            session.Dock=DockStyle.Top; session.AutoSize=false; session.Height=65;
            vxRight.Controls.Add(session);
            vxEmpty=VxLabel("▣\n\nNo active Roblox sessions\nLaunch an account to see it here.",11,false,vxMuted);
            vxEmpty.Dock=DockStyle.Top; vxEmpty.AutoSize=false; vxEmpty.Height=100;
            vxRight.Controls.Add(vxEmpty);
            // Reverse docking order so the first heading appears at the top.
            vxRight.Controls.SetChildIndex(launchTitle, vxRight.Controls.Count-1);
            vxRight.Controls.SetChildIndex(vxSelected, vxRight.Controls.Count-2);
            vxRight.Controls.SetChildIndex(launch, vxRight.Controls.Count-3);
            vxRight.Controls.SetChildIndex(launchAll, vxRight.Controls.Count-4);
            vxRight.Controls.SetChildIndex(notice, vxRight.Controls.Count-5);
            vxRight.Controls.SetChildIndex(session, vxRight.Controls.Count-6);

            vxTimer=new Timer { Interval=2000 };
            vxTimer.Tick += (s,e)=>UpdateVexorUi();
            vxTimer.Start();
            FormClosed += (s,e)=>{vxTimer.Stop();vxTimer.Dispose();};
        }

        private void UpdateSelectionCount()
        {
            if(vxSelected!=null && vxAccounts!=null)
                vxSelected.Text="Selected Accounts: "+vxAccounts.CheckedItems.Count;
        }

        private void UpdateVexorUi()
        {
            if(vxBusy || vxAccounts==null || AccountsList==null) return;
            vxBusy=true;
            try
            {
                var selected=vxAccounts.CheckedItems.Cast<ListViewItem>().Select(x=>x.Tag).ToArray();
                string query=vxSearch==null ? "" : vxSearch.Text.Trim();
                vxAccounts.BeginUpdate();
                vxAccounts.Items.Clear();
                int number=0;
                foreach(var account in AccountsList.ToArray())
                {
                    if(account==null) continue;
                    string name=string.IsNullOrWhiteSpace(account.Alias) ? account.Username : account.Alias;
                    if(!string.IsNullOrEmpty(query) &&
                        (name??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)<0 &&
                        (account.Username??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)<0) continue;
                    var item=new ListViewItem((++number).ToString());
                    item.SubItems.Add(name??"Unnamed");
                    item.SubItems.Add(account.Username??"Unknown");
                    item.SubItems.Add("Idle");
                    item.Tag=account;
                    item.Checked=selected.Contains(account);
                    vxAccounts.Items.Add(item);
                }
                vxAccounts.EndUpdate();
                vxCount.Text=AccountsList.Count.ToString()+" / 10";
                UpdateSelectionCount();
            }
            finally {vxBusy=false;}
        }

        private void LaunchVexorSelection()
        {
            var selected=vxAccounts.CheckedItems.Cast<ListViewItem>().Select(x=>x.Tag).ToArray();
            if(selected.Length==0){MessageBox.Show("Select an account using its checkbox first.","VEXOR");return;}
            if(selected.Length>1){MessageBox.Show("Launching multiple accounts at once is not yet verified in this build. Please select one account.","VEXOR");return;}
            AccountsView.SelectObject(selected[0]);
            JoinServer.PerformClick();
        }

        private void RemoveSelectedVexor()
        {
            var selected=vxAccounts.CheckedItems.Cast<ListViewItem>().Select(x=>x.Tag).ToArray();
            if(selected.Length!=1){MessageBox.Show("Select exactly one account to remove.","VEXOR");return;}
            AccountsView.SelectObject(selected[0]);
            Remove.PerformClick();
            UpdateVexorUi();
        }
    }
}