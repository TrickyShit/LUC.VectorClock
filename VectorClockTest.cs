namespace LUC.VectorClock
{
    using NUnit.Framework;

    [TestFixture]
    public class VectorClockTest
    {
        [TestCase]
        public void ShouldHaveZeroVersionsWhenCreated()
        {
            var clock = VectorClock.Create();

            Assert.AreEqual(0, clock.Versions.Count);
        }

        [TestCase]
        public void ShouldNotHappenBeforeItself()
        {
            var clock1 = VectorClock.Create();
            var clock2 = VectorClock.Create();

            Assert.AreEqual(clock1, clock2);
            Assert.IsFalse(clock1 < clock2);
            Assert.IsFalse(clock1 > clock2);

            Assert.IsFalse(clock1.IsConcurrentWith(clock2));
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest1()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");
            var clock31 = clock21.Increment("2");
            var clock41 = clock31.Increment("1");

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment("1");
            var clock32 = clock22.Increment("2");
            var clock42 = clock32.Increment("1");

            Assert.IsTrue(clock41 == clock42);
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest2()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");
            var clock31 = clock21.Increment("2");
            var clock41 = clock31.Increment("1");

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment("1");
            var clock32 = clock22.Increment("2");
            var clock42 = clock32.Increment("1");
            var clock52 = clock42.Increment("3");

            Assert.IsTrue(clock41 < clock52);
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest3()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment("2");

            Assert.IsTrue(clock21.IsConcurrentWith(clock22));
            Assert.IsTrue(clock21.IsConcurrentWith(clock22));
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest4()
        {
            var clock13 = VectorClock.Create();
            var clock23 = clock13.Increment("1");
            var clock33 = clock23.Increment("2");
            var clock43 = clock33.Increment("1");

            var clock14 = VectorClock.Create();
            var clock24 = clock14.Increment("1");
            var clock34 = clock24.Increment("1");
            var clock44 = clock34.Increment("3");

            Assert.IsTrue(clock43.IsConcurrentWith(clock44));
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest5()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("2");
            var clock31 = clock21.Increment("2");

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment("1");
            var clock32 = clock22.Increment("2");
            var clock42 = clock32.Increment("2");
            var clock52 = clock42.Increment("3");

            Assert.IsTrue(clock31 < clock52);
            Assert.IsTrue(clock52 > clock31);
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest6()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");
            var clock31 = clock21.Increment("2");

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment("1");
            var clock32 = clock22.Increment("1");

            Assert.IsTrue(clock31.IsConcurrentWith(clock32));
            Assert.IsTrue(clock32.IsConcurrentWith(clock31));
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest7()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");
            var clock31 = clock21.Increment("2");
            var clock41 = clock31.Increment("2");
            var clock51 = clock41.Increment("3");

            var clock12 = clock41;
            var clock22 = clock12.Increment("2");
            var clock32 = clock22.Increment("2");

            Assert.IsTrue(clock51.IsConcurrentWith(clock32));
            Assert.IsTrue(clock32.IsConcurrentWith(clock51));
        }

        [TestCase]
        public void ShouldPassMiscComparisonTest8()
        {
            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment("1");
            var clock31 = clock21.Increment("3");

            var clock12 = clock31.Increment("2");

            var clock41 = clock31.Increment("3");

            Assert.IsTrue(clock41.IsConcurrentWith(clock12));
            Assert.IsTrue(clock12.IsConcurrentWith(clock41));
        }

        [TestCase]
        public void ShouldCorrectlyMergeTwoClocks()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");
            var node3 = Node.Create("3");

            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment(node1);
            var clock31 = clock21.Increment(node2);
            var clock41 = clock31.Increment(node2);
            var clock51 = clock41.Increment(node3);

            var clock12 = clock41;
            var clock22 = clock12.Increment(node2);
            var clock32 = clock22.Increment(node2);

            var merged1 = clock32.Merge(clock51);
            Assert.AreEqual(3, merged1.Count);
            Assert.IsTrue(merged1.ContainsNode(node1));
            Assert.IsTrue(merged1.ContainsNode(node2));
            Assert.IsTrue(merged1.ContainsNode(node3));

            var merged2 = clock51.Merge(clock32);
            Assert.AreEqual(3, merged2.Count);
            Assert.IsTrue(merged2.ContainsNode(node1));
            Assert.IsTrue(merged2.ContainsNode(node2));
            Assert.IsTrue(merged2.ContainsNode(node3));

            Assert.IsTrue(clock32 < merged1);
            Assert.IsTrue(clock51 < merged1);

            Assert.IsTrue(clock32 < merged2);
            Assert.IsTrue(clock51 < merged2);

            Assert.IsTrue(merged1 == merged2);
        }

        [TestCase]
        public void ShouldCorrectlyMergeTwoDisjointVectorClocks()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");
            var node3 = Node.Create("3");
            var node4 = Node.Create("4");

            var clock11 = VectorClock.Create();
            var clock21 = clock11.Increment(node1);
            var clock31 = clock21.Increment(node2);
            var clock41 = clock31.Increment(node2);
            var clock51 = clock41.Increment(node3);

            var clock12 = VectorClock.Create();
            var clock22 = clock12.Increment(node4);
            var clock32 = clock22.Increment(node4);

            var merged1 = clock32.Merge(clock51);
            Assert.AreEqual(4, merged1.Count);
            Assert.IsTrue(merged1.ContainsNode(node1));
            Assert.IsTrue(merged1.ContainsNode(node2));
            Assert.IsTrue(merged1.ContainsNode(node3));
            Assert.IsTrue(merged1.ContainsNode(node4));

            var merged2 = clock51.Merge(clock32);
            Assert.AreEqual(4, merged2.Count);
            Assert.IsTrue(merged2.ContainsNode(node1));
            Assert.IsTrue(merged2.ContainsNode(node2));
            Assert.IsTrue(merged2.ContainsNode(node3));
            Assert.IsTrue(merged2.ContainsNode(node4));

            Assert.IsTrue(clock32 < merged1);
            Assert.IsTrue(clock51 < merged1);

            Assert.IsTrue(clock32 < merged2);
            Assert.IsTrue(clock51 < merged2);

            Assert.IsTrue(merged1 == merged2);
        }

        [TestCase]
        public void ShouldPassBlankClockIncrementing()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");

            var v1 = VectorClock.Create();
            var v2 = VectorClock.Create();

            var vv1 = v1.Increment(node1);
            var vv2 = v2.Increment(node2);

            Assert.IsTrue(vv1 > v1);
            Assert.IsTrue(vv2 > v2);

            Assert.IsTrue(vv1 > v2);
            Assert.IsTrue(vv2 > v1);

            Assert.IsFalse(vv2 > vv1);
            Assert.IsFalse(vv1 > vv2);
        }

        [TestCase]
        public void ShouldPassMergingBehavior()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");
            var node3 = Node.Create("3");

            var a = VectorClock.Create();
            var b = VectorClock.Create();

            var a1 = a.Increment(node1);
            var b1 = b.Increment(node2);

            var a2 = a1.Increment(node1);
            var c = a2.Merge(b1);
            var c1 = c.Increment(node3);

            Assert.IsTrue(c1 > a2);
            Assert.IsTrue(c1 > b1);
        }

        [TestCase]
        public void ShouldSupportPruning()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");
            var node3 = Node.Create("3");

            var a = VectorClock.Create();
            var b = VectorClock.Create();

            var a1 = a.Increment(node1);
            var b1 = b.Increment(node2);

            var c = a1.Merge(b1);
            var c1 = c.Prune(node1).Increment(node3);
            Assert.IsFalse(c1.ContainsNode(node1));
            Assert.IsTrue(c1.IsConcurrentWith(c));

            Assert.IsFalse(c.Prune(node1).Merge(c1).ContainsNode(node1));

            var c2 = c.Increment(node2);
            Assert.IsTrue(c1.IsConcurrentWith(c2));
        }

        [TestCase]
        public void ShouldCompareAsConcurrentWhenMemberRemoved()
        {
            var node1 = Node.Create("1");
            var node2 = Node.Create("2");

            var a = VectorClock.Create().Increment(node1).Increment(node2);
            var b = a.Prune(node2).Increment(node1); // remove node2, increment node1

            Assert.AreEqual(Ordering.Concurrent, a.CompareTo(b));
        }

        [TestCase]
        public void TestHashCode()
        {
            var aNode = Node.Create("v2");
            var a = VectorClock.Create().Increment(aNode);
            var a1Node = Node.Create("v2");
            var a1 = VectorClock.Create();
            a1=a1.Increment(a1Node);
            Assert.AreEqual(aNode.GetHashCode(), a1Node.GetHashCode());
            Assert.IsTrue(a==a1);
            Assert.AreEqual(a.GetHashCode(), a1.GetHashCode());
            Assert.AreEqual(a.ToString(), a1.ToString());
        }

        [TestCase]
        public void TestBeforeAfter()
        {
            var qNode = Node.Create("1");
            var q = VectorClock.Create();
            q=q.Increment(qNode);
            var q1Node = Node.Create("2");
            var q2Node = Node.Create("3");
            var q1 = VectorClock.Create().Increment(q1Node);
            q1 = q1.Increment((q2Node));
            q = q.Merge(q1);
            q1 = q.Increment(q1Node);
            Assert.AreEqual(Ordering.Before, q.CompareTo(q1));
            Assert.IsTrue(q<q1);
            Assert.IsTrue(q.IsBefore(q1));
            Assert.AreEqual(Ordering.After, q1.CompareTo(q));
            Assert.IsTrue(q1.IsAfter(q));
            Assert.IsTrue(q1>q);
        }


    }
}