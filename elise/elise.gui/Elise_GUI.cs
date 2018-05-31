using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Elise;
using Elise.Sequencing;
using Elise.Formatting;
using Elise.Rendering;
using Elise.Sources;

static class UI
{
    public static Panel Panel(DockStyle dock) => new Panel
    {
        Dock = dock,
        BackColor = SystemColors.Control
    };

    public static GroupBox GroupBox(string text) => new GroupBox
    {
        Dock = DockStyle.Fill,
        AutoSize = false,
        Text = text
    };

    public static SplitContainer SplitPanel(Orientation orientation) => new SplitContainer
    {
        Dock = DockStyle.Fill,
        Orientation = orientation,
        BackColor = Color.WhiteSmoke
    };

    public static Label Label(string text) => new Label
    {
        Dock = DockStyle.Left,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = text + ":"
    };

    public static ComboBox List(params string[] items)
    {
        var source = new string[items.Length];
        Array.Copy(items, source, items.Length);

        return new ComboBox
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 140,
            DataSource = source,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
    }

    public static NumericUpDown Spinner(int min, int max) => new NumericUpDown
    {
        Dock = DockStyle.Left,
        Minimum = min,
        Maximum = max,
        Value = min,
        AutoSize = false,
        Width = 60
    };

    public static Control[] DockAll(DockStyle dock, params Control[] controls)
    {
        Array.ForEach(controls, c => c.Dock = dock);
        return controls;
    }

    public static TContainer Container<TContainer>(DockStyle dock, params Control[] controls) where TContainer : Control
    {
        var container = Activator.CreateInstance(typeof(TContainer)) as TContainer;
        container.Dock = dock;
        container.AutoSize = false;
        controls = controls.Reverse().ToArray();
        container.Controls.AddRange(controls);
        return container;
    }

    public static Panel ListPanel(string text, ComboBox list)
    {
        var container = Container<Panel>(DockStyle.Left, Label(text), list);
        container.AutoSize = false;
        container.Height = 50;
        container.Width = 80;
        container.Padding = new Padding(0);
        container.Margin = new Padding(0);
        return container;
    }

    public static Panel ListPanel(string text, params string[] items)
    {
        return ListPanel(text, List(items));
    }

    public static Panel SpinPanel(string text, NumericUpDown spinner)
    {
        var container = Container<Panel>(DockStyle.Left, Label(text), spinner);
        container.AutoSize = false;
        container.Width = 90;
        container.Padding = new Padding(0);
        container.Margin = new Padding(0);
        return container;
    }

    public static Panel SpinPanel(string text, int min, int max)
    {
        return SpinPanel(text, Spinner(min, max));
    }

    public static SplitContainer Splitter(Control left, Control right, Orientation orientation = Orientation.Vertical)
    {
        var container = SplitPanel(orientation);
        container.Panel1.Controls.Add(left);
        container.Panel2.Controls.Add(right);
        return container;
    }

    public static RichTextBox RTFGrid() => new RichTextBox
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        ReadOnly = true,
        WordWrap = true,
        TabStop = false,
        RightMargin = 0,
        ScrollBars = RichTextBoxScrollBars.ForcedBoth,
        BackColor = Color.White,
        ForeColor = Color.LightGray,
        Font = new Font("Consolas", 8),
    };

    public static Tuple<string, EventHandler> Command(string item, EventHandler command)
    {
        return new Tuple<string, EventHandler>(item, command);
    }

    public static ToolStripMenuItem SubMenu(string name, params Tuple<string, EventHandler>[] items)
    {
        var subMenu = new ToolStripMenuItem(name);
        subMenu.DropDownItems.AddRange(items.Select(i => new ToolStripMenuItem(i.Item1, null, i.Item2)).ToArray());
        return subMenu;
    }

    public static MenuStrip Menu(params ToolStripMenuItem[] menus)
    {
        var menu = new MenuStrip();
        menu.Dock = DockStyle.Top;
        menu.Items.AddRange(menus);
        return menu;
    }
}

