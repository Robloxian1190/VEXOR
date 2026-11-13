using System;
using System.Drawing;
using System.Windows.Forms;

namespace RBX_Alt_Manager
{
    public partial class AccountManager
    {
        private readonly Color vxBackground = Color.FromArgb(13, 10, 24);
        private readonly Color vxSurface = Color.FromArgb(28, 20, 48);
        private readonly Color vxPurple = Color.FromArgb(151, 73, 246);
        private Panel vxTop;
        private Label vxSessionLabel;
        private Timer vxRefresh;

        private void InitializeVexorDashboard()
        {
            SuspendLayout();
            Text = "VEXOR  |  Account Studio";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = vxBackground;
            ForeColor = Color.FromArgb(239, 230, 255);
            const int header = 92;
            ClientSize = new Size(Math.Max(ClientSize.Width, 900), ClientSize.Height + header);
            foreach (Control c in Controls)
            {
                if (c == PasswordPanel) continue;
                c.Top += header;
            }
            MinimumSize = new Size(Math.Max(MinimumSize.Width, 920), MinimumSize.Height + header);

            vxTop = new Panel { Name = "VexorTopBar", BackColor = vxSurface, Dock = DockStyle.Top, Height = header };
            vxTop.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = vxPurple });
            vxTop.Controls.Add(new Label {
                Text = "◆  VEXOR", Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(198, 141, 255), AutoSize = true, Location = new Point(22, 11)
            });
            vxTop.Controls.Add(new Label {
                Text = "ACCOUNT STUDIO  /  COMMAND CENTER", Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(174, 153, 200), AutoSize = true, Location = new Point(27, 58)
            });
            vxTop.Controls.Add(MakeVexorButton("＋  ADD ACCOUNT", 360, () => Add.PerformClick()));
            vxTop.Controls.Add(MakeVexorButton("▶  LAUNCH", 506, () => JoinServer.PerformClick()));
            vxSessionLabel = new Label {
                AutoSize = true, Text = "ACCOUNTS  0", ForeColor = Color.FromArgb(198, 141, 255),
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Math.Max(700, ClientSize.Width - 165), 34)
            };
            vxTop.Controls.Add(vxSessionLabel);
            vxTop.Resize += (sender, args) => vxSessionLabel.Left = Math.Max(680, vxTop.Width - 165);
            Controls.Add(vxTop);
            vxTop.BringToFront();

            foreach (Control c in Controls)
            {
                if (c == vxTop || c == PasswordPanel) continue;
                if (c is Button)
                {
                    var b = (Button)c;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = vxPurple;
                    b.FlatAppearance.BorderSize = 1;
                    b.BackColor = vxSurface;
                    b.ForeColor = Color.White;
                    b.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                }
            }
            AccountsView.BackColor = vxSurface;
            AccountsView.ForeColor = Color.FromArgb(239, 230, 255);
            AccountsView.Font = new Font("Segoe UI", 9);
            vxRefresh = new Timer { Interval = 2500 };
            vxRefresh.Tick += (sender, args) => {
                if (!IsDisposed && vxSessionLabel != null)
                    vxSessionLabel.Text = "ACCOUNTS  " + (AccountsList == null ? 0 : AccountsList.Count);
            };
            vxRefresh.Start();
            FormClosed += (sender, args) => { vxRefresh.Stop(); vxRefresh.Dispose(); };
            ResumeLayout(true);
        }

        private Button MakeVexorButton(string label, int left, Action click)
        {
            var b = new Button {
                Text = label, Location = new Point(left, 24), Size = new Size(136, 40),
                BackColor = vxPurple, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (sender, args) => click();
            return b;
        }
    }
}