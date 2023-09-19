using System.Collections.Generic;

namespace RelEcs
{
    public ref struct QueryEnumerator
    {
        public IReadOnlyList<Table> Tables { get; }
        public int TableIndex { get; private set; }
        public int EntityIndex { get; private set; }

        public QueryEnumerator(IReadOnlyList<Table> tables)
        {
            Tables = tables;
            TableIndex = 0;
            EntityIndex = -1;
        }

        public bool MoveNext()
        {
            if (TableIndex == Tables.Count) return false;

            if (++EntityIndex < Tables[TableIndex].Count) return true;

            EntityIndex = 0;
            TableIndex++;

            while (TableIndex < Tables.Count && Tables[TableIndex].IsEmpty)
            {
                TableIndex++;
            }

            return TableIndex < Tables.Count && EntityIndex < Tables[TableIndex].Count;
        }
    }
}
