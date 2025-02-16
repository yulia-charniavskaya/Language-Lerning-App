using System;
using Gtk;
using System.Data.SQLite;
using Cairo;
using System.Collections.Generic;
using Pango;

using CairoCtx = Cairo.Context;
using GtkCairoHelper = Gtk.CairoHelper;
public class LanguageLearningApp
{
    public static void Main()
    {
        Application.Init();
        new MainWindow();
        Application.Run();
    }
}

public class MainWindow : Window
{
    private SQLiteConnection connection;

    public MainWindow() : base("Изучение иностранных слов")
    {
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        InitializeDatabase();

        VBox vbox = new VBox();

        Label titleLabel = new Label("Изучение иностранных слов")
        {
            Xalign = 0.5f,
            Yalign = 0.5f,
            Sensitive = false
        };
        titleLabel.ModifyFont(Pango.FontDescription.FromString("Bold 16"));
        titleLabel.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0));

        Button cardButton = CreateStyledButton("Карточки");
        cardButton.Clicked += (sender, e) => OpenCardWindow();

        Button testButton = CreateStyledButton("Тест");
        testButton.Clicked += (sender, e) => OpenTestWindow();

        Button addWordButton = CreateStyledButton("Добавить слово");
        addWordButton.Clicked += (sender, e) => OpenAddWordWindow();

        Button statsButton = CreateStyledButton("Статистика");
        statsButton.Clicked += (sender, e) => OpenStatsWindow();

        Button listButton = CreateStyledButton("Список слов");
        listButton.Clicked += (sender, e) => OpenWordListWindow();

        vbox.PackStart(titleLabel, false, false, 20);
        vbox.PackStart(cardButton, true, true, 5);
        vbox.PackStart(testButton, true, true, 5);
        vbox.PackStart(addWordButton, true, true, 5);
        vbox.PackStart(statsButton, true, true, 5);
        vbox.PackStart(listButton, true, true, 5);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Application.Quit();
    }

    private void InitializeDatabase()
    {
        string dbPath = "words.db";
        connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        connection.Open();

        using (var command = new SQLiteCommand(connection))
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS words (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    word TEXT NOT NULL UNIQUE,
                    translation TEXT NOT NULL,
                    transcription TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_results (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                test_date TEXT NOT NULL,
                correct_answers INTEGER NOT NULL,
                total_questions INTEGER NOT NULL
            )";
            command.ExecuteNonQuery();
        }
    }

    private Button CreateStyledButton(string label)
    {
        Button button = new Button(label);
        button.ModifyBg(StateType.Normal, new Gdk.Color(100, 149, 237));
        button.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0));
        button.BorderWidth = 10;
        button.SetSizeRequest(200, 40);
        return button;
    }

    private void OpenCardWindow() { new CardWindow(connection).ShowAll(); }
    private void OpenTestWindow() { new TestWindow(connection).ShowAll(); }
    private void OpenAddWordWindow() { new AddWordWindow(connection).ShowAll(); }
    private void OpenStatsWindow() { new StatsWindow(connection).ShowAll(); }
    private void OpenWordListWindow() { new WordListWindow(connection).ShowAll(); }
}
public class CardWindow : Window
{
    public CardWindow(SQLiteConnection connection) : base("Карточки")
    {
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox vbox = new VBox();

        Label label = new Label("Карточки (в разработке)");
        vbox.PackStart(label, true, true, 10);
        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        vbox.PackStart(backButton, false, false, 0);

        Add(vbox);
        ShowAll();
    }
}

public class TestWindow : Window
{
    private SQLiteConnection connection;
    private List<Tuple<string, string>> words = new List<Tuple<string, string>>();
    private int currentQuestion = 0;
    private int correctAnswers = 0;
    private int totalQuestions = 0;
    private bool isForeignToNative = true;

    private Label questionLabel;
    private Entry answerEntry;
    private Label statusLabel;
    private Button checkButton;
    private RadioButton rbForeignToNative;
    private RadioButton rbNativeToForeign;

    public TestWindow(SQLiteConnection connection) : base("Тест")
    {
        this.connection = connection;
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox mainBox = new VBox(false, 10);

        HBox modeBox = new HBox(true, 10);
        rbForeignToNative = new RadioButton("Иностранный → Родной");
        rbNativeToForeign = new RadioButton(rbForeignToNative, "Родной → Иностранный");
        modeBox.PackStart(rbForeignToNative, true, true, 0);
        modeBox.PackStart(rbNativeToForeign, true, true, 0);

        Button startButton = new Button("Начать тест");
        startButton.Clicked += OnStartTest;

        questionLabel = new Label("Выберите направление и начните тест");
        answerEntry = new Entry { IsEditable = false };
        checkButton = new Button("Проверить") { Sensitive = false };
        checkButton.Clicked += OnCheckAnswer;

        statusLabel = new Label("Правильных ответов: 0/0");

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();

        mainBox.PackStart(modeBox, false, false, 5);
        mainBox.PackStart(startButton, false, false, 5);
        mainBox.PackStart(questionLabel, true, true, 10);
        mainBox.PackStart(answerEntry, false, false, 5);
        mainBox.PackStart(checkButton, false, false, 5);
        mainBox.PackStart(statusLabel, false, false, 5);
        mainBox.PackStart(backButton, false, false, 5);

        Add(mainBox);
        ShowAll();
    }

    private void OnStartTest(object sender, EventArgs e)
    {
        words.Clear();
        using (var cmd = new SQLiteCommand("SELECT word, translation FROM words", connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                words.Add(Tuple.Create(
                    reader["word"].ToString(),
                    reader["translation"].ToString()
                ));
            }
        }

        if (words.Count == 0)
        {
            ShowMessage("Ошибка", "В базе нет слов для теста!");
            Destroy();
            return;
        }

        currentQuestion = 0;
        correctAnswers = 0;
        totalQuestions = words.Count;
        isForeignToNative = rbForeignToNative.Active;

        ShuffleWords();

        answerEntry.IsEditable = true;
        checkButton.Sensitive = true;
        answerEntry.Text = "";

        ShowNextQuestion();
    }