class EliseUI : Form
{
    Panel HitsPanel;
    RichTextBox Grid;
    MenuStrip MainMenu;
    ComboBox BookList, ToBookList, SourceList;
    StatusBar StatusBar;
    TextBox TermTextBox;
    ListBox TermListBox;
    ListView HitsListView;
    ProgressBar ProgressBar;
    SplitContainer VSplitter, HSplitter;
    BackgroundWorker Searcher;
    Button AddTermButton, RemoveTermButton, SearchButton;
    NumericUpDown StartSpinner, StopSpinner, FromSkipSpinner, ToSkipSpinner;
    NumericUpDown ChapterSpinner, VerseSpinner, ToChapterSpinner, ToVerseSpinner;

    bool IsWorking = false;
    bool HasSearched = false;
    bool IsSaved = false;
    Bible Bible = new Bible();

    Language Language = Language.English;

    public UTF8Encoding Encoding = new UTF8Encoding(false);

    public string Book
    {
        get { return BookList.Text; }
        set { BookList.Text = value; }
    }

    public string ToBook
    {
        get { return ToBookList.Text; }
        set { ToBookList.Text = value; }
    }

    public int Chapter
    {
        get { return (int)ChapterSpinner.Value; }
        set { ChapterSpinner.Value = value; }
    }

    public int ToChapter
    {
        get { return (int)ToChapterSpinner.Value; }
        set { ToChapterSpinner.Value = value; }
    }

    public int Verse
    {
        get { return (int)VerseSpinner.Value; }
        set { VerseSpinner.Value = value; }
    }

    public int ToVerse
    {
        get { return (int)ToVerseSpinner.Value; }
        set { ToVerseSpinner.Value = value; }
    }

    public Reference Reference { get; private set; }

    public int Start
    {
        get { return (int)StartSpinner.Value; }
        set { StartSpinner.Value = (decimal)value; }
    }

    public int Stop
    {
        get { return (int)StopSpinner.Value; }
        set { StopSpinner.Value = (decimal)value; }
    }

    public int FromSkip
    {
        get { return (int)FromSkipSpinner.Value; }
        set { FromSkipSpinner.Value = (decimal)value; }
    }

    public int ToSkip
    {
        get { return (int)ToSkipSpinner.Value; }
        set { ToSkipSpinner.Value = (decimal)value; }
    }

    public string GridText
    {
        get { return Grid != null ? Grid.Text : string.Empty; }
        set { if (Grid != null) Grid.Text = value; }
    }

    public string[] Terms
    {
        get
        {
            var terms = new List<string>();
            foreach (var term in TermListBox.Items) terms.Add(term.ToString());
            return terms.ToArray();
        }
        set
        {
            TermListBox.Items.Clear();
            foreach (var term in value) TermListBox.Items.Add(term);
        }
    }

    void DrawUI()
    {
        // UI Constants
        const int Max = 10000000;
        const int GroupBoxHeight = 50;
        SuspendLayout();

        // Menu
        MainMenu = UI.Menu(
            UI.SubMenu("&File",
                UI.Command("&Open...", Open),
                UI.Command("&Save...", Save),
                UI.Command("E&xit", Exit)),
            UI.SubMenu("&Help",
                UI.Command("&About", About))
        );

        /** Left Panel **/
        /* Search Options */
        var sources = Directory.GetFiles("../../../Elise/Resources/Sources", "*.src").Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
        SourceList = UI.List(sources);
        var SourceListPanel = UI.ListPanel("&Source", SourceList);
        SourceListPanel.Height = 30;

        // Range Options
        const int StartMin = 0;
        const int StartMax = Max;
        const int DefaultStop = 1000;
        const int SkipMin = 2;
        const int SkipMax = Max;
        const int DefaultToSkip = 20;

        StartSpinner = UI.Spinner(StartMin, StartMax);
        StartSpinner.Value = StartMin;
        var StartSpinPanel = UI.SpinPanel("&Start", StartSpinner);

        StopSpinner = UI.Spinner(StartMin, StartMax);
        StopSpinner.Value = DefaultStop;
        var StopSpinPanel = UI.SpinPanel("&Stop", StopSpinner);

        var RangeOptions = UI.Container<GroupBox>(DockStyle.Top, StartSpinPanel, StopSpinPanel);
        RangeOptions.Text = "Range &Options";
        RangeOptions.Height = GroupBoxHeight;

        // Skip Options
        FromSkipSpinner = UI.Spinner(SkipMin, SkipMax);
        FromSkipSpinner.Value = SkipMin;
        var FromSkipPanel = UI.SpinPanel("&From", FromSkipSpinner);

        ToSkipSpinner = UI.Spinner(SkipMin, SkipMax);
        ToSkipSpinner.Value = DefaultToSkip;
        var ToSkipPanel = UI.SpinPanel("&To", ToSkipSpinner);

        var SkipOptions = UI.Container<GroupBox>(DockStyle.Top, FromSkipPanel, ToSkipPanel);
        SkipOptions.Text = "Skip &Interval";
        SkipOptions.Height = GroupBoxHeight;

        // Term Options
        var TermLabel = UI.Label("&Term");
        TermTextBox = new TextBox { Dock = DockStyle.Top, AutoSize = false, Width = 80 };
        AddTermButton = new Button { Dock = DockStyle.Left, Text = "Add" };
        RemoveTermButton = new Button { Dock = DockStyle.Left, Text = "Remove" };
        var TermButtons = UI.Container<Panel>(DockStyle.Top, AddTermButton, RemoveTermButton);
        TermButtons.Height = 20;

        var TermPanel = UI.Container<Panel>(DockStyle.Top, TermLabel, TermTextBox, TermButtons);
        TermPanel.AutoSize = true;

        SearchButton = new Button { Dock = DockStyle.Top, Text = "&Search" };
        TermListBox = new ListBox { Dock = DockStyle.Top };

        var TermOptions = UI.Container<GroupBox>(DockStyle.Top, TermPanel, TermListBox, SearchButton);
        TermOptions.Text = "Search &Terms";
        TermOptions.AutoSize = true;

        var SearchOptions = UI.Container<GroupBox>(DockStyle.Fill,
            UI.DockAll(DockStyle.Top, SourceListPanel, RangeOptions, SkipOptions, TermOptions)
        );

        SearchOptions.Text = "&Search Options";

        var LeftPanel = UI.Container<Panel>(DockStyle.Fill, SearchOptions);

        /** Rich Text Grid **/
        Grid = UI.RTFGrid();

        /** Bottom Panel **/
        ProgressBar = new ProgressBar
        {
            Dock = DockStyle.Right,
            Style = ProgressBarStyle.Continuous,
        };

        StatusBar = UI.Container<StatusBar>(DockStyle.Bottom, ProgressBar);

        // Passage selection
        BookList = UI.List(Bible.Books);
        var BookListPanel = UI.ListPanel("&Book", BookList);
        ChapterSpinner = UI.Spinner(1, 1);
        var ChapterSpinPanel = UI.SpinPanel("&Chapter", ChapterSpinner);
        VerseSpinner = UI.Spinner(1, 1);
        var VerseSpinPanel = UI.SpinPanel("&Verse", VerseSpinner);

        ToBookList = UI.List(Bible.Books);
        var ToBookListPanel = UI.ListPanel("- &Book", ToBookList);
        ToChapterSpinner = UI.Spinner(1, 1);
        var ToChapterSpinPanel = UI.SpinPanel("&Chapter", ToChapterSpinner);
        ToVerseSpinner = UI.Spinner(1, 1);
        var ToVerseSpinPanel = UI.SpinPanel("&Verse", ToVerseSpinner);

        var PassagePanel = UI.Container<GroupBox>(DockStyle.Top,
            BookListPanel, ChapterSpinPanel, VerseSpinPanel,
            ToBookListPanel, ToChapterSpinPanel, ToVerseSpinPanel
        );

        PassagePanel.Height = 48;
        var GridPanel = UI.Container<Panel>(DockStyle.Fill, PassagePanel, Grid);

        /*** VSplitter ***/
        VSplitter = UI.Splitter(LeftPanel, GridPanel);

        /** Hits Panel **/
        var HitsLabel = UI.Label("Search &Hits");
        HitsLabel.Dock = DockStyle.Top;
        HitsLabel.AutoSize = false;
        HitsLabel.Font = new Font("MS Sans Serif", 8);
        HitsLabel.BackColor = SystemColors.ControlLight;

        /* Hits List */
        HitsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            GridLines = true,
            MultiSelect = false,
            FullRowSelect = true,
            ShowGroups = true,
            HideSelection = false
        };

