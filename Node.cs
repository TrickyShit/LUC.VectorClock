namespace LUC.VectorClock
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    public class Node : IComparable<Node>
    {
        private readonly int computedHashValue;
        private readonly string value;

        public Node(string value)
        {
            this.value = value;
            this.computedHashValue = 23;
            unchecked
            {
                foreach (var c in value)
                {
                    this.computedHashValue *= this.computedHashValue * 31 + c; // using the byte value of each char
                }
            }
        }

        public int CompareTo(Node other)
        {
            return Comparer<string>.Default.Compare(this.value, other.value);
        }

        public static Node Create(string name)
        {
            return Hash(name);
        }

        public static Node FromHash(string hash)
        {
            return new Node(hash);
        }

        private static Node Hash(string name)
        {
            var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(name);
            var hash = md5.ComputeHash(inputBytes);


            var sb = new StringBuilder();

            foreach (var t in hash)
            {
                sb.Append(t.ToString("X2"));
            }

            return new Node(sb.ToString());
        }

        public override int GetHashCode()
        {
            return this.computedHashValue;
        }

        public override bool Equals(object obj)
        {
            var that = obj as Node;
            if (that == null)
            {
                return false;
            }

            return this.value.Equals(that.value);
        }

        public override string ToString()
        {
            return this.value;
        }
    }
}