    private void ShuffleWords()
    {
        Random rng = new Random();
        int n = words.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value = words[k];
            words[k] = words[n];
            words[n] = value;
        }
    }

    private void ShowNextQuestion()
    {
        if (currentQuestion >= words.Count)
        {
            FinishTest();
            return;
        }

        var currentWord = words[currentQuestion];
        string question = isForeignToNative ? currentWord.Item1 : currentWord.Item2;
        questionLabel.Text = $"Переведите: {question}";
        statusLabel.Text = $"Правильных ответов: {correctAnswers}/{totalQuestions}";
        answerEntry.Text = "";
        answerEntry.GrabFocus();
    }

    private void OnCheckAnswer(object sender, EventArgs e)
    {
        var correctPair = words[currentQuestion];
        string correctAnswer = isForeignToNative ? correctPair.Item2 : correctPair.Item1;
        string userAnswer = answerEntry.Text.Trim();

        if (string.Equals(userAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
        {
            correctAnswers++;
            ShowMessage("Правильно!", $"Верный ответ: {correctAnswer}", MessageType.Info);
        }
        else
        {
            ShowMessage("Неправильно", $"Правильный ответ: {correctAnswer}", MessageType.Error);
        }

        currentQuestion++;
        ShowNextQuestion();
    }

    private void FinishTest()
    {
        string result = $"Тест завершён!\nПравильных ответов: {correctAnswers} из {totalQuestions}";
        ShowMessage("Результаты", result, MessageType.Info);

        using (var insertResultCmd = new SQLiteCommand(connection))
        {
            insertResultCmd.CommandText = @"
            INSERT INTO test_results (test_date, correct_answers, total_questions)
            VALUES (@date, @correct, @total)";
            insertResultCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
            insertResultCmd.Parameters.AddWithValue("@correct", correctAnswers);
            insertResultCmd.Parameters.AddWithValue("@total", totalQuestions);
            insertResultCmd.ExecuteNonQuery();
        }

        answerEntry.IsEditable = false;
        checkButton.Sensitive = false;
        questionLabel.Text = "Выберите направление и начните тест";
        statusLabel.Text = "Правильных ответов: 0/0";
    }


    private void ShowMessage(string title, string message, MessageType type = MessageType.Info)
    {
        using (var md = new MessageDialog(this, DialogFlags.Modal, type, ButtonsType.Ok, message))
        {
            md.Title = title;
            md.Run();
        }
    }
}

public class TestResult
{
    public string TestDate { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }

    public TestResult(string testDate, int correctAnswers, int totalQuestions)
    {
        TestDate = testDate;
        CorrectAnswers = correctAnswers;
        TotalQuestions = totalQuestions;
    }
}

public class AddWordWindow : Window
{
    private SQLiteConnection connection;
    private Entry wordEntry;
    private Entry translationEntry;
    private Entry transcriptionEntry;

    public AddWordWindow(SQLiteConnection connection) : base("Добавить слово")
    {
        this.connection = connection;
        SetDefaultSize(300, 200);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox vbox = new VBox();

        wordEntry = new Entry { PlaceholderText = "Введите слово" };
        translationEntry = new Entry { PlaceholderText = "Введите перевод" };
        transcriptionEntry = new Entry { PlaceholderText = "Введите транскрипцию (опционально)" };

        vbox.PackStart(wordEntry, true, true, 10);
        vbox.PackStart(translationEntry, true, true, 10);
        vbox.PackStart(transcriptionEntry, true, true, 10);

        Button addButton = new Button("Добавить");
        addButton.Clicked += OnAddButtonClicked;
        vbox.PackStart(addButton, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }

    private void OnAddButtonClicked(object sender, EventArgs e)
    {
        string word = wordEntry.Text;
        string translation = translationEntry.Text;
        string transcription = transcriptionEntry.Text;

        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(translation))
        {
            ShowMessage("Ошибка", "Поля 'Слово' и 'Перевод' обязательны.");
            return;
        }

        using (var checkCommand = new SQLiteCommand(connection))
        {
            checkCommand.CommandText = "SELECT COUNT(*) FROM words WHERE word = @word";
            checkCommand.Parameters.AddWithValue("@word", word);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count > 0)
            {
                ShowMessage("Ошибка", "Такое слово уже существует.");
                return;
            }
        }

        using (var insertCommand = new SQLiteCommand(connection))
        {
            insertCommand.CommandText = @"
                INSERT INTO words (word, translation, transcription)
                VALUES (@word, @translation, @transcription)";
            insertCommand.Parameters.AddWithValue("@word", word);
            insertCommand.Parameters.AddWithValue("@translation", translation);
            insertCommand.Parameters.AddWithValue("@transcription", transcription);
            insertCommand.ExecuteNonQuery();
        }

        ShowMessage("Успех", "Слово успешно добавлено.");
        wordEntry.Text = "";
        translationEntry.Text = "";
        transcriptionEntry.Text = "";
    }

    private void ShowMessage(string title, string message)
    {
        using (MessageDialog dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message))
        {
            dialog.Title = title;
            dialog.Run();
        }
    }
}

public class WordListWindow : Window
{
    private SQLiteConnection connection;
    private TreeView treeView;
    private ListStore listStore;
    public WordListWindow(SQLiteConnection connection) : base("Список слов")
    {
        this.connection = connection;
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox vbox = new VBox();

        listStore = new ListStore(typeof(int), typeof(string), typeof(string), typeof(string), typeof(string));
        treeView = new TreeView(listStore);

        treeView.AppendColumn("ID", new CellRendererText(), "text", 0);
        treeView.AppendColumn("Слово", new CellRendererText(), "text", 1);
        treeView.AppendColumn("Перевод", new CellRendererText(), "text", 2);
        treeView.AppendColumn("контекст использования", new CellRendererText(), "text", 3);
        treeView.AppendColumn("Статус", new CellRendererText(), "text", 4);

        vbox.PackStart(treeView, true, true, 0);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        vbox.PackStart(backButton, false, false, 0);

        Add(vbox);
        ShowAll();

        LoadWordList();
    }

    private void LoadWordList()
    {
        listStore.Clear();
        using (var command = new SQLiteCommand("SELECT w.id, w.word, w.translation, w.transcription, " +
                                               "(SELECT status FROM progress WHERE word_id = w.id ORDER BY id DESC LIMIT 1) AS status " +
                                               "FROM words w", connection))
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string word = reader["word"].ToString();
                    string translation = reader["translation"].ToString();
                    string transcription = reader["transcription"].ToString();
                    string status = reader["status"] != DBNull.Value ? reader["status"].ToString() : "";

                    listStore.AppendValues(id, word, translation, transcription, status);
                }
            }
        }
    }
}

public class StatsWindow : Window
{
    private SQLiteConnection connection;
    private List<TestResult> testResults;

    public StatsWindow(SQLiteConnection connection) : base("Статистика тестов")
    {
        this.connection = connection;
        SetDefaultSize(500, 400);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        LoadTestResults();

        VBox vbox = new VBox(false, 10);
        vbox.BorderWidth = 10;

        DrawingArea drawingArea = new DrawingArea();
        drawingArea.SetSizeRequest(480, 300);
        drawingArea.Drawn += OnDrawingAreaDrawn;
        vbox.PackStart(drawingArea, true, true, 0);

        Label summaryLabel = new Label();
        if (testResults.Count > 0)
        {
            TestResult latest = testResults[testResults.Count - 1];
            summaryLabel.Text = $"Последний тест: {latest.TestDate} — {latest.CorrectAnswers}/{latest.TotalQuestions}";
        }
        else
        {
            summaryLabel.Text = "Нет данных по тестам";
        }
        vbox.PackStart(summaryLabel, false, false, 5);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        vbox.PackStart(backButton, false, false, 5);

        Add(vbox);
        ShowAll();
    }

    private void LoadTestResults()
    {
        testResults = new List<TestResult>();
        using (var command = new SQLiteCommand("SELECT test_date, correct_answers, total_questions FROM test_results ORDER BY test_date", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string date = reader["test_date"].ToString();
                int correct = Convert.ToInt32(reader["correct_answers"]);
                int total = Convert.ToInt32(reader["total_questions"]);
                testResults.Add(new TestResult(date, correct, total));
            }
        }
    }

    private void OnDrawingAreaDrawn(object o, DrawnArgs args)
    {
        CairoCtx cr = args.Cr;
        DrawingArea area = (DrawingArea)o;
        int width = area.Allocation.Width;
        int height = area.Allocation.Height;

        cr.SetSourceRGB(1, 1, 1);
        cr.Rectangle(0, 0, width, height);
        cr.Fill();

        if (testResults.Count == 0)
            return;

        int margin = 40;
        cr.SetSourceRGB(0, 0, 0);
        cr.LineWidth = 2;

        cr.MoveTo(margin, margin);
        cr.LineTo(margin, height - margin);
        cr.LineTo(width - margin, height - margin);
        cr.Stroke();

        int numTests = testResults.Count;
        int availableWidth = width - 2 * margin;
        int barWidth = availableWidth / (numTests * 2); 
        int gap = barWidth;

        double maxPercentage = 100.0;
        double scale = (height - 2 * margin) / maxPercentage;

        Pango.Layout layout = new Pango.Layout(this.CreatePangoContext());
        layout.FontDescription = Pango.FontDescription.FromString("Sans 10");

        for (int i = 0; i < numTests; i++)
        {
            TestResult result = testResults[i];
            double percentage = (result.TotalQuestions > 0) ? ((double)result.CorrectAnswers / result.TotalQuestions * 100.0) : 0;
            int barHeight = (int)(percentage * scale);
            int x = margin + gap + i * (barWidth + gap);
            int y = height - margin - barHeight;

            cr.SetSourceRGB(0.2, 0.2, 0.8);
            cr.Rectangle(x, y, barWidth, barHeight);
            cr.Fill();

            layout.SetText($"{percentage:0}%");
            cr.MoveTo(x, y - 20);
            Pango.CairoHelper.ShowLayout(cr, layout);

            layout.SetText(result.TestDate);
            cr.MoveTo(x, height - margin + 5);
            Pango.CairoHelper.ShowLayout(cr, layout);
        }
    }
}