
public class ListEqualityComparer<T> : IEqualityComparer<List<T>>
{
    // Check if two lists are equal based on their elements and order
    public bool Equals(List<T>? x, List<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        // SequenceEqual performs element-by-element comparison
        return x.SequenceEqual(y);
    }

    // Generate a hash code for a list based on its elements
    public int GetHashCode(List<T> obj)
    {
        if (obj == null) return 0;
        int hash = 17; // A prime number
        foreach (var item in obj)
        {
            // Combine the hash codes of individual elements
            hash = hash * 31 + (item == null ? 0 : item.GetHashCode());
        }
        return hash;
    }
}