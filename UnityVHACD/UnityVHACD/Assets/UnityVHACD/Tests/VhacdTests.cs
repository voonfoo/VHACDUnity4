using System;
using NUnit.Framework;

namespace Vhacd.Tests
{
    public class VhacdTests
    {
        [Test]
        public void TestGetConvexHullWithoutDecompose()
        {
            IUnityVhacd vhacd = new UnityVhacd(VhacdParameters.Default);
            Assert.IsTrue(vhacd.GetNConvexHulls() == 0);
            Assert.Throws<InvalidOperationException>(() => vhacd.GetConvexHull(0));
        }
    }
}