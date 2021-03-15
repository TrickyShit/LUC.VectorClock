namespace LUC.VectorClock
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class VectorClock
    {
        private bool Equals(VectorClock other)
        {
            return this.IsSameAs(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is VectorClock clock && this.Equals(clock);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 23;
                foreach (var c in this.Versions)
                {
                    hashCode = hashCode * 31 + c.Key.GetHashCode();
                    hashCode = hashCode * 31 + c.Value.GetHashCode();
                }
                return hashCode;
            }
        }

        public ImmutableSortedDictionary<Node, long> Versions { get; }

        public int Count => this.Versions.Count;

        public bool ContainsNode(Node node)
        {
            return this.Versions.ContainsKey(node);
        }

        public static VectorClock Create()
        {
            return Create(ImmutableSortedDictionary.Create<Node, long>());
        }

        public static VectorClock Create(ImmutableSortedDictionary<Node, long> seedValues)
        {
            return new VectorClock(seedValues);
        }

        private VectorClock(ImmutableSortedDictionary<Node, long> versions)
        {
            this.Versions = versions;
        }

        public VectorClock Increment(Node node)
        {
            var currentTimestamp = GetOrElse(this.Versions, node, Timestamp.Zero);
            return new VectorClock(this.Versions.SetItem(node, currentTimestamp + 1));
        }
        
        public VectorClock Increment(string value)
        {
            return Increment(Node.Create(value));
        }

        public bool IsConcurrentWith(VectorClock that)
        {
            return this.CompareOnlyTo(that, Ordering.Concurrent) == Ordering.Concurrent;
        }

        public bool IsBefore(VectorClock that)
        {
            return this.CompareOnlyTo(that, Ordering.Before) == Ordering.Before;
        }

        public bool IsAfter(VectorClock that)
        {
            return this.CompareOnlyTo(that, Ordering.After) == Ordering.After;
        }

        public bool IsSameAs(VectorClock that)
        {
            return this.CompareOnlyTo(that, Ordering.Same) == Ordering.Same;
        }

        public static bool operator >(VectorClock left, VectorClock right)
        {
            return left.IsAfter(right);
        }

        public static bool operator <(VectorClock left, VectorClock right)
        {
            return left.IsBefore(right);
        }

        public static bool operator ==(VectorClock left, VectorClock right)
        {
            if (ReferenceEquals(left, null))
                return false;

            return left.IsSameAs(right);
        }

        public static bool operator !=(VectorClock left, VectorClock right)
        {
            if (ReferenceEquals(left, null))
                return false;

            return left.IsConcurrentWith(right);
        }

        private static readonly KeyValuePair<Node, long> CmpEndMarker = new KeyValuePair<Node, long>(Node.Create("endmarker"), long.MinValue);

        internal Ordering CompareOnlyTo(VectorClock that, Ordering order)
        {
            if (ReferenceEquals(this, that) || this.Versions.Equals(that.Versions)) return Ordering.Same;

            return Compare(this.Versions.GetEnumerator(), that.Versions.GetEnumerator(), order == Ordering.Concurrent ? Ordering.FullOrder : order);
        }

        private static Ordering Compare(IEnumerator<KeyValuePair<Node, long>> i1, IEnumerator<KeyValuePair<Node, long>> i2, Ordering requestedOrder)
        {
            Ordering CompareNext(KeyValuePair<Node, long> nt1, KeyValuePair<Node, long> nt2, Ordering currentOrder)
            {
                while (true)
                {
                    if (requestedOrder != Ordering.FullOrder && currentOrder != Ordering.Same && currentOrder != requestedOrder) return currentOrder;

                    if (nt1.Equals(CmpEndMarker) && nt2.Equals(CmpEndMarker)) return currentOrder;

                    // i1 is empty but i2 is not, so i1 can only be Before
                    if (nt1.Equals(CmpEndMarker)) return currentOrder == Ordering.After ? Ordering.Concurrent : Ordering.Before;

                    // i2 is empty but i1 is not, so i1 can only be After
                    if (nt2.Equals(CmpEndMarker)) return currentOrder == Ordering.Before ? Ordering.Concurrent : Ordering.After;

                    // compare the nodes
                    var nc = nt1.Key.CompareTo(nt2.Key);
                    if (nc == 0)
                    {
                        // both nodes exist compare the timestamps
                        // same timestamp so just continue with the next nodes   
                        if (nt1.Value == nt2.Value)
                        {
                            nt1 = NextOrElse(i1, CmpEndMarker);
                            nt2 = NextOrElse(i2, CmpEndMarker);
                            continue;
                        }

                        if (nt1.Value < nt2.Value)
                        {
                            // t1 is less than t2, so i1 can only be Before
                            if (currentOrder == Ordering.After) return Ordering.Concurrent;
                            nt1 = NextOrElse(i1, CmpEndMarker);
                            nt2 = NextOrElse(i2, CmpEndMarker);
                            currentOrder = Ordering.Before;
                            continue;
                        }

                        if (currentOrder == Ordering.Before) return Ordering.Concurrent;

                        nt1 = NextOrElse(i1, CmpEndMarker);
                        nt2 = NextOrElse(i2, CmpEndMarker);
                        currentOrder = Ordering.After;

                        continue;
                    }

                    if (nc < 0)
                    {
                        // this node only exists in i1 so i1 can only be After
                        if (currentOrder == Ordering.Before) return Ordering.Concurrent;

                        nt1 = NextOrElse(i1, CmpEndMarker);
                        currentOrder = Ordering.After;

                        continue;
                    }

                    // this node only exists in i2 so i1 can only be Before
                    if (currentOrder == Ordering.After) return Ordering.Concurrent;

                    nt2 = NextOrElse(i2, CmpEndMarker);
                    currentOrder = Ordering.Before;
                }
            }

            return CompareNext(NextOrElse(i1, CmpEndMarker), NextOrElse(i2, CmpEndMarker), Ordering.Same);
        }

        private static T NextOrElse<T>(IEnumerator<T> iteration, T @default)
        {
            return iteration.MoveNext() ? iteration.Current : @default;
        }

        public Ordering CompareTo(VectorClock that)
        {
            return this.CompareOnlyTo(that, Ordering.FullOrder);
        }

        public VectorClock Merge(VectorClock that)
        {
            var mergedVersions = that.Versions;
            foreach (var pair in this.Versions)
            {
                var mergedVersionsCurrentTime = GetOrElse(mergedVersions, pair.Key, Timestamp.Zero);
                if (pair.Value > mergedVersionsCurrentTime)
                {
                    mergedVersions = mergedVersions.SetItem(pair.Key, pair.Value);
                }
            }

            return new VectorClock(mergedVersions);
        }

        public VectorClock Prune(Node removedNode)
        {
            var newVersions = this.Versions.Remove(removedNode);
            if (!ReferenceEquals(newVersions, this.Versions))
            {
                return Create(newVersions);
            }
            return this;
        }

        public override string ToString()
        {
            var versionsString = this.Versions.Select(p => p.Key + "->" + p.Value);

            return $"VectorClock({string.Join(", ", versionsString)})";
        }

        private static TValue GetOrElse<TKey, TValue>(IDictionary<TKey, TValue> hash, TKey key, TValue elseValue)
        {
            if (hash.TryGetValue(key, out var value))
                return value;
            return elseValue;
        }
    }
}

