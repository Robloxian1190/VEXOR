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
        // Explicit table grids prevent WinForms Dock.Fill controls from
        // overlapping their siblings when DPI or window size changes.
        var shell = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Margin = Padding.Empty, Padding = Padding.Empty,
            BackColor = Background
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        var sidebar = new Panel {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 11, 32),
            Margin = Padding.Empty
        };
        shell.Controls.Add(sidebar, 0, 0);
        var logo = Label("◈  VEXOR", 25, true);
        logo.Location = new Point(22, 25);
        sidebar.Controls.Add(logo);
        var tagline = Label("ACCOUNT STUDIO", 10, false, Muted);
        tagline.Location = new Point(30, 84);
        sidebar.Controls.Add(tagline);
        var navigation = new[] { "⌂  Dashboard", "♧  Accounts", "▷  Launcher", "▤  Sessions", "⚙  Settings" };
        for (int i = 0; i < navigation.Length; i++)
        {
            int index = i;
            var nav = Button("  " + navigation[i], () => Navigate(index), i == 0);
            nav.TextAlign = ContentAlignment.MiddleLeft;
            nav.SetBounds(18, 155 + i * 62, 197, 48);
            sidebar.Controls.Add(nav);
        }
        var note = Label("●  INDEPENDENT BUILD", 9, true, Color.FromArgb(73, 220, 158));
        note.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        sidebar.Controls.Add(note);
        sidebar.Resize += (_, _) => note.Location = new Point(20, sidebar.ClientSize.Height - 60);
        note.Location = new Point(20, 570);

        var main = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Margin = Padding.Empty, Padding = new Padding(20, 18, 20, 18),
            BackColor = Background
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(main, 1, 0);

        var hero = new GradientHero {
            Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 10),
            BackColor = Color.FromArgb(34, 17, 66)
        };
        main.Controls.Add(hero, 0, 0);
        var welcome = Label("Welcome back to VEXOR", 26, true);
        welcome.Location = new Point(24, 25);
        hero.Controls.Add(welcome);
        var subtitle = Label("Manage profiles, monitor Roblox and organize your workspace.", 11, false, Muted);
        subtitle.Location = new Point(26, 86);
        hero.Controls.Add(subtitle);
        var mark = Label("V", 66, true, Color.FromArgb(175, 89, 255));
        hero.Controls.Add(mark);
        hero.Resize += (_, _) => mark.Location = new Point(hero.ClientSize.Width - 130, 16);

        var cards = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        for (int i = 0; i < 3; i++)
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
        main.Controls.Add(cards, 0, 1);
        AddStat(cards, 0, "SAVED PROFILES", total, "0");
        AddStat(cards, 1, "ROBLOX PROCESSES", running, "0");
        AddStat(cards, 2, "VEXOR STATUS", status, "Ready  •  v0.3");

        var workspace = new Panel {
            Dock = DockStyle.Fill, BackColor = PanelColor,
            Margin = new Padding(4, 0, 4, 0), Padding = new Padding(16)
        };
        main.Controls.Add(workspace, 0, 2);
        var content = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Margin = Padding.Empty, BackColor = PanelColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
        workspace.Controls.Add(content);

        var heading = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(heading, 0, 0);
        var h = Label("♧  Account Manager", 19, true);
        h.Location = new Point(6, 7);
        heading.Controls.Add(h);

        var toolbar = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false,
            AutoScroll = true, Margin = Padding.Empty
        };
        content.Controls.Add(toolbar, 0, 1);
        search.Width = 210;
        search.Height = 40;
        search.Font = new Font("Segoe UI", 12);
        search.Margin = new Padding(4, 7, 8, 4);
        search.PlaceholderText = "Search profiles...";
        search.TextChanged += (_, _) => RefreshProfiles();
        toolbar.Controls.Add(search);
        AddToolbar(toolbar, "Add", 82, AddProfile, true);
        AddToolbar(toolbar, "Edit", 82, EditProfile);
        AddToolbar(toolbar, "Delete", 82, DeleteProfile);
        AddToolbar(toolbar, "Import", 90, ImportProfiles);
        AddToolbar(toolbar, "Export", 90, ExportProfiles);
        AddToolbar(toolbar, "Duplicate", 100, DuplicateProfile);
        AddToolbar(toolbar, "Sign in", 100, SignInOfficial);

        table.Dock = DockStyle.Fill;
        table.Margin = new Padding(0, 4, 0, 8);
        table.BackgroundColor = PanelColor;
        table.BorderStyle = BorderStyle.None;
        table.GridColor = Color.FromArgb(56, 41, 82);
        table.EnableHeadersVisualStyles = false;
        table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(43, 29, 71);
        table.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        table.ColumnHeadersHeight = 44;
        table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        table.DefaultCellStyle.BackColor = PanelColor;
        table.DefaultCellStyle.ForeColor = Color.White;
        table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(94, 38, 157);
        table.DefaultCellStyle.SelectionForeColor = Color.White;
        table.RowTemplate.Height = 42;
        table.AllowUserToAddRows = false;
        table.AllowUserToDeleteRows = false;
        table.ReadOnly = true;
        table.MultiSelect = false;
        table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        table.AutoGenerateColumns = false;
        table.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 22, 51);
        table.RowHeadersVisible = false;
        table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        table.Columns.Add(new DataGridViewTextBoxColumn {
            DataPropertyName = nameof(AccountProfile.Alias), HeaderText = "PROFILE NAME"
        });
        table.Columns.Add(new DataGridViewTextBoxColumn {
            DataPropertyName = nameof(AccountProfile.Username), HeaderText = "ROBLOX USERNAME"
        });
        table.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AccountProfile.Notes), HeaderText = "NOTES" });
        table.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(AccountProfile.HasEncryptedCookie), HeaderText = "SAVED SESSION", FillWeight = 35 });
        table.DataSource = binding;
        table.SelectionChanged += (_, _) => selectedName.Text =
            CurrentProfile() is { } p ? "Selected: " + p.Alias : "Select a profile to manage it";
        content.Controls.Add(table, 0, 2);

        var bottom = new Panel {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 21, 54),
            Margin = new Padding(0, 4, 0, 0)
        };
        content.Controls.Add(bottom, 0, 3);
        selectedName.Text = "Select a profile to manage it";
        selectedName.ForeColor = Muted;
        selectedName.Location = new Point(15, 12);
        selectedName.AutoSize = true;
        bottom.Controls.Add(selectedName);
        var open = Button("Open Roblox", OpenRoblox, true);
        open.SetBounds(14, 42, 210, 43);
        bottom.Controls.Add(open);
        var explanation = Label("Profiles are stored locally. Game launching is coming later.", 9, false, Muted);
        explanation.Location = new Point(240, 57);
        bottom.Controls.Add(explanation);
    }

    private static void AddStat(TableLayoutPanel cards, int index, string name, Label value, string initial)
    {
        var box = new Panel { Dock = DockStyle.Fill, BackColor = PanelColor, Margin = new Padding(4) };
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
            for (int i = 0; i < 7; i++)
                e.Graphics.DrawEllipse(pen, Width - 230 + i * 19, -120 + i * 8, 350, 270);
        }
    }

    private static void OpenRoblox()
    {
        try { Process.Start(new ProcessStartInfo("https://www.roblox.com/home") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show("Could not open Roblox: " + ex.Message, "VEXOR"); }
    }
}
