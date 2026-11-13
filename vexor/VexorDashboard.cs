using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RBX_Alt_Manager
{
    public partial class AccountManager
    {
        private static readonly Color VBg=Color.FromArgb(9,7,20);
        private static readonly Color VCard=Color.FromArgb(18,13,34);
        private static readonly Color VBorder=Color.FromArgb(100,39,149);
        private static readonly Color VPurple=Color.FromArgb(151,21,235);
        private static readonly Color VText=Color.FromArgb(246,240,255);
        private static readonly Color VMuted=Color.FromArgb(177,166,199);

        private sealed class VPanel : Panel
        {
            public int Radius=17;
            public bool Glow=false;
            public VPanel() { DoubleBuffered=true; BackColor=VCard; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var p=new GraphicsPath())
                {
                    int d=Radius*2, w=Math.Max(d+2,Width-2), h=Math.Max(d+2,Height-2);
                    p.AddArc(1,1,d,d,180,90);p.AddArc(w-d,1,d,d,270,90);
                    p.AddArc(w-d,h-d,d,d,0,90);p.AddArc(1,h-d,d,d,90,90);p.CloseFigure();
                    using(var pen=new Pen(Glow?Color.FromArgb(200,54,255):VBorder,Glow?2f:1.3f))
                        e.Graphics.DrawPath(pen,p);
                }
            }
        }
        private sealed class VHero : Panel
        {
            public VHero() { DoubleBuffered=true; BackColor=VBg; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var brush=new LinearGradientBrush(ClientRectangle,
                    Color.FromArgb(31,11,57),VBg,0f))
                    e.Graphics.FillRectangle(brush,ClientRectangle);
                float cx=Width*.75f, h=Height;
                using(var brush=new LinearGradientBrush(new Point(0,0),new Point(0,Math.Max(1,Height)),
                    Color.FromArgb(97,14,205),Color.FromArgb(216,48,250)))
                {
                    e.Graphics.FillPolygon(brush,new PointF[]{new PointF(cx-95,0),new PointF(cx-50,0),new PointF(cx+5,h-12),new PointF(cx-20,h-12)});
                    e.Graphics.FillPolygon(brush,new PointF[]{new PointF(cx+95,0),new PointF(cx+52,0),new PointF(cx-20,h-12),new PointF(cx+5,h-12)});
                }
                using(var pen=new Pen(Color.FromArgb(65,141,23,220),1f))
                    for(int i=0;i<10;i++) e.Graphics.DrawArc(pen,(int)cx-220+i*13,-140+i*8,470,290,175,125);
            }
        }
        private Panel vRoot,vSide,vBody,vRows,vRight;
        private VPanel vAccountsCard,vLaunchCard,vSessionCard;
        private Label vCount,vRunning,vSelected,vStatus,vEmpty;
        private TextBox vSearch;
        private FlowLayoutPanel vAccountRows;
        private Timer vTimer;
        private readonly HashSet<Account> vChecked=new HashSet<Account>();
        private bool vUpdating;
        private Button vAll;
        private readonly List<Button> vNav=new List<Button>();

        private Label VL(string value,int size,bool bold=false,Color? color=null)
        {
            return new Label { Text=value,AutoSize=true,BackColor=Color.Transparent,
                ForeColor=color??VText,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular) };
        }
        private Button VB(string value,EventHandler action,bool primary=false)
        {
            var b=new Button { Text=value,Height=42,BackColor=primary?VPurple:Color.FromArgb(40,30,61),
                ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,
                Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(4) };
            b.FlatAppearance.BorderSize=1;
            b.FlatAppearance.BorderColor=primary?Color.FromArgb(201,73,255):VBorder;
            b.FlatAppearance.MouseOverBackColor=Color.FromArgb(119,27,201);
            b.Click+=action;
            return b;
        }
        private VPanel Card(Control parent)
        {
            var p=new VPanel { Dock=DockStyle.Fill,Margin=new Padding(6),Padding=new Padding(16) };
            parent.Controls.Add(p);return p;
        }
        private void InitializeVexorDashboard()
        {
            Text="VEXOR";
            StartPosition=FormStartPosition.CenterScreen;
            MinimumSize=new Size(1200,760);
            ClientSize=new Size(1580,880);
            Shown+=(s,e)=>{if(vRoot==null) BuildRealVexor();};
        }
        private void BuildRealVexor()
        {
            SuspendLayout();
            vRoot=new Panel { Dock=DockStyle.Fill,BackColor=VBg };
            Controls.Add(vRoot);vRoot.BringToFront();
            vSide=new Panel { Dock=DockStyle.Left,Width=270,BackColor=Color.FromArgb(12,8,25) };
            vRoot.Controls.Add(vSide);
            var brand=VL("◢  VEXOR",26,true);brand.Location=new Point(25,20);vSide.Controls.Add(brand);
            var sub=VL("Account Studio",10,false,VMuted);sub.Location=new Point(70,76);vSide.Controls.Add(sub);
            string[] nav={"⌂   Dashboard","♧   Accounts","▷   Launcher","▤   Sessions","▣   VM / RDP","⚙   Settings","⚒   Tools"};
            for(int i=0;i<nav.Length;i++)
            {
                int index=i;
                var b=VB(nav[i],(s,e)=>NavClick(index),i==0);
                b.TextAlign=ContentAlignment.MiddleLeft;
                b.Font=new Font("Segoe UI",12,i==0?FontStyle.Bold:FontStyle.Regular);
                b.SetBounds(20,140+i*62,230,50);
                b.BackColor=i==0?Color.FromArgb(83,15,151):vSide.BackColor;
                if(i!=0)b.FlatAppearance.BorderSize=0;
                vSide.Controls.Add(b);vNav.Add(b);
            }
            var online=VL("●  VEXOR READY",11,true,Color.FromArgb(36,221,135));
            online.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
            online.Location=new Point(28,vSide.Height-64);vSide.Controls.Add(online);
            vSide.Resize+=(s,e)=>online.Top=vSide.Height-64;

            vBody=new Panel { Dock=DockStyle.Fill,BackColor=VBg,Padding=new Padding(18,16,18,18) };
            vRoot.Controls.Add(vBody);vBody.BringToFront();
            var hero=new VHero { Dock=DockStyle.Top,Height=155 };
            vBody.Controls.Add(hero);
            var hello=VL("Welcome back",26,true);hello.Location=new Point(22,32);hero.Controls.Add(hello);
            var subtitle=VL("Manage, launch, and organize your Roblox accounts.",11,false,VMuted);
            subtitle.Location=new Point(24,86);hero.Controls.Add(subtitle);

            var stats=new TableLayoutPanel { Dock=DockStyle.Top,Height=125,ColumnCount=4,Padding=new Padding(0,8,0,8) };
            for(int i=0;i<4;i++)stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
            vBody.Controls.Add(stats);stats.BringToFront();
            string[] titles={"♧   Accounts","▷   Running","▣   Max Instances","ϟ   Status"};
            string[] vals={"0","—","10","Ready"};
            for(int i=0;i<4;i++)
            {
                var p=Card(stats);stats.Controls.Add(p,i,0);
                var caption=VL(titles[i],10,false,VMuted);caption.Location=new Point(18,15);p.Controls.Add(caption);
                var val=VL(vals[i],22,true,i==3?Color.FromArgb(30,222,125):VText);
                val.Location=new Point(20,46);p.Controls.Add(val);
                if(i==0)vCount=val;
                if(i==1)vRunning=val;
                if(i==3)vStatus=val;
            }

            var split=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(0,14,0,0) };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));
            vBody.Controls.Add(split);split.BringToFront();
            vAccountsCard=Card(split);split.Controls.Add(vAccountsCard,0,0);
            var leftHeader=new Panel { Dock=DockStyle.Top,Height=54 };
            vAccountsCard.Controls.Add(leftHeader);
            var accountTitle=VL("♧  Account Manager",17,true);
            accountTitle.Location=new Point(3,8);leftHeader.Controls.Add(accountTitle);
            vSearch=new TextBox { Anchor=AnchorStyles.Top|AnchorStyles.Right,Width=240,
                Font=new Font("Segoe UI",11),BackColor=Color.FromArgb(31,22,51),
                ForeColor=VText,BorderStyle=BorderStyle.FixedSingle };
            leftHeader.Controls.Add(vSearch);
            leftHeader.Resize+=(s,e)=>vSearch.Left=leftHeader.Width-vSearch.Width-3;
            vSearch.TextChanged+=(s,e)=>UpdateRealVexor();

            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=58,WrapContents=false };
            vAccountsCard.Controls.Add(toolbar);
            vAll=VB("☐  Select All",(s,e)=>SelectAllReal());vAll.Width=135;toolbar.Controls.Add(vAll);
            var add=VB("+  Add Account",(s,e)=>Add.PerformClick(),true);add.Width=145;toolbar.Controls.Add(add);
            var import=VB("⇧ Import",(s,e)=>byCookieToolStripMenuItem_Click(s,e));import.Width=100;toolbar.Controls.Add(import);
            var export=VB("⇩ Export",(s,e)=>ExportRealVexor());export.Width=100;toolbar.Controls.Add(export);
            var delete=VB("🗑 Delete",(s,e)=>DeleteRealVexor());delete.Width=100;toolbar.Controls.Add(delete);

            var headings=new Panel { Dock=DockStyle.Top,Height=40,BackColor=Color.FromArgb(29,22,48) };
            vAccountsCard.Controls.Add(headings);
            string[] cols={"☐     #","Account Name","Username","Status","Actions"};
            int[] loc={8,94,300,515,635};
            for(int i=0;i<cols.Length;i++)
            {var l=VL(cols[i],9,true,VMuted);l.Location=new Point(loc[i],12);headings.Controls.Add(l);}
            vAccountRows=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,
                WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(13,10,27) };
            vAccountsCard.Controls.Add(vAccountRows);
            vAccountRows.BringToFront();
            vAccountsCard.Controls.SetChildIndex(leftHeader,3);
            vAccountsCard.Controls.SetChildIndex(toolbar,2);
            vAccountsCard.Controls.SetChildIndex(headings,1);

            var rightStack=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=2 };
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent,54));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent,46));
            split.Controls.Add(rightStack,1,0);
            vLaunchCard=Card(rightStack);rightStack.Controls.Add(vLaunchCard,0,0);
            var launchHead=VL("▷  Launch Control",17,true);launchHead.Location=new Point(18,16);vLaunchCard.Controls.Add(launchHead);
            vSelected=VL("Selected Accounts: 0",10,false,VMuted);vSelected.Anchor=AnchorStyles.Right|AnchorStyles.Top;
            vLaunchCard.Controls.Add(vSelected);
            vLaunchCard.Resize+=(s,e)=>vSelected.Location=new Point(Math.Max(190,vLaunchCard.Width-vSelected.Width-18),25);
            var launch=VB("▷  Launch Selected",(s,e)=>LaunchRealVexor(),true);
            launch.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right;
            launch.SetBounds(18,83,Math.Max(150,vLaunchCard.Width-36),53);vLaunchCard.Controls.Add(launch);
            var launchAll=VB("Launch All (not yet supported)",(s,e)=>MessageBox.Show(
                "Multiple simultaneous Roblox clients are not verified. Select one account to launch.","VEXOR"));
            launchAll.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right;
            launchAll.SetBounds(18,149,Math.Max(150,vLaunchCard.Width-36),49);vLaunchCard.Controls.Add(launchAll);
            vLaunchCard.Resize+=(s,e)=>{launch.Width=vLaunchCard.Width-36;launchAll.Width=vLaunchCard.Width-36;};
            var mode=VL("Launch Mode",10,false,VMuted);mode.Location=new Point(19,218);vLaunchCard.Controls.Add(mode);
            var standard=VL("Standard Roblox client",12,true);standard.Location=new Point(19,246);vLaunchCard.Controls.Add(standard);
            var info=VL("Saved sessions use the existing account manager.\nMulti-instance and VM controls are not available yet.",
                9,false,VMuted);info.Location=new Point(19,288);vLaunchCard.Controls.Add(info);

            vSessionCard=Card(rightStack);rightStack.Controls.Add(vSessionCard,0,1);
            var sessionTitle=VL("▤  Session Manager",17,true);sessionTitle.Location=new Point(18,17);vSessionCard.Controls.Add(sessionTitle);
            vEmpty=VL("▣\n\nNo active Roblox sessions\nLaunch an account to get started.",
                12,false,VMuted);vEmpty.TextAlign=ContentAlignment.MiddleCenter;
            vEmpty.AutoSize=false;vEmpty.Dock=DockStyle.Fill;vSessionCard.Controls.Add(vEmpty);
            sessionTitle.BringToFront();

            vTimer=new Timer { Interval=1800 };
            vTimer.Tick+=(s,e)=>UpdateRealVexor();
            vTimer.Start();
            FormClosed+=(s,e)=>{vTimer.Stop();vTimer.Dispose();};
            ResumeLayout(true);
            UpdateRealVexor();
        }

        private void NavClick(int i)
        {
            if(i==0)UpdateRealVexor();
            if(i==1)vSearch.Focus();
            if(i==2)LaunchRealVexor();
            if(i==3)MessageBox.Show("Live session tracking is planned for a future version.","VEXOR");
            if(i==4)MessageBox.Show("VM / RDP is not implemented yet.","VEXOR");
            if(i==5)ConfigButton_Click(this,EventArgs.Empty);
            if(i==6){UtilsForm.Show();UtilsForm.BringToFront();}
        }
        private void UpdateRealVexor()
        {
            if(vUpdating||vAccountRows==null||AccountsList==null)return;
            vUpdating=true;
            try
            {
                vChecked.RemoveWhere(a=>!AccountsList.Contains(a));
                var query=vSearch.Text.Trim();
                var visible=AccountsList.Where(a=>a!=null&&(query.Length==0||
                    (a.Username??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||
                    (a.Alias??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
                vCount.Text=AccountsList.Count.ToString();
                vSelected.Text="Selected Accounts: "+vChecked.Count;
                // Do not show invented session counts.
                vRunning.Text="—";
                if(vAccountRows.Tag is string && (string)vAccountRows.Tag==query+"|"+AccountsList.Count+"|"+vChecked.Count)
                    return;
                vAccountRows.Tag=query+"|"+AccountsList.Count+"|"+vChecked.Count;
                vAccountRows.SuspendLayout();vAccountRows.Controls.Clear();
                for(int i=0;i<visible.Length;i++)
                {
                    var account=visible[i];
                    var line=new Panel { Height=44,Width=Math.Max(690,vAccountRows.ClientSize.Width-25),
                        BackColor=i%2==0?Color.FromArgb(23,17,39):Color.FromArgb(15,11,29),
                        Margin=new Padding(0,0,0,1) };
                    var check=new CheckBox { Checked=vChecked.Contains(account),Location=new Point(10,12),
                        Size=new Size(22,22),ForeColor=VText };
                    check.CheckedChanged+=(s,e)=>{if(check.Checked)vChecked.Add(account);else vChecked.Remove(account);
                        vSelected.Text="Selected Accounts: "+vChecked.Count;vAccountRows.Tag=null;};
                    line.Controls.Add(check);
                    var number=VL((i+1).ToString(),10,false,VMuted);number.Location=new Point(52,12);line.Controls.Add(number);
                    var name=VL(string.IsNullOrWhiteSpace(account.Alias)?account.Username:account.Alias,10,true);
                    name.Location=new Point(100,12);line.Controls.Add(name);
                    var username=VL(account.Username??"",10);username.Location=new Point(310,12);line.Controls.Add(username);
                    var idle=VL("● Idle",10,false,VMuted);idle.Location=new Point(525,12);line.Controls.Add(idle);
                    var play=VB("▷",(s,e)=>LaunchOneReal(account),true);play.SetBounds(645,4,43,35);
                    line.Controls.Add(play);vAccountRows.Controls.Add(line);
                }
                vAccountRows.ResumeLayout(true);
            }
            finally{vUpdating=false;}
        }
        private void SelectAllReal()
        {
            var q=vSearch.Text.Trim();
            var matches=AccountsList.Where(a=>a!=null&&(q.Length==0||
                (a.Username??"").IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0||
                (a.Alias??"").IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            bool all=matches.All(a=>vChecked.Contains(a));
            foreach(var a in matches){if(all)vChecked.Remove(a);else vChecked.Add(a);}
            vAccountRows.Tag=null;UpdateRealVexor();
        }
        private void LaunchOneReal(Account a)
        {
            AccountsView.SelectObject(a);JoinServer.PerformClick();
        }
        private void LaunchRealVexor()
        {
            if(vChecked.Count!=1){MessageBox.Show("Select exactly one account to launch.","VEXOR");return;}
            LaunchOneReal(vChecked.First());
        }
        private void DeleteRealVexor()
        {
            if(vChecked.Count!=1){MessageBox.Show("Select exactly one account to delete.","VEXOR");return;}
            AccountsView.SelectObject(vChecked.First());Remove.PerformClick();
            vChecked.Clear();vAccountRows.Tag=null;UpdateRealVexor();
        }
        private void ExportRealVexor()
        {
            using(var dlg=new SaveFileDialog { Filter="CSV files|*.csv",FileName="VEXOR-accounts.csv" })
            {
                if(dlg.ShowDialog()!=DialogResult.OK)return;
                Func<string,string> esc=x=>((char)34).ToString()+(x??"").Replace(((char)34).ToString(),new string((char)34,2))+((char)34).ToString();
                var lines=new List<string>{"Alias,Username"};
                foreach(var a in AccountsList)lines.Add(esc(a.Alias)+","+esc(a.Username));
                File.WriteAllLines(dlg.FileName,lines);
                MessageBox.Show("Exported account names only; no credentials were included.","VEXOR");
            }
        }
    }
}