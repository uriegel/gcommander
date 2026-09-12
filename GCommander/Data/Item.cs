using System.ComponentModel;

class Item : INotifyPropertyChanged
{
    public string Name { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Item(string name) => Name = name;
    protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}    
