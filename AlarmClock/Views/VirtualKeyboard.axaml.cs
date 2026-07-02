using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace AlarmClock.Views;

public partial class VirtualKeyboardView : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<VirtualKeyboardView, string>(
            nameof(Text),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.Register<VirtualKeyboardView, ICommand?>(nameof(SubmitCommand));

    public static readonly StyledProperty<ICommand?> DismissCommandProperty =
        AvaloniaProperty.Register<VirtualKeyboardView, ICommand?>(nameof(DismissCommand));

    private readonly List<KeyButton> _buttons = [];
    private bool _isShiftActive;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? SubmitCommand
    {
        get => GetValue(SubmitCommandProperty);
        set => SetValue(SubmitCommandProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public VirtualKeyboardView()
    {
        InitializeComponent();
        BuildKeyboard();
    }

    private void BuildKeyboard()
    {
        AddRow(0, [
            Key("`", "~"), Key("1", "!"), Key("2", "@"), Key("3", "#"),
            Key("4", "$"), Key("5", "%"), Key("6", "^"), Key("7", "&"),
            Key("8", "*"), Key("9", "("), Key("0", ")"), Key("-", "_"),
            Key("=", "+"), Action("Back", KeyKind.Backspace, 84)
        ]);

        AddRow(20, [
            Key("q"), Key("w"), Key("e"), Key("r"), Key("t"), Key("y"), Key("u"),
            Key("i"), Key("o"), Key("p"), Key("[", "{"), Key("]", "}"), Key("\\", "|")
        ]);

        AddRow(40, [
            Key("a"), Key("s"), Key("d"), Key("f"), Key("g"), Key("h"),
            Key("j"), Key("k"), Key("l"), Key(";", ":"), Key("'", "\""),
            Action("Done", KeyKind.Submit, 84)
        ]);

        AddRow(0, [
            Action("Shift", KeyKind.Shift, 84),
            Key("z"), Key("x"), Key("c"), Key("v"), Key("b"), Key("n"),
            Key("m"), Key(",", "<"), Key(".", ">"), Key("/", "?"),
            Action("Shift", KeyKind.Shift, 84)
        ]);

        AddRow(0, [
            Action("Hide", KeyKind.Dismiss, 84),
            Action("Space", KeyKind.Space, 260),
            Action("Clear", KeyKind.Clear, 84),
            Action("Done", KeyKind.Submit, 120)
        ]);
    }

    private void AddRow(double leftMargin, IReadOnlyCollection<KeySpec> keys)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(leftMargin, 0, 0, 0)
        };

        foreach (var key in keys)
        {
            AddButton(row, key);
        }

        KeyboardRows.Children.Add(row);
    }

    private void AddButton(StackPanel row, KeySpec key)
    {
        var button = new Button
        {
            Content = GetLabel(key),
            Width = key.Width,
            Tag = key
        };

        button.Classes.Add("keyboard-key");

        if (key.Kind != KeyKind.Character)
        {
            button.Classes.Add("keyboard-action");
        }

        if (key.Kind == KeyKind.Shift)
        {
            button.Classes.Add("keyboard-shift");
        }

        button.Click += OnKeyClicked;

        _buttons.Add(new KeyButton(button, key));
        row.Children.Add(button);
    }

    private void OnKeyClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: KeySpec key })
        {
            return;
        }

        switch (key.Kind)
        {
            case KeyKind.Character:
                Text += GetCharacter(key);
                break;
            case KeyKind.Backspace:
                if (Text.Length > 0)
                {
                    Text = Text[..^1];
                }
                break;
            case KeyKind.Shift:
                _isShiftActive = !_isShiftActive;
                RefreshLabels();
                break;
            case KeyKind.Space:
                Text += " ";
                break;
            case KeyKind.Clear:
                Text = string.Empty;
                break;
            case KeyKind.Submit:
                ExecuteCommand(SubmitCommand);
                break;
            case KeyKind.Dismiss:
                ExecuteCommand(DismissCommand);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key.Kind), key.Kind, null);
        }
    }

    private void RefreshLabels()
    {
        foreach (var keyButton in _buttons)
        {
            keyButton.Button.Content = GetLabel(keyButton.Key);

            if (keyButton.Key.Kind == KeyKind.Shift)
            {
                if (_isShiftActive && !keyButton.Button.Classes.Contains("active"))
                {
                    keyButton.Button.Classes.Add("active");
                }
                else if (!_isShiftActive)
                {
                    keyButton.Button.Classes.Remove("active");
                }
            }
        }
    }

    private string GetLabel(KeySpec key)
    {
        return key.Kind == KeyKind.Character ? GetCharacter(key) : key.Normal;
    }

    private string GetCharacter(KeySpec key)
    {
        if (!_isShiftActive)
        {
            return key.Normal;
        }

        return key.Shift ?? key.Normal.ToUpperInvariant();
    }

    private void ExecuteCommand(ICommand? command)
    {
        if (command?.CanExecute(Text) == true)
        {
            command.Execute(Text);
        }
    }

    private static KeySpec Key(string normal, string? shift = null)
    {
        return new KeySpec(KeyKind.Character, normal, shift, 40);
    }

    private static KeySpec Action(string label, KeyKind kind, double width)
    {
        return new KeySpec(kind, label, null, width);
    }

    private enum KeyKind
    {
        Character,
        Backspace,
        Shift,
        Space,
        Clear,
        Submit,
        Dismiss
    }

    private sealed record KeySpec(KeyKind Kind, string Normal, string? Shift, double Width);

    private sealed record KeyButton(Button Button, KeySpec Key);
}