        HitsListView.Columns.AddRange(new ColumnHeader[]
        {
            new ColumnHeader { Text = "Search Term" },
            new ColumnHeader { Text = "Search Position" },
            new ColumnHeader { Text = "Skip Interval" }
        });

        HitsPanel = UI.Container<Panel>(DockStyle.Fill, HitsLabel, HitsListView);
        HSplitter = UI.Splitter(VSplitter, HitsPanel, Orientation.Horizontal);
        Controls.Add(UI.Container<Panel>(DockStyle.Fill, MainMenu, StatusBar, HSplitter));
        ResumeLayout();

        const int SplitterDistance = 200;
        VSplitter.FixedPanel = FixedPanel.Panel1;
        VSplitter.Panel1MinSize = SplitterDistance;
        VSplitter.SplitterDistance = SplitterDistance;
        VSplitter.Update();

        HSplitter.FixedPanel = FixedPanel.Panel2;
        HSplitter.Panel2MinSize = 100;
        HSplitter.SplitterDistance = 400;
        HSplitter.Panel2Collapsed = true;
        HSplitter.Update();

        HitsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    void CanSearch(object sender, EventArgs e)
    {
        SearchButton.Enabled = TermListBox.Items.Count > 0;
    }

    public EliseUI()
    {
        Text = "ELISE :: Equidistant Letter Interval Sequencing Engine";
        Size = new Size(800, 540);
        ShowIcon = false;
        DrawUI();

        // Events
        AddTermButton.Click += AddTerm;
        TermTextBox.KeyUp += (s, e) => { if (e.KeyCode == Keys.Enter) AddTerm(s, e); };
        RemoveTermButton.Click += RemoveTerm;
        SearchButton.Click += Search;
        HitsListView.Click += TermSelected;
        Closing += ConfirmExit;

        // Passage selection
        BookList.SelectedIndexChanged += (s, e) => UpdateChapters(BookList, ChapterSpinner);
        BookList.SelectedIndexChanged += (s, e) =>
        {
            var bookIndex = BookList.SelectedIndex;
            var source = new string[Bible.Books.Length];
            Array.Copy(Bible.Books, bookIndex, source, 0, Bible.Books.Length - bookIndex);
            ToBookList.DataSource = source;
            ToBookList.SelectedIndex = 0;
        };

        ResumeUpdates();

        // UI State
        AddTermButton.Enabled = false;
        RemoveTermButton.Enabled = false;
        SearchButton.Enabled = false;

        TermTextBox.TextChanged += (s, e) => AddTermButton.Enabled = TermTextBox.TextLength > 0;
        TermListBox.SelectedIndexChanged += (s, e) => RemoveTermButton.Enabled = TermListBox.SelectedIndex != -1;

        AddTermButton.Click += CanSearch;
        RemoveTermButton.Click += CanSearch;
        TermTextBox.TextChanged += CanSearch;

        Load += (s, e) =>
        {
            CanSearch(null, null);
            UpdateChapters(BookList, ChapterSpinner);
            UpdateVerses(BookList, ChapterSpinner, VerseSpinner);
            UpdateChapters(ToBookList, ToChapterSpinner, true);
            UpdateVerses(ToBookList, ToChapterSpinner, ToVerseSpinner, true);
            LoadGrid(null, null);
        };

        // Background workers
        Searcher = Worker<string[], int>(SearchForHits, DisplayHits, true);
    }

