using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace VEXOR;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Dashboard());
    }
}

public sealed class AccountProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool HasEncryptedCookie { get; set; }
}

public sealed class Dashboard : Form
{
    private static readonly Color Background = Color.FromArgb(10, 8, 23);
    private static readonly Color PanelColor = Color.FromArgb(23, 17, 43);
    private static readonly Color Purple = Color.FromArgb(137, 49, 228);
    private static readonly Color Muted = Color.FromArgb(183, 170, 206);
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VEXOR");
    private static readonly string DataFile = Path.Combine(DataFolder, "profiles.json");
    private static readonly string CookieFolder = Path.Combine(DataFolder, "sessions");

    private readonly List<AccountProfile> profiles = new();
    private readonly DataGridView table = new();
    private readonly TextBox search = new();
    private readonly Label total = new();
    private readonly Label status = new();
    private readonly Label selectedName = new();
    private readonly Label empty = new();
    private readonly Label running = new();
    private readonly System.Windows.Forms.Timer sessionTimer = new() { Interval = 2500 };
    private readonly BindingSource binding = new();

    public Dashboard()
    {
        Text = "VEXOR";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1370, 830);
        MinimumSize = new Size(1050, 680);
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        LoadProfiles();
        Build();
        RefreshProfiles();
        sessionTimer.Tick += (_, _) => RefreshSessions();
        sessionTimer.Start();
        RefreshSessions();
        FormClosed += (_, _) => sessionTimer.Dispose();
    }

    private void RefreshSessions()
    {
        try { running.Text = Process.GetProcessesByName("RobloxPlayerBeta").Length.ToString(); }
        catch { running.Text = "—"; }
    }

    private static Label Label(string text, int size, bool bold = false, Color? color = null)
        => new() { Text = text, AutoSize = true, ForeColor = color ?? Color.White,
            BackColor = Color.Transparent, Font = new Font("Segoe UI", size,
            bold ? FontStyle.Bold : FontStyle.Regular) };

    private static Button Button(string text, Action action, bool primary = false)
    {
        var b = new Button { Text = text, Height = 42, AutoSize = false,
            BackColor = primary ? Purple : Color.FromArgb(44, 34, 68),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(4) };
        b.FlatAppearance.BorderColor = primary ? Purple : Color.FromArgb(83, 57, 116);
        b.Click += (_, _) => action();
        return b;
    }

    private void Build()
    {
        MinimumSize = new Size(1200, 740);
        Size = new Size(1560, 920);
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2,
            Margin = Padding.Empty, BackColor = Background };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        var side = new NeonPanel { Dock = DockStyle.Fill, Margin = Padding.Empty,
            BackColor = Color.FromArgb(12, 7, 27), DrawBorder = false };
        shell.Controls.Add(side, 0, 0);
        var logo = Label("◢", 40, true, Color.FromArgb(194, 68, 255));
        logo.Location = new Point(25, 14); side.Controls.Add(logo);
        var brand = Label("VEXOR", 26, true);
        brand.Location = new Point(94, 24); side.Controls.Add(brand);
        var sub = Label("ACCOUNT MANAGER", 10, false, Muted);
        sub.Location = new Point(95, 77); side.Controls.Add(sub);
        string[] nav = { "⌂    Dashboard", "♧    Accounts", "▷    Launcher",
            "▤    Sessions", "▣    VM / RDP", "⚙    Settings", "⚒    Tools" };
        for (int i = 0; i < nav.Length; i++)
        {
            int index = i;
            var button = Button(nav[i], () => Navigate(index < 4 ? index : index == 5 ? 4 : 6), i == 0);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Font = new Font("Segoe UI", 12, i == 0 ? FontStyle.Bold : FontStyle.Regular);
            button.SetBounds(20, 145 + i * 61, 230, 49);
            if (i != 0) { button.BackColor = Color.FromArgb(18, 12, 37);
                button.FlatAppearance.BorderSize = 0; }
            side.Controls.Add(button);
        }
        var engine = new NeonPanel { Height = 78, BackColor = Color.FromArgb(20, 14, 42),
            DrawBorder = true, BorderColor = Color.FromArgb(81, 42, 133) };
        side.Controls.Add(engine);
        var engineTitle = Label("●  VEXOR ONLINE", 11, true, Color.FromArgb(44, 229, 136));
        engineTitle.Location = new Point(15, 12); engine.Controls.Add(engineTitle);
        var engineDesc = Label("Independent account studio", 9, false, Muted);
        engineDesc.Location = new Point(16, 40); engine.Controls.Add(engineDesc);
        side.Resize += (_, _) => engine.SetBounds(20, side.Height - 102, side.Width - 40, 78);
        engine.SetBounds(20, 660, 230, 78);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(22, 18, 22, 18), Margin = Padding.Empty, BackColor = Background };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(main, 1, 0);
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(top, 0, 0);
        var hero = new GradientHero { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 8) };
        top.Controls.Add(hero, 0, 0);
        var welcome = Label("Welcome back to VEXOR", 25, true);
        welcome.Location = new Point(20, 25); hero.Controls.Add(welcome);
        var subtitle = Label("Manage, launch and organize your Roblox account profiles.", 11, false, Muted);
        subtitle.Location = new Point(22, 80); hero.Controls.Add(subtitle);
        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
            Margin = Padding.Empty };
        for (int i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        top.Controls.Add(stats, 0, 1);
        AddStat(stats, 0, "♧  ACCOUNTS", total, "0");
        AddStat(stats, 1, "▷  ROBLOX PROCESSES", running, "0");
        AddStat(stats, 2, "▣  LAUNCH MODE", Label("Browser", 17, true), "");
        AddStat(stats, 3, "ϟ  STATUS", status, "Ready");

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2,
            Margin = Padding.Empty, Padding = new Padding(0, 8, 0, 0) };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        main.Controls.Add(workspace, 0, 1);
        var accountCard = new NeonPanel { Dock = DockStyle.Fill, Margin = new Padding(4, 4, 10, 4),
            Padding = new Padding(14), BackColor = PanelColor };
        workspace.Controls.Add(accountCard, 0, 0);
        var accountGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        accountGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        accountCard.Controls.Add(accountGrid);
        var accountHeader = new Panel { Dock = DockStyle.Fill };
        accountGrid.Controls.Add(accountHeader, 0, 0);
        var accountHeading = Label("♧  Account Manager", 18, true);
        accountHeading.Location = new Point(8, 12); accountHeader.Controls.Add(accountHeading);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            WrapContents = true, Margin = Padding.Empty };
        accountGrid.Controls.Add(toolbar, 0, 1);
        search.Width = 215; search.Font = new Font("Segoe UI", 11);
        search.PlaceholderText = "Search accounts...";
        search.Margin = new Padding(4, 8, 6, 4);
        search.TextChanged += (_, _) => RefreshProfiles();
        toolbar.Controls.Add(search);
        AddToolbar(toolbar, "+ Add Account", 132, AddProfile, true);
        AddToolbar(toolbar, "Import", 85, ImportProfiles);
        AddToolbar(toolbar, "Export", 85, ExportProfiles);
        AddToolbar(toolbar, "Delete", 85, DeleteProfile);
        AddToolbar(toolbar, "Edit", 80, EditProfile);
        AddToolbar(toolbar, "Duplicate", 100, DuplicateProfile);
        AddToolbar(toolbar, "Sign in", 85, SignInOfficial);

        table.Dock = DockStyle.Fill;
        table.Margin = new Padding(0, 5, 0, 4);
        table.BackgroundColor = PanelColor;
        table.BorderStyle = BorderStyle.None;
        table.GridColor = Color.FromArgb(50, 32, 75);
        table.EnableHeadersVisualStyles = false;
        table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 24, 59);
        table.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(226, 210, 244);
        table.ColumnHeadersHeight = 42;
        table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        table.DefaultCellStyle.BackColor = PanelColor;
        table.DefaultCellStyle.ForeColor = Color.White;
        table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(86, 31, 139);
        table.DefaultCellStyle.SelectionForeColor = Color.White;
        table.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(29, 19, 48);
        table.RowTemplate.Height = 40;
        table.AllowUserToAddRows = false;
        table.AllowUserToDeleteRows = false;
        table.ReadOnly = true; table.MultiSelect = false;
        table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        table.AutoGenerateColumns = false;
        table.RowHeadersVisible = false;
        table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        table.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AccountProfile.Alias), HeaderText = "ACCOUNT NAME" });
        table.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AccountProfile.Username), HeaderText = "USERNAME" });
        table.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(AccountProfile.HasEncryptedCookie), HeaderText = "SESSION", FillWeight = 55 });
        table.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AccountProfile.Notes), HeaderText = "NOTES" });
        table.DataSource = binding;
        table.SelectionChanged += (_, _) => selectedName.Text =
            CurrentProfile() is { } p ? "Selected: " + p.Alias : "Selected Accounts: 0";
        accountGrid.Controls.Add(table, 0, 2);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2,
            Margin = Padding.Empty };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 53));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        workspace.Controls.Add(right, 1, 0);
        var launch = new NeonPanel { Dock = DockStyle.Fill, Margin = new Padding(6, 4, 4, 12),
            Padding = new Padding(16), BackColor = PanelColor };
        right.Controls.Add(launch, 0, 0);
        var launchLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5 };
        launchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        launch.Controls.Add(launchLayout);
        var launchTitle = Label("▷  Launch Control", 18, true);
        launchTitle.Margin = new Padding(6, 8, 0, 0);
        launchLayout.Controls.Add(launchTitle, 0, 0);
        var launchSelected = Button("▷  Open Selected Account", () => {
            if (CurrentProfile() is not { } profile) {
                MessageBox.Show("Select an account profile first.", "VEXOR"); return;
            }
            OpenRoblox();
            MessageBox.Show("Opened Roblox. Account-specific automatic sign-in is not yet available for " + profile.Alias + ".", "VEXOR");
        }, true);
        launchSelected.Dock = DockStyle.Fill;
        launchLayout.Controls.Add(launchSelected, 0, 1);
        var launchAll = Button("↗  Open Roblox", OpenRoblox);
        launchAll.Dock = DockStyle.Fill;
        launchLayout.Controls.Add(launchAll, 0, 2);
        var modeTitle = Label("LAUNCH MODE", 9, false, Muted);
        modeTitle.Margin = new Padding(5, 12, 0, 0);
        launchLayout.Controls.Add(modeTitle, 0, 3);
        var mode = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 20, 50) };
        launchLayout.Controls.Add(mode, 0, 4);
        var modeDesc = Label("Official Roblox website\nAccount-specific game launching: not yet available", 10, false, Muted);
        modeDesc.Location = new Point(12, 17);
        mode.Controls.Add(modeDesc);
        var session = new NeonPanel { Dock = DockStyle.Fill, Margin = new Padding(6, 4, 4, 4),
            Padding = new Padding(16), BackColor = PanelColor };
        right.Controls.Add(session, 0, 1);
        var sessionHeading = Label("▤  Session Manager", 18, true);
        sessionHeading.Location = new Point(18, 18); session.Controls.Add(sessionHeading);
        var divider = new Panel { BackColor = Color.FromArgb(69, 35, 109), Height = 1,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        divider.SetBounds(18, 72, 310, 1); session.Controls.Add(divider);
        var sessionEmpty = Label("▣\n\nRoblox process monitoring active", 13, true, Muted);
        sessionEmpty.TextAlign = ContentAlignment.MiddleCenter;
        session.Controls.Add(sessionEmpty);
        session.Resize += (_, _) => {
            divider.Width = Math.Max(10, session.ClientSize.Width - 36);
            sessionEmpty.SetBounds(16, 94, Math.Max(10, session.ClientSize.Width - 32),
                Math.Max(70, session.ClientSize.Height - 125));
        };
        sessionEmpty.SetBounds(16, 94, 310, 140);
    }

    private static void AddStat(TableLayoutPanel cards, int index, string name, Label value, string initial)
    {
        var box = new NeonPanel { Dock = DockStyle.Fill, BackColor = PanelColor, Margin = new Padding(5, 0, 5, 6) };
        cards.Controls.Add(box, index, 0);
        var label = Label(name, 9, false, Muted);
        label.Location = new Point(14, 12);
        box.Controls.Add(label);
        value.Text = initial;
        value.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        value.Location = new Point(14, 40);
        value.AutoSize = true;
        box.Controls.Add(value);
    }

    private static void AddToolbar(FlowLayoutPanel parent, string text, int width, Action action, bool primary = false)
    {
        var b = Button(text, action, primary);
        b.Width = width;
        parent.Controls.Add(b);
    }

    private void Navigate(int index)
    {
        if (index == 0 || index == 1) { search.Focus(); return; }
        if (index == 2) { OpenRoblox(); return; }
        MessageBox.Show(index == 3 ? "Session tracking is planned, not implemented yet." :
            "VEXOR saves profiles in: " + DataFolder, "VEXOR", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadProfiles()
    {
        try
        {
            if (File.Exists(DataFile))
                profiles.AddRange(JsonSerializer.Deserialize<List<AccountProfile>>(File.ReadAllText(DataFile)) ?? new());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not load VEXOR profiles. No data was overwritten.\n" + ex.Message,
                "VEXOR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool SaveProfiles()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            var temp = DataFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, DataFile, true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to save profiles: " + ex.Message, "VEXOR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void RefreshProfiles()
    {
        var query = search.Text.Trim();
        var selected = CurrentProfile()?.Id;
        binding.DataSource = profiles.Where(p =>
            p.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Alias.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        total.Text = profiles.Count.ToString();
        table.ClearSelection();
        if (selected.HasValue)
            foreach (DataGridViewRow row in table.Rows)
                if (row.DataBoundItem is AccountProfile p && p.Id == selected.Value)
                    { row.Selected = true; break; }
    }

    private AccountProfile? CurrentProfile() =>
        table.SelectedRows.Count > 0 ? table.SelectedRows[0].DataBoundItem as AccountProfile : null;

    private static string? Prompt(string title, string initial = "")
    {
        using var dialog = new Form { Text = title, Size = new Size(430, 180),
            StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, BackColor = PanelColor, ForeColor = Color.White };
        var field = new TextBox { Left = 20, Top = 20, Width = 372, Text = initial };
        var ok = Button("Save", () => dialog.DialogResult = DialogResult.OK, true);
        ok.SetBounds(280, 70, 112, 38);
        dialog.Controls.Add(field);
        dialog.Controls.Add(ok);
        dialog.AcceptButton = ok;
        return dialog.ShowDialog() == DialogResult.OK ? field.Text.Trim() : null;
    }

    private void AddProfile()
    {
        using var choice = new Form {
            Text = "Add Roblox account • VEXOR", Size = new Size(470, 270),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
            MinimizeBox = false, BackColor = PanelColor, ForeColor = Color.White
        };
        var title = Label("Choose an account method", 16, true);
        title.Location = new Point(20, 18);
        choice.Controls.Add(title);
        var official = Button("Sign in with Roblox", () => {
            choice.DialogResult = DialogResult.Yes;
        }, true);
        official.SetBounds(22, 72, 410, 48);
        choice.Controls.Add(official);
        var cookie = Button("Import .ROBLOSECURITY cookie", () => {
            choice.DialogResult = DialogResult.No;
            choice.Close();
        });
        cookie.SetBounds(22, 132, 410, 48);
        choice.Controls.Add(cookie);
        var result = choice.ShowDialog(this);
        if (result == DialogResult.Yes) {
            AddBasicProfile();
            SignInOfficial();
        } else if (result == DialogResult.No) {
            ImportCookieProfile();
        }
    }

    private void AddBasicProfile()
    {
        var username = Prompt("Roblox username");
        if (string.IsNullOrWhiteSpace(username)) return;
        var alias = Prompt("Profile display name", username);
        if (alias is null) return;
        profiles.Add(new AccountProfile { Username = username, Alias = string.IsNullOrWhiteSpace(alias) ? username : alias });
        SaveProfiles();
        RefreshProfiles();
    }

    private void SignInOfficial()
    {
        try {
            Process.Start(new ProcessStartInfo("https://www.roblox.com/login") { UseShellExecute = true });
            MessageBox.Show("Sign in on the official Roblox website. VEXOR does not collect or store your password.\\n\\nYou can add a username profile separately. Official browser login is not automatically linked to a VEXOR profile.", "VEXOR • Official login", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) { MessageBox.Show("Could not open Roblox login: " + ex.Message); }
    }

    private void ImportCookieProfile()
    {
        var username = Prompt("Roblox username for this session");
        if (string.IsNullOrWhiteSpace(username)) return;
        var alias = Prompt("Profile display name", username);
        if (alias is null) return;
        var cookie = PromptSecret("Paste your .ROBLOSECURITY session cookie");
        if (string.IsNullOrWhiteSpace(cookie)) return;
        try {
            var p = new AccountProfile { Username = username, Alias = string.IsNullOrWhiteSpace(alias) ? username : alias, HasEncryptedCookie = true };
            Directory.CreateDirectory(CookieFolder);
            var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(cookie.Trim()), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(Path.Combine(CookieFolder, p.Id.ToString("N") + ".bin"), encrypted);
            profiles.Add(p);
            SaveProfiles();
            RefreshProfiles();
            MessageBox.Show("Cookie encrypted for your Windows account. VEXOR does not yet use saved cookies to sign into Roblox or launch clients.", "VEXOR", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) { MessageBox.Show("Could not save encrypted session: " + ex.Message, "VEXOR"); }
    }

    private static string? PromptSecret(string title)
    {
        using var dialog = new Form {
            Text = title, Size = new Size(520, 200), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
            BackColor = PanelColor, ForeColor = Color.White
        };
        var input = new TextBox { Left = 18, Top = 18, Width = 465, Height = 60, Multiline = true,
            UseSystemPasswordChar = false, ScrollBars = ScrollBars.Vertical };
        var save = Button("Encrypt & save", () => dialog.DialogResult = DialogResult.OK, true);
        save.SetBounds(300, 95, 183, 40);
        dialog.Controls.Add(input);
        dialog.Controls.Add(save);
        dialog.AcceptButton = save;
        return dialog.ShowDialog() == DialogResult.OK ? input.Text : null;
    }

    private void EditProfile()
    {
        var profile = CurrentProfile();
        if (profile is null) { MessageBox.Show("Select a profile first."); return; }
        var alias = Prompt("Edit profile name", profile.Alias);
        if (string.IsNullOrWhiteSpace(alias)) return;
        profile.Alias = alias;
        var notes = Prompt("Profile notes", profile.Notes);
        if (notes is not null) profile.Notes = notes;
        SaveProfiles();
        RefreshProfiles();
    }

    private void DuplicateProfile()
    {
        var p = CurrentProfile();
        if (p is null) { MessageBox.Show("Select a profile first."); return; }
        profiles.Add(new AccountProfile { Username = p.Username, Alias = p.Alias + " (copy)", Notes = p.Notes, HasEncryptedCookie = false });
        SaveProfiles();
        RefreshProfiles();
    }

    private void DeleteProfile()
    {
        var profile = CurrentProfile();
        if (profile is null) { MessageBox.Show("Select a profile first."); return; }
        if (MessageBox.Show("Delete profile '" + profile.Alias + "'?", "VEXOR",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        profiles.Remove(profile);
        var secretPath = Path.Combine(CookieFolder, profile.Id.ToString("N") + ".bin");
        if (File.Exists(secretPath)) File.Delete(secretPath);
        SaveProfiles();
        RefreshProfiles();
    }

    private void ImportProfiles()
    {
        using var picker = new OpenFileDialog { Filter = "VEXOR profile JSON|*.json" };
        if (picker.ShowDialog() != DialogResult.OK) return;
        try
        {
            var incoming = JsonSerializer.Deserialize<List<AccountProfile>>(File.ReadAllText(picker.FileName)) ?? new();
            int added = 0;
            foreach (var profile in incoming)
            {
                if (string.IsNullOrWhiteSpace(profile.Username)) continue;
                if (profiles.Any(p => p.Id == profile.Id)) continue;
                profile.Alias = string.IsNullOrWhiteSpace(profile.Alias) ? profile.Username : profile.Alias;
                profiles.Add(profile);
                added++;
            }
            SaveProfiles();
            RefreshProfiles();
            MessageBox.Show("Imported " + added + " profiles.", "VEXOR");
        }
        catch (Exception ex) { MessageBox.Show("Import failed: " + ex.Message, "VEXOR"); }
    }

    private void ExportProfiles()
    {
        using var picker = new SaveFileDialog { Filter = "VEXOR profile JSON|*.json", FileName = "VEXOR-profiles.json" };
        if (picker.ShowDialog() != DialogResult.OK) return;
        try { File.WriteAllText(picker.FileName, JsonSerializer.Serialize(profiles,
            new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { MessageBox.Show("Export failed: " + ex.Message, "VEXOR"); }
    }

    private sealed class NeonPanel : Panel
    {
        public bool DrawBorder = true;
        public Color BorderColor = Color.FromArgb(115, 35, 169);
        public NeonPanel() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!DrawBorder) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            int w = Math.Max(24, Width - 2), h = Math.Max(24, Height - 2), r = 15;
            path.AddArc(1, 1, r, r, 180, 90);
            path.AddArc(w - r, 1, r, r, 270, 90);
            path.AddArc(w - r, h - r, r, r, 0, 90);
            path.AddArc(1, h - r, r, r, 90, 90);
            path.CloseFigure();
            using var pen = new Pen(BorderColor, 1.5f);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private sealed class GradientHero : Panel
    {
        public GradientHero() { DoubleBuffered = true; }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(46, 19, 89), Color.FromArgb(19, 12, 42), 0f);
            e.Graphics.FillRectangle(brush, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(66, 164, 65, 255), 1.5f);
            for (int i = 0; i < 9; i++)
                e.Graphics.DrawEllipse(pen, Width - 290 + i * 19, -120 + i * 8, 400, 270);
            using var glow = new LinearGradientBrush(
                new Point(Math.Max(0, Width - 350), 0), new Point(Math.Max(1, Width - 150), Math.Max(1, Height)),
                Color.FromArgb(173, 34, 240), Color.FromArgb(92, 16, 152));
            var cx = Width * .78f;
            e.Graphics.FillPolygon(glow, new PointF[] {
                new(cx - 105, 0), new(cx - 65, 0), new(cx + 1, Height - 10), new(cx - 24, Height - 10) });
            e.Graphics.FillPolygon(glow, new PointF[] {
                new(cx + 100, 0), new(cx + 62, 0), new(cx - 24, Height - 10), new(cx + 1, Height - 10) });
        }
    }

    private static void OpenRoblox()
    {
        try { Process.Start(new ProcessStartInfo("https://www.roblox.com/home") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show("Could not open Roblox: " + ex.Message, "VEXOR"); }
    }
}
