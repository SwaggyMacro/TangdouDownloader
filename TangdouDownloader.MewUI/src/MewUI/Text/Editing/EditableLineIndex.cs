namespace Aprillz.MewUI.Text.Editing;

internal readonly record struct EditableLineRecord(int Length, int DelimiterLength)
{
    public int TotalLength => Length + DelimiterLength;
}

internal readonly record struct EditableLinePosition(
    int LineNumber,
    int Offset,
    int Length,
    int DelimiterLength);

/// <summary>
/// Implicit treap of logical-line lengths. Prefix character counts and line lookup remain
/// logarithmic while edits add or remove lines without shifting every following offset.
/// </summary>
internal sealed class EditableLineIndex
{
    private Node? _root;
    private uint _priorityState = 0x9E3779B9u;

    public int Count => GetCount(_root);
    public int TextLength => GetTotalLength(_root);

    public void Reset(IReadOnlyList<EditableLineRecord> lines)
    {
        _root = null;
        for (int index = 0; index < lines.Count; index++)
        {
            _root = Merge(_root, new Node(lines[index], NextPriority()));
        }
    }

    public EditableLinePosition GetByNumber(int lineNumber)
    {
        if ((uint)lineNumber >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }

        int offset = 0;
        int remaining = lineNumber;
        Node? current = _root;
        while (current is not null)
        {
            int leftCount = GetCount(current.Left);
            if (remaining < leftCount)
            {
                current = current.Left;
                continue;
            }

            offset += GetTotalLength(current.Left);
            if (remaining == leftCount)
            {
                return new EditableLinePosition(
                    lineNumber,
                    offset,
                    current.Record.Length,
                    current.Record.DelimiterLength);
            }

            offset += current.Record.TotalLength;
            remaining -= leftCount + 1;
            current = current.Right;
        }

        throw new InvalidOperationException("The line index is inconsistent.");
    }

    public EditableLinePosition GetByOffset(int offset, int textLength)
    {
        if (offset < 0 || offset > textLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (offset == textLength)
        {
            return GetByNumber(Count - 1);
        }

        int lineNumber = 0;
        int lineOffset = 0;
        int remaining = offset;
        Node? current = _root;
        while (current is not null)
        {
            int leftLength = GetTotalLength(current.Left);
            int leftCount = GetCount(current.Left);
            if (remaining < leftLength)
            {
                current = current.Left;
                continue;
            }

            remaining -= leftLength;
            lineOffset += leftLength;
            lineNumber += leftCount;
            if (remaining < current.Record.TotalLength)
            {
                return new EditableLinePosition(
                    lineNumber,
                    lineOffset,
                    current.Record.Length,
                    current.Record.DelimiterLength);
            }

            remaining -= current.Record.TotalLength;
            lineOffset += current.Record.TotalLength;
            lineNumber++;
            current = current.Right;
        }

        return GetByNumber(Count - 1);
    }

    public void SetLineLength(int lineNumber, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
        _root = SetLineLength(_root, lineNumber, length);
    }

    public void ReplaceRange(int startLine, int count, IReadOnlyList<EditableLineRecord> replacement)
    {
        if (startLine < 0 || count < 0 || startLine > Count - count)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine));
        }
        if (replacement.Count == 0)
        {
            throw new ArgumentException("A document must retain at least one logical line.", nameof(replacement));
        }

        Split(_root, startLine, out var before, out var remaining);
        Split(remaining, count, out _, out var after);
        Node? inserted = null;
        for (int index = 0; index < replacement.Count; index++)
        {
            inserted = Merge(inserted, new Node(replacement[index], NextPriority()));
        }
        _root = Merge(Merge(before, inserted), after);
    }

    private static Node SetLineLength(Node? node, int lineNumber, int length)
    {
        if (node is null)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }
        int leftCount = GetCount(node.Left);
        if (lineNumber < leftCount)
        {
            node.Left = SetLineLength(node.Left, lineNumber, length);
        }
        else if (lineNumber > leftCount)
        {
            node.Right = SetLineLength(node.Right, lineNumber - leftCount - 1, length);
        }
        else
        {
            node.Record = node.Record with { Length = length };
        }
        Update(node);
        return node;
    }

    private static void Split(Node? root, int leftCount, out Node? left, out Node? right)
    {
        if (root is null)
        {
            left = null;
            right = null;
            return;
        }

        int rootLeftCount = GetCount(root.Left);
        if (leftCount <= rootLeftCount)
        {
            Split(root.Left, leftCount, out left, out var newLeft);
            root.Left = newLeft;
            Update(root);
            right = root;
        }
        else
        {
            Split(root.Right, leftCount - rootLeftCount - 1, out var newRight, out right);
            root.Right = newRight;
            Update(root);
            left = root;
        }
    }

    private static Node? Merge(Node? left, Node? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        if (left.Priority <= right.Priority)
        {
            left.Right = Merge(left.Right, right);
            Update(left);
            return left;
        }
        right.Left = Merge(left, right.Left);
        Update(right);
        return right;
    }

    private uint NextPriority()
    {
        uint value = _priorityState;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _priorityState = value;
        return value;
    }

    private static int GetCount(Node? node) => node?.Count ?? 0;
    private static int GetTotalLength(Node? node) => node?.TotalLength ?? 0;

    private static void Update(Node node)
    {
        node.Count = GetCount(node.Left) + 1 + GetCount(node.Right);
        node.TotalLength = GetTotalLength(node.Left) + node.Record.TotalLength + GetTotalLength(node.Right);
    }

    private sealed class Node(EditableLineRecord record, uint priority)
    {
        public EditableLineRecord Record { get; set; } = record;
        public uint Priority { get; } = priority;
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Count { get; set; } = 1;
        public int TotalLength { get; set; } = record.TotalLength;
    }
}