    void ResumeUpdates()
    {
        ChapterSpinner.ValueChanged += (s, e) => UpdateVerses(BookList, ChapterSpinner, VerseSpinner);
        ToBookList.SelectedIndexChanged += (s, e) => UpdateChapters(ToBookList, ToChapterSpinner, true);
        ToChapterSpinner.ValueChanged += (s, e) => UpdateVerses(ToBookList, ToChapterSpinner, ToVerseSpinner, true);

        BookList.SelectedIndexChanged += LoadGrid;
        ChapterSpinner.ValueChanged += LoadGrid;
        VerseSpinner.Click += LoadGrid;
        ToBookList.SelectedIndexChanged += LoadGrid;
        ToChapterSpinner.ValueChanged += LoadGrid;
        ToVerseSpinner.Click += LoadGrid;
    }

    void SuspendUpdates()
    {
        ChapterSpinner.ValueChanged -= (s, e) => UpdateVerses(BookList, ChapterSpinner, VerseSpinner);
        ToBookList.SelectedIndexChanged -= (s, e) => UpdateChapters(ToBookList, ToChapterSpinner, true);
        ToChapterSpinner.ValueChanged -= (s, e) => UpdateVerses(ToBookList, ToChapterSpinner, ToVerseSpinner, true);

        BookList.SelectedIndexChanged -= LoadGrid;
        ChapterSpinner.ValueChanged -= LoadGrid;
        VerseSpinner.Click -= LoadGrid;
        ToBookList.SelectedIndexChanged -= LoadGrid;
        ToChapterSpinner.ValueChanged -= LoadGrid;
        ToVerseSpinner.Click -= LoadGrid;
    }

    void UpdateChapters(ComboBox list, NumericUpDown spinner, bool max = false)
    {
        var book = list.Text;
        int chapters = Bible.Chapters(book);
        spinner.Maximum = chapters;
        spinner.Value = max ? chapters : 1;
    }

    void UpdateVerses(ComboBox list, NumericUpDown chapterSpinner, NumericUpDown verseSpinner, bool max = false)
    {
        var book = list.Text;
        int chapter = (int)chapterSpinner.Value;
        int verses = Bible.Verses(book, chapter);
        verseSpinner.Maximum = verses;
        verseSpinner.Value = max ? verses : 1;
    }

    BackgroundWorker Worker<T, U>(Func<T, U> work, Action<U> completed, bool cancellable = false)
    {
        var worker = new BackgroundWorker();
        worker.WorkerSupportsCancellation = cancellable;
        worker.DoWork += (_, e) => { e.Result = (U)work((T)e.Argument); };
        worker.RunWorkerCompleted += (_, e) => { completed((U)e.Result); };
        return worker;
    }

    void BeginWork(string status, BackgroundWorker worker, object args, bool waitCursor = false)
    {
        StatusBar.Text = status;
        ProgressBar.Show();
        if (waitCursor) ProgressBar.Style = ProgressBarStyle.Marquee;
        if (waitCursor) UseWaitCursor = true;
        worker.RunWorkerAsync(args);
        IsWorking = true;
    }

    object FinishWork(string status, object result, bool waitCursor = false)
    {
        UseWaitCursor = false;
        StatusBar.Text = status;
        ProgressBar.Style = ProgressBarStyle.Continuous;
        ProgressBar.Hide();
        IsWorking = false;
        return result;
    }

    Language DetectLanguage(string text)
    {
        return (from entry in Formatter.Formatters
                let language = entry.Key
                let formatter = entry.Value
                where Regex.IsMatch(text, $"[{formatter.Pattern}]")
                select language).FirstOrDefault();
    }

    string FormatByLanguage(string text, Language language)
    {
        return Formatter.Formatters[language].Format(text);
    }

    string LoadPassage(string unused)
    {
        StatusBar.Text = "Loading passage...";
        Reference = new Reference(Book, Chapter, Verse, ToBook, ToChapter, ToVerse);
        var text = Bible.Passage(Book, Chapter, Verse, ToBook, ToChapter, ToVerse);
        Language = DetectLanguage(text);
        text = FormatByLanguage(text, Language);
        return string.Join(" ", text.ToCharArray());
    }

    void DisplayGrid(string grid)
    {
        GridText = grid;
        StopSpinner.Maximum = (decimal)GridText.Length / 2;
        ToSkipSpinner.Maximum = StopSpinner.Maximum;
        StopSpinner.Value = StopSpinner.Maximum;
        FinishWork("Grid loaded.", grid, true);
    }

    void LoadGrid(object sender, EventArgs e)
    {
        var passage = LoadPassage(null);
        DisplayGrid(passage);
    }

    void AddTerm(object sender, EventArgs e)
    {
        var term = TermTextBox.Text;
        if (term.Trim().Length == 0) return;

        if (DetectLanguage(term) != Language)
        {
            string translation;

            if (!TryTranslateTerm(term, out translation))
            {
                TermTextBox.Clear();
                return;
            }

            term = translation;
        }

        term = FormatByLanguage(term, Language);

        if (!TermListBox.Items.Contains(term))
        {
            TermListBox.Items.Add(term);
        }

        TermTextBox.Clear();
    }

    bool TryTranslateTerm(string term, out string translation)
    {
        translation = string.Empty;
        var toLang = Language;
        var fromLang = DetectLanguage(term);
        Func<Language, string> langCode = (lang) => lang.ToString().ToLower().Substring(0, 2);

        const string service = @"https://translate.yandex.net/api/v1.5/tr.json/translate?";
        var url = WebUtility.UrlEncode($@"{service}?key=API-KEY&text={term}&lang={langCode(fromLang)}-{langCode(toLang)}");

        using (var client = new WebClient())
        {
            try
            {
                var response = client.DownloadString(url);
                translation = response;
            }
            catch (WebException)
            {
                MessageBox.Show(
                    $"Translation of {fromLang} term '{term}' into {toLang} failed. Url was: {url}",
                    "Translation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }
        }

        return true;
    }

    void RemoveTerm(object sender, EventArgs e)
    {
        if (TermListBox.SelectedIndex == -1) return;
        var term = TermListBox.Items[TermListBox.SelectedIndex];

        if (TermListBox.Items.Contains(term))
        {
            TermListBox.Items.Remove(term);
        }

        TermListBox.SelectedIndex = TermListBox.Items.Count - 1;
    }

    void RefreshHits(IEnumerable<Hit> hits, params string[] terms)
    {
        //HitsListView.Groups.Clear();
        HitsListView.Items.Clear();
        var termItems = new List<Hit>();

        foreach (var term in terms)
        {
            var rterm = new String(term.Reverse().ToArray());
            //var termGroup = new ListViewGroup(term, term);
            //termGroup.HeaderAlignment = HorizontalAlignment.Center;
            var termHits = hits.Where(h => h.Term == term || h.Term == rterm).ToList();
            termItems.AddRange(termHits);
            // termItems = termHits.Select(h => new ListViewItem(new string[]
            // { h.Term.ToString(), h.Index.ToString(), h.Skip.ToString() })).ToArray();
            // //termGroup.Items.AddRange(termItems); 
            //HitsListView.Groups.Add(termGroup);
            // HitsListView.Items.AddRange(termItems);
        }

        termItems = termItems.OrderBy(i => i.Index).ThenBy(i => i.Skip).ToList();
        HitsListView.Items.AddRange(termItems.Select(h => new ListViewItem(new string[]
            { h.Term.ToString(), h.Index.ToString(), h.Skip.ToString() })).ToArray());
    }

    string RTF { get; set; }

    int SearchForHits(params string[] terms)
    {
        Formatter formatter = Formatter.Formatters[Language];
        StatusBar.Text = "Loading sequence...";
        var passage = Bible.Passage(Book, Chapter, Verse, ToBook, ToChapter, ToVerse);
        var sequencer = new Sequencer(formatter, passage, Start, Stop, FromSkip, ToSkip, -1);

        sequencer.ProgressUpdated += (object sender, ProgressUpdatedEventArgs e) =>
        {
            e.Cancel = Searcher.CancellationPending;
            int progress = e?.Progress ?? 0;
            ProgressBar.Value = progress;
            // StatusBar.Text = $"{progress}/{Stop}";
        };

        ProgressBar.Maximum = Stop;
        StatusBar.Text = "Sequencing...";
        var timer = Stopwatch.StartNew();
        var hits = sequencer.Search(terms);
        timer.Stop();
        Text = new TimeSpan(timer.ElapsedMilliseconds).ToString();
        StatusBar.Text = "Rendering...";
        Renderer renderer = Renderer.GetRendererByLanguage(Language, sequencer.Grid, hits.ToArray());
        RTF = renderer.Render();
        Grid.Rtf = RTF;
        StatusBar.Text = "Loading hits...";
        RefreshHits(hits, terms);
        return hits.Count();
    }

    void DisplayHits(int hits)
    {
        HasSearched = hits > 0;
        ShowHitsList(HasSearched);
        FinishWork($"Search completed, {hits} hit(s) found", hits);
        SearchButton.Text = "&Search";
    }

    void Search(object sender, EventArgs e)
    {
        if (!IsWorking)
        {
            BeginWork($"Searching {Reference} starting from position {Start} to {Stop} "
                    + $"skipping every {FromSkip} to {ToSkip} letter(s) looking for {string.Join(",", Terms)}...",
                    Searcher, Terms.ToArray(), false);
            SearchButton.Text = "&Cancel Search";
        }
        else
        {
            if (MessageBox.Show("Are you sure you want to cancel the current search in progress?", "Confirm Search Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StatusBar.Text = "Cancelling search...";
                SearchButton.Enabled = false;
                Searcher.CancelAsync();
                FinishWork("Search cancelled", Searcher);
                MessageBox.Show("Search cancelled", "Search Cancelled");
                SearchButton.Text = "&Search";
                SearchButton.Enabled = true;
            }
        }
    }

    void TermSelected(object sender, EventArgs e)
    {
        if (HitsListView.SelectedItems.Count == 0) return;
        var selectedItem = HitsListView.SelectedItems[0];
        var selectedTerm = selectedItem.SubItems[0].Text;
        var selectedIndex = int.Parse(selectedItem.SubItems[1].Text);
        var selectedSkip = int.Parse(selectedItem.SubItems[2].Text);

        Grid.SelectionStart = 0;
        Grid.SelectionLength = Grid.Text.Length;
        Grid.SelectionBackColor = Color.Transparent;

        int originalIndex = (selectedIndex * 2) - selectedSkip * 2;
        int offsetIndex = originalIndex;

        for (var i = 1; i <= selectedTerm.Length; i++)
        {
            offsetIndex += selectedSkip * 2;
            Grid.SelectionStart = offsetIndex;
            Grid.SelectionLength = 1;
            Grid.SelectionBackColor = Color.Yellow;
        }

        Grid.SelectionStart = offsetIndex;
        Grid.SelectionLength = 0;
        Grid.Focus();

        var selectedText = string.Empty;
        try
        {
            selectedText = Grid.Text.Substring(selectedIndex * 2, selectedTerm.Length * selectedSkip);
            selectedText = selectedText.Replace(" ", string.Empty).Trim();
        }
        catch { }

        StatusBar.Text = $"Hit: Term '{selectedTerm}' found at {selectedIndex} skipping {selectedSkip} letter(s) in '{selectedText}'";
    }

    void ShowHitsList(bool state)
    {
        HSplitter.Panel2Collapsed = !state;
    }

    void LoadSearch(string path)
    {
        try
        {
            var state = File.ReadAllText(path, new UTF8Encoding(false));
            var fields = state.Split('|');

            var source = fields[0];
            Reference = Bible.ParseReference(source);

            SuspendUpdates();
            Book = Reference.Book;
            Chapter = Reference.Chapter;
            Verse = Reference.Verse;
            ToBook = Reference.ToBook;
            ToChapter = Reference.ToChapter;
            ToVerse = Reference.ToVerse;
            ResumeUpdates();

            Grid.SelectAll();
            Grid.SelectionColor = Color.Gainsboro;
            Grid.DeselectAll();
            LoadGrid(null, null);

            Start = int.Parse(fields[1].Trim());
            Stop = int.Parse(fields[2].Trim());
            FromSkip = int.Parse(fields[3].Trim());
            ToSkip = int.Parse(fields[4].Trim());
            var terms = fields[5].Split(',');
            TermListBox.Items.Clear();
            TermListBox.Items.AddRange(terms);
            CanSearch(null, null);
            Grid.LoadFile($@"{path}.rtf");

            var hits = fields[6].Split(';');
            HitsListView.Items.Clear();

            foreach (var hit in hits)
            {
                var parts = hit.Split(',');
                var term = parts[0];
                var index = int.Parse(parts[1].Trim());
                var skip = int.Parse(parts[2].Trim());
                var item = new ListViewItem(new string[] { term, index.ToString(), skip.ToString() });
                HitsListView.Items.Add(item);
            }

            ShowHitsList(hits.Length > 0);
            StatusBar.Text = $"Saved search '{path}.els' loaded successfully.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Loading Saved Search Failed");
            return;
        }
    }

    void Open(object sender, EventArgs e)
    {
        var dialog = new OpenFileDialog();
        dialog.Filter = "Elise Searches|*.els";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            LoadSearch(dialog.FileName);
        }
    }

    string SerializeHits()
    {
        var hitems = new List<string>();

        foreach (var hit in HitsListView.Items)
        {
            var hitem = hit as ListViewItem;
            var sitems = new List<string>();

            foreach (var item in hitem.SubItems)
            {
                var sitem = item as ListViewItem.ListViewSubItem;
                sitems.Add(sitem.Text);
            }

            hitems.Add(string.Join(",", sitems));
        }

        return string.Join(";", hitems);
    }

    string SerializeState(string path)
    {
        var source = $"{Book} {Chapter}:{Verse}-{ToBook} {ToChapter}:{ToVerse}";
        var terms = string.Join(",", Terms);
        var hits = SerializeHits();
        var state = $"{source}|{Start}|{Stop}|{FromSkip}|{ToSkip}|{terms}|{hits}";
        Grid.SaveFile($@"{path}.rtf", RichTextBoxStreamType.RichText);
        return state;
    }

    void Save(object sender, EventArgs e)
    {
        var dialog = new SaveFileDialog();
        dialog.Filter = "(*.els) Elise Search|*.els";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var path = dialog.FileName;
            var state = SerializeState(path);

            try
            {
                File.WriteAllText(path, state, new UTF8Encoding(false));
                StatusBar.Text = $"Search '{path}' saved successfully";
                IsSaved = true;
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.ToString(), "Error Saving Search");
            }
        }
    }

    bool Confirm(string question, string title, DialogResult expected = DialogResult.Yes)
    {
        return expected == MessageBox.Show(question, $"Elise - {title}",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }

    void About(object sender, EventArgs e)
    {
        MessageBox.Show(@"
Elise :: Equidistant Letter Interval Sequencing Engine 
Version 1.0.0

Copyright (C) Michael Schutt 2016
All Rights Reserved.", "About Elise",
    MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void Exit(object sender, EventArgs e)
    {
        ConfirmExit(sender, new CancelEventArgs());
    }

    void ConfirmExit(object sender, CancelEventArgs e)
    {
        var message = "Are you sure you want to exit?";
        message = IsWorking ? $"Searching is currently in progress. {message}"
                : HasSearched && !IsSaved ? $"Search results are not saved. {message}"
                : message;

        if ((IsWorking || (HasSearched && !IsSaved)) && !Confirm(message, "Confirm Exit"))
        {
            e.Cancel = true;
            return;
        }

        Application.Exit();
    }
}

//class Program
//{
//    [STAThread]
//    public static void _Main()
//    {
//        Application.EnableVisualStyles();
//        Application.Run(new EliseUI());
//    }
//}
