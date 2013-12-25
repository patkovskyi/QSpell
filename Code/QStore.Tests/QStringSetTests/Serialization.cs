﻿namespace QStore.Tests.QStringSetTests
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using QStore.Tests.TestComparers;
    using QStore.Tests.TestHelpers;

    [TestClass]
    public class Serialization
    {
        public static void SerializationTestHelper(
            Func<QStringSet, QStringSet> serializationLoop,
            params string[] words)
        {
            var comparer = new NonSerializableComparer<char>();
            var set = QStringSet.Create(words, comparer);
            set = serializationLoop(set);
            set.SetComparer(comparer);
            Assert.AreEqual(words.Length, set.WordCount);
            var expectedWords = words.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expectedWords, set.Enumerate().ToArray());
        }

        [TestMethod]
        [DeploymentItem(TestData.BaseformsDeployedPath)]
        public void BinaryBaseforms()
        {
            SerializationTestHelper(
                SerializationHelper.BinaryLoop,
                File.ReadAllLines(TestData.BaseformsDeployedPath, TestData.Encoding));
        }

        [TestMethod]
        [DeploymentItem(TestData.BaseformsDeployedPath)]
        public void BsonBaseforms()
        {
            SerializationTestHelper(
                SerializationHelper.BsonLoop,
                File.ReadAllLines(TestData.BaseformsDeployedPath, TestData.Encoding));
        }

        [TestMethod]
        [DeploymentItem(TestData.BaseformsDeployedPath)]
        public void DataContractBaseforms()
        {
            SerializationTestHelper(
                SerializationHelper.DataContractLoop,
                File.ReadAllLines(TestData.BaseformsDeployedPath, TestData.Encoding));
        }

        [TestMethod]
        [DeploymentItem(TestData.BaseformsDeployedPath)]
        public void ProtoBaseforms()
        {
            SerializationTestHelper(
                SerializationHelper.ProtoLoop,
                File.ReadAllLines(TestData.BaseformsDeployedPath, TestData.Encoding));
        }
    }
}