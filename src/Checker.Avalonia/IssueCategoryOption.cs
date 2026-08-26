using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDCLogChecker.Avalonia;

public sealed class IssueCategoryOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public IssueCategoryOption(string categoryText, int count, bool isAll = false)
    {
        CategoryText = categoryText;
        Count = count;
        IsAll = isAll;
    }

    public string CategoryText { get; }
    public int Count { get; }
    public bool IsAll { get; }
    public string DisplayText => $"{CategoryText} {Count}";
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(Foreground));
        }
    }

    public string BorderColor => IsSelected ? (IsAll ? "#C0392B" : "#47779D") : "#CAD6DE";
    public string Foreground => IsSelected ? (IsAll ? "#B94339" : "#315A7D") : "#536A7D";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
