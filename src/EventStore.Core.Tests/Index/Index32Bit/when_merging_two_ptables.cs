using System;
using System.Collections.Generic;
using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index.Index32Bit
{
    [TestFixture]
    public class when_merging_two_ptables: SpecificationWithDirectoryPerTestFixture
    {
        protected byte _ptableVersion = PTableVersions.Index32Bit;
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [OneTimeSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            for (int i = 0; i < 2; i++)
            {
                _files.Add(GetTempFilePath());

                var table = new HashListMemTable(_ptableVersion, maxSize: 20);
                for (int j = 0; j < 10; j++)
                {
                    table.Add((ulong)(0x010100000000 << (j + 1)), i + 1, i*j);
                }
                _tables.Add(PTable.FromMemtable(table, _files[i]));
            }
            _files.Add(GetTempFilePath());
            _newtable = PTable.MergeTo(_tables, _files[2], (streamId, hash) => hash, x => true, x => new System.Tuple<string, bool>("", true), _ptableVersion);
        }

        [OneTimeTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void there_are_twenty_records_in_merged_index()
        {
            Assert.AreEqual(20, _newtable.Count);
        }

        [Test]
        public void the_items_are_sorted()
        {
            var last = new IndexEntry(ulong.MaxValue, 0, long.MaxValue);
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue((last.Stream == item.Stream ? last.Version > item.Version : last.Stream > item.Stream) || 
                    ((last.Stream == item.Stream && last.Version == item.Version) && last.Position > item.Position));
                last = item;
            }
        }
    }
}