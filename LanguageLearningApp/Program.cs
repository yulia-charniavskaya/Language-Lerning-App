using System;
using Gtk;

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
    public MainWindow() : base("Изучение иностранных слов")
    {
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255)); 

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

        vbox.PackStart(titleLabel, false, false, 20);
        vbox.PackStart(cardButton, true, true, 5);
        vbox.PackStart(testButton, true, true, 5);
        vbox.PackStart(addWordButton, true, true, 5);
        vbox.PackStart(statsButton, true, true, 5);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Application.Quit();
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

    private void OpenCardWindow() { new CardWindow(); }
    private void OpenTestWindow() { new TestWindow(); }
    private void OpenAddWordWindow() { new AddWordWindow(); }
    private void OpenStatsWindow() { new StatsWindow(); }
}

public class CardWindow : Window
{
    public CardWindow() : base("Карточки")
    {
        SetDefaultSize(300, 200);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255));

        VBox vbox = new VBox();
        Label label = new Label(" карточки");
        label.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(label, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        backButton.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }
}

public class TestWindow : Window
{
    public TestWindow() : base("Тест")
    {
        SetDefaultSize(300, 200);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255)); 

        VBox vbox = new VBox();
        Label label = new Label("тест");
        label.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(label, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        backButton.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }
}

public class AddWordWindow : Window
{
    public AddWordWindow() : base("Добавить слово")
    {
        SetDefaultSize(300, 200);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255)); 

        VBox vbox = new VBox();

        Entry wordEntry = new Entry { PlaceholderText = "Введите слово" };
        Entry translationEntry = new Entry { PlaceholderText = "Введите перевод" };
        Entry exampleEntry = new Entry { PlaceholderText = "Введите пример использования" };

        vbox.PackStart(wordEntry, true, true, 10);
        vbox.PackStart(translationEntry, true, true, 10);
        vbox.PackStart(exampleEntry, true, true, 10);

        Button addButton = new Button("Добавить");
        addButton.Clicked += (sender, e) =>
        {
           
            Console.WriteLine($"Слово: {wordEntry.Text}, Перевод: {translationEntry.Text}, Пример: {exampleEntry.Text}");
            wordEntry.Text = "";
            translationEntry.Text = "";
            exampleEntry.Text = "";
        };
        addButton.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0));
        vbox.PackStart(addButton, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        backButton.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }
}

public class StatsWindow : Window
{
    public StatsWindow() : base("Статистика")
    {
        SetDefaultSize(300, 200);
        SetPosition(WindowPosition.Center);
        ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 255)); 

        VBox vbox = new VBox();
        Label label = new Label("статистика");
        label.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0)); 
        vbox.PackStart(label, true, true, 10);

        Button backButton = new Button("Назад");
        backButton.Clicked += (sender, e) => Destroy();
        backButton.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0));
        vbox.PackStart(backButton, true, true, 10);

        Add(vbox);
        ShowAll();
        DeleteEvent += (o, args) => Destroy();
    }
}