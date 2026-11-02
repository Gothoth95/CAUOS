namespace ClassicAssist.UI.ViewModels
{
    public sealed class MainNavEntry
    {
        public MainNavEntry( int index, string icon, string label )
        {
            Index = index;
            Icon = icon;
            Label = label;
        }

        public int Index { get; }
        public string Icon { get; }
        public string Label { get; }
    }
}
