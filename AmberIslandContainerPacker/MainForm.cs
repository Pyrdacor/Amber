using System.Diagnostics;
using AmberIsland.GameData;

namespace AmberIslandContainerPacker;

internal sealed class MainForm : Form
{
    private const int MinIndex = 1;
    private const int MaxIndex = 65535;
    private const int MaxFileSize = 65535; // FileContainer stores sizes as 16-bit words.

    private sealed class Entry(uint index, byte[] data)
    {
        public uint Index = index;
        public readonly byte[] Data = data;
    }

    private readonly List<Entry> entries = [];
    private readonly ListView list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        LabelEdit = true,
        AllowDrop = true,
        HideSelection = false,
        MultiSelect = true
    };
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    private string? currentPath;
    private bool dirty;

    public MainForm()
    {
        Width = 520;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        list.Columns.Add("Index", 120, HorizontalAlignment.Left);
        list.Columns.Add("Size (bytes)", 340, HorizontalAlignment.Left);

        statusStrip.Items.Add(statusLabel);

        var menu = BuildMenu();

        // Fill first, then docked controls.
        Controls.Add(list);
        Controls.Add(statusStrip);
        Controls.Add(menu);
        MainMenuStrip = menu;

        WireEvents();
        NewContainer();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&New", null, (_, _) => OnNew()) { ShortcutKeys = Keys.Control | Keys.N });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open…", null, (_, _) => OnOpen()) { ShortcutKeys = Keys.Control | Keys.O });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save", null, (_, _) => OnSave()) { ShortcutKeys = Keys.Control | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save &As…", null, (_, _) => OnSaveAs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 });

        var editMenu = new ToolStripMenuItem("&Edit");
        editMenu.DropDownItems.Add(new ToolStripMenuItem("&Add files…", null, (_, _) => OnAddFiles()) { ShortcutKeys = Keys.Insert });
        editMenu.DropDownItems.Add(new ToolStripMenuItem("&Remove selected", null, (_, _) => RemoveSelected()) { ShortcutKeys = Keys.Delete });
        editMenu.DropDownItems.Add(new ToolStripMenuItem("Edit &index", null, (_, _) => BeginEditSelectedIndex()) { ShortcutKeys = Keys.F2 });

        menu.Items.Add(fileMenu);
        menu.Items.Add(editMenu);
        return menu;
    }

    private void WireEvents()
    {
        list.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        list.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddFiles(paths);
        };

        list.DoubleClick += (_, _) => OpenSelectedExternally();

        list.BeforeLabelEdit += (_, e) =>
        {
            // The edited label is the current index text; nothing to prevent here.
        };
        list.AfterLabelEdit += OnAfterLabelEdit;

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChanges())
                e.Cancel = true;
        };
    }

    // ---- File menu actions ----

    private void OnNew()
    {
        if (!ConfirmDiscardChanges())
            return;
        NewContainer();
    }

    private void NewContainer()
    {
        entries.Clear();
        currentPath = null;
        dirty = false;
        RefreshList();
        UpdateTitle();
    }

    private void OnOpen()
    {
        if (!ConfirmDiscardChanges())
            return;

        using var dlg = new OpenFileDialog
        {
            Filter = "Amber Island container (*.aic)|*.aic|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var stream = File.OpenRead(dlg.FileName);
            var files = FileContainer.ReadAllFiles(stream);

            entries.Clear();
            foreach (var file in files)
                entries.Add(new Entry(file.Key, file.Value));

            currentPath = dlg.FileName;
            dirty = false;
            RefreshList();
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open container:\n{ex.Message}", "Open",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool OnSave()
    {
        if (currentPath is null)
            return OnSaveAs();
        return SaveTo(currentPath);
    }

    private bool OnSaveAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Amber Island container (*.aic)|*.aic|All files (*.*)|*.*",
            FileName = currentPath is null ? "container.aic" : Path.GetFileName(currentPath)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;

        if (!SaveTo(dlg.FileName))
            return false;

        currentPath = dlg.FileName;
        UpdateTitle();
        return true;
    }

    private bool SaveTo(string path)
    {
        try
        {
            var dict = entries.ToDictionary(e => e.Index, e => e.Data);
            using (var stream = File.Create(path))
                FileContainer.Write(stream, dict);

            dirty = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save container:\n{ex.Message}", "Save",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // ---- Adding / removing files ----

    private void OnAddFiles()
    {
        using var dlg = new OpenFileDialog { Multiselect = true, Filter = "All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        AddFiles(dlg.FileNames);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var skipped = new List<string>();
        Entry? lastAdded = null;

        foreach (var path in paths)
        {
            if (!File.Exists(path))
                continue; // ignore dropped directories

            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                skipped.Add($"{Path.GetFileName(path)} ({ex.Message})");
                continue;
            }

            if (data.Length > MaxFileSize)
            {
                skipped.Add($"{Path.GetFileName(path)} (too large: {data.Length} bytes, max {MaxFileSize})");
                continue;
            }

            uint index = NextFreeIndex();
            if (index == 0)
            {
                skipped.Add($"{Path.GetFileName(path)} (no free index ≤ {MaxIndex})");
                continue;
            }

            lastAdded = new Entry(index, data);
            entries.Add(lastAdded);
            dirty = true;
        }

        RefreshList();
        UpdateTitle();

        if (lastAdded is not null)
            SelectEntry(lastAdded);

        if (skipped.Count > 0)
        {
            MessageBox.Show(this,
                "These files were not added:\n\n" + string.Join("\n", skipped),
                "Add files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Lowest unused index ≥ 1, or 0 if none is available up to <see cref="MaxIndex"/>.</summary>
    private uint NextFreeIndex()
    {
        var used = entries.Select(e => e.Index).ToHashSet();
        for (uint i = MinIndex; i <= MaxIndex; i++)
        {
            if (!used.Contains(i))
                return i;
        }
        return 0;
    }

    private void RemoveSelected()
    {
        if (list.SelectedItems.Count == 0)
            return;

        var toRemove = list.SelectedItems.Cast<ListViewItem>()
            .Select(i => (Entry)i.Tag!)
            .ToList();

        foreach (var entry in toRemove)
            entries.Remove(entry);

        dirty = true;
        RefreshList();
        UpdateTitle();
    }

    // ---- Index editing ----

    private void BeginEditSelectedIndex()
    {
        if (list.SelectedItems.Count == 1)
        {
            list.Focus();
            list.SelectedItems[0].BeginEdit();
        }
    }

    private void OnAfterLabelEdit(object? sender, LabelEditEventArgs e)
    {
        // A null label means the edit was cancelled (Esc) or left unchanged.
        if (e.Label is null)
            return;

        var entry = (Entry)list.Items[e.Item].Tag!;

        if (!uint.TryParse(e.Label.Trim(), out uint newIndex) || newIndex < MinIndex || newIndex > MaxIndex)
        {
            e.CancelEdit = true;
            MessageBox.Show(this, $"The index must be a whole number between {MinIndex} and {MaxIndex}.",
                "Edit index", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (newIndex == entry.Index)
            return; // unchanged

        if (entries.Any(other => other != entry && other.Index == newIndex))
        {
            e.CancelEdit = true;
            MessageBox.Show(this, $"Index {newIndex} is already taken.",
                "Edit index", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        entry.Index = newIndex;
        dirty = true;

        // Cancel the in-place text change; RefreshList re-sorts and re-renders authoritatively.
        e.CancelEdit = true;
        BeginInvoke(() =>
        {
            RefreshList();
            UpdateTitle();
            SelectEntry(entry);
        });
    }

    // ---- Double-click: open file data externally ----

    private void OpenSelectedExternally()
    {
        if (list.SelectedItems.Count != 1)
            return;

        var entry = (Entry)list.SelectedItems[0].Tag!;

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"aic_file_{entry.Index}.bin");
            File.WriteAllBytes(tempPath, entry.Data);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open the file data:\n{ex.Message}", "Open data",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---- List rendering ----

    private void RefreshList()
    {
        entries.Sort((a, b) => a.Index.CompareTo(b.Index));

        list.BeginUpdate();
        list.Items.Clear();
        foreach (var entry in entries)
        {
            var item = new ListViewItem(entry.Index.ToString()) { Tag = entry };
            item.SubItems.Add(entry.Data.Length.ToString("N0"));
            list.Items.Add(item);
        }
        list.EndUpdate();

        statusLabel.Text = $"{entries.Count} file(s), {entries.Sum(e => (long)e.Data.Length):N0} bytes total";
    }

    private void SelectEntry(Entry entry)
    {
        foreach (ListViewItem item in list.Items)
        {
            if (ReferenceEquals(item.Tag, entry))
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
            else
            {
                item.Selected = false;
            }
        }
    }

    // ---- Save-state tracking ----

    private void UpdateTitle()
    {
        string name = currentPath is null ? "untitled" : Path.GetFileName(currentPath);
        Text = $"Amber Island Container Packer — {name}{(dirty ? " *" : "")}";
    }

    /// <summary>Returns true if it is safe to proceed (discard / saved), false to abort.</summary>
    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
            return true;

        var result = MessageBox.Show(this,
            "The container has unsaved changes. Save them now?",
            "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => OnSave(),
            DialogResult.No => true,
            _ => false
        };
    }
}
