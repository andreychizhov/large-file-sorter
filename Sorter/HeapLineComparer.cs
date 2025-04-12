// Comparer for the priority queue (sorts by Text, then Number)
class HeapLineComparer : IComparer<(int Number, string Text)>
{
    public int Compare((int Number, string Text) a, (int Number, string Text) b)
    {
        int textCompare = a.Text.CompareTo(b.Text);
        return textCompare != 0 ? textCompare : a.Number.CompareTo(b.Number);
    }
}