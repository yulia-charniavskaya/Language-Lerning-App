using System;
using Gtk;
using System.Data.SQLite;

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
                CREATE TABLE IF NOT EXISTS progress (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    word_id INTEGER NOT NULL,
                    status TEXT NOT NULL,
                    FOREIGN KEY (word_id) REFERENCES words (id)
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

    public StatsWindow(SQLiteConnection connection) : base("Статистика")
    {
        this.connection = connection;
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox vbox = new VBox();

        var rememberedCount = GetProgressCount("запомнил");
        var stillLearningCount = GetProgressCount("ещё учу");

        Label statsLabel = new Label($"Запомнено: {rememberedCount}, Ещё учу: {stillLearningCount}");
        vbox.PackStart(statsLabel, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }

    private int GetProgressCount(string status)
    {
        using (var command = new SQLiteCommand(connection))
        {
            command.CommandText = "SELECT COUNT(*) FROM progress WHERE status = @status";
            command.Parameters.AddWithValue("@status", status);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}