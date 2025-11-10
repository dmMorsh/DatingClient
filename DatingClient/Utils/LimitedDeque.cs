namespace DatingClient.Utils;

public class LimitedDeque<T>
{
    private readonly LinkedList<T> _list = new();
    private readonly int _maxSize;
    private readonly int _undoBuffer;
    private LinkedListNode<T>? _current;

    public LimitedDeque(int maxSize, int undoBuffer)
    {
        _maxSize = maxSize;
        _undoBuffer = undoBuffer;
    }

    public T? Current => _current is not null ? _current.Value : default;

    public bool HasPrevious => _current?.Previous is not null;
    
    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            _list.AddLast(item);

        if (_current is null)
            _current = _list.First;

        TrimStartIfNeeded();
    }
    
    public void AddToStart(IEnumerable<T> items)
    {
        foreach (var item in items.Reverse())
            _list.AddFirst(item);

        TrimEndIfNeeded();
    }
    
    public IReadOnlyCollection<T> GetAll() => _list.ToList();

    public bool MoveNext()
    {
        if (_current?.Next is null)
        {
            _current = null;
            return false;
        }
        _current = _current.Next;
        TrimStartIfNeeded();
        return true;
    }

    public bool MovePrevious()
    {
        if (_current?.Previous is null)
            return false;
        _current = _current.Previous;
        return true;
    }

    public int RemainingAhead
    {
        get
        {
            int count = 0;
            var node = _current?.Next;
            while (node is not null)
            {
                count++;
                node = node.Next;
            }
            return count;
        }
    }

    public void Clear()
    {
        _list.Clear();
        _current = null;
    }

    private void TrimStartIfNeeded()
    {
        int? distance = GetDistanceNext(_list.First, _current);
        while (_list.Count > _maxSize)
        {
            var first = _list.First;
            if (first is null)
                break;

            // wont delete elements that in "Undo" zone
            if (_undoBuffer == 0 || distance > _undoBuffer)
            {
                _list.RemoveFirst();
                distance--;
            }   
            else
                break;
        }
    }
    
    private void TrimEndIfNeeded()
    {
        while (_list.Count > _maxSize)
            _list.RemoveLast();
    }

    private static int GetDistanceNext(LinkedListNode<T>? from, LinkedListNode<T>? to)
    {
        int distance = 0;
        var node = from;
        while (node is not null && node != to)
        {
            node = node.Next;
            distance++;
        }
        return distance;
    }
}
