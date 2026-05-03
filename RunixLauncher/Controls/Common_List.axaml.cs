using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls;

public partial class Common_List : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<Common_List, string>(nameof(Label), string.Empty);
    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    private Dictionary<Guid, DataEntry>? elements;

    private Func<Control>? factory;
    private Action<DataEntry, object>? drawElement;

    private Func<object>? createData;
    private Func<Task>? updater;

    private Guid? selected
    {
        get => m_Selected;
        set
        {
            if (m_Selected.HasValue && (elements?.ContainsKey(m_Selected.Value) ?? false))
                elements[m_Selected.Value].ToggleSelection(false);

            m_Selected = value == m_Selected ? null : value;
            btn_Remove.IsVisible = m_Selected.HasValue;

            if (m_Selected.HasValue && (elements?.ContainsKey(m_Selected.Value) ?? false))
                elements[m_Selected.Value].ToggleSelection(true);
        }
    }
    private Guid? m_Selected;

    public Common_List()
    {
        elements = new Dictionary<Guid, DataEntry>();

        InitializeComponent();
        DataContext = this;

        btn_Add.RegisterClick(AddElement);
        btn_Remove.RegisterClick(RemoveEntry);
    }

    public void Setup<ELEMENT_TYPE, DATATYPE>(Func<ELEMENT_TYPE> factory, Action<ELEMENT_TYPE, DATATYPE> drawer, Func<DATATYPE> generator, Func<List<ELEMENT_TYPE>, Task> updater)
        where ELEMENT_TYPE : Control
    {
        this.factory = factory;
        this.drawElement = (c, o) => drawer((ELEMENT_TYPE)c.itemUI, (DATATYPE)o);

        this.createData = () => (object)generator()!;
        this.updater = () => updater(GetData<ELEMENT_TYPE>());

        selected = null;
    }

    public async Task LoadAsync<T>(Func<Task<ICollection<T>>> loader)
    {
        elements?.Clear();

        // cool animation here
        Load(await loader());
    }

    public void Load<T>(ICollection<T> data)
    {
        selected = null;

        elements?.Clear();
        container.Children.Clear();

        for (int i = 0; i < data.Count; i++)
            CreateElement(data.ElementAt(i)!);
    }

    private async Task AddElement()
    {
        object o = createData!();
        CreateElement(o);
        await updater!();
    }

    private void CreateElement(object data)
    {
        DataEntry entry = new DataEntry(this, factory!());

        elements!.Add(entry.id, entry);
        drawElement!(entry, data);
    }

    private async Task RemoveEntry()
    {
        if (!selected.HasValue || !(elements?.ContainsKey(selected.Value) ?? false))
            return;

        elements[selected.Value].Remove();
        selected = null;

        await updater!();
    }

    public async Task RequestUpdate() => await updater!();

    public List<T> GetData<T>() where T : Control
        => elements!.Select(x => (T)x.Value.itemUI).ToList();

    private struct DataEntry
    {
        private Common_List master;

        public Guid id;
        public Border border;
        public Control itemUI;

        public DataEntry(Common_List master, Control itemUI)
        {
            Guid id = Guid.NewGuid();

            this.id = id;
            this.master = master;
            this.itemUI = itemUI;

            border = new Border();
            border.PointerPressed += (_, __) => master.selected = id;

            Grid row = new Grid();
            row.Margin = new Thickness(5);
            row.ColumnDefinitions = [new ColumnDefinition(20, GridUnitType.Pixel), new ColumnDefinition(GridLength.Star)];

            Grid.SetColumn(itemUI, 1);
            row.Children.Add(itemUI);

            border.Child = row;
            master.container.Children.Add(border);

            ToggleSelection(false);
        }

        public void Remove()
        {
            master.container.Children.Remove(border);
            master.elements!.Remove(id);
        }

        public void ToggleSelection(bool to)
        {
            border.Background = to ? CommonColours.SettingsList_selectedBrush : CommonColours.SettingsList_unselectedBrush;
        }
    }
}