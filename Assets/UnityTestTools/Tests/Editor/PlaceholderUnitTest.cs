using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace UnityTest
{
    [TestFixture]
    internal class PlaceholderUnitTest
    {
        [Test]
        [Category("Placeholder test")]
        public void PassingTest()
        {
            Assert.Pass();
        }
    }
}
