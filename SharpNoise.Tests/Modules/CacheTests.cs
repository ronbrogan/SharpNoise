﻿using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SharpNoise.Tests.Modules
{
    /// <summary>
    /// Tests for the <see cref="Cache"/> module
    /// </summary>
    public class CacheTests
    {
        class TestModule : Module
        {
            public double Counter { get; private set; }

            public TestModule()
                : base(0)
            {
            }

            public override double GetValue(double x, double y, double z)
            {
                return ++Counter;
            }
        }

        public static IEnumerable<object[]> PositionData()
        {
            yield return new object[] { 0, 0, 0 };
            yield return new object[] { 1, -1, 2 };
            yield return new object[] { -10000, 100, 100 };
        }

        [Theory]
        [MemberData("PositionData")]
        public void TestGetValueCalledOnce(double x, double y, double z)
        {
            var testModule = new TestModule();
            var cache = new Cache { Source0 = testModule };

            var cachedValue = cache.GetValue(x, y, z);

            // Make sure repeated calls do not reach the test module's counter
            Assert.Equal(cachedValue, cache.GetValue(x, y, z));
            Assert.Equal(cachedValue, cache.GetValue(x, y, z));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(20)]
        [InlineData(100)]
        public void TestMultithreaded(int threadCount)
        {
            var source = new Perlin();
            var cache = new Cache { Source0 = source };

            var threadArray = new Thread[threadCount];

            bool startFlag = false;
            for (int i = 0; i < threadArray.Length; i++)
            {
                threadArray[i] = new Thread(() =>
                {
                    for (int k = 0; k < 100; k++)
                    {
                        SpinWait.SpinUntil(() => startFlag);
                        var sourceValue = source.GetValue(i + k, i, i);
                        Assert.Equal(sourceValue, cache.GetValue(i + k, i, i));
                        Assert.Equal(sourceValue, cache.GetValue(i + k, i, i));
                        Assert.Equal(sourceValue, cache.GetValue(i, i + k, i));
                        Assert.Equal(sourceValue, cache.GetValue(i, i + k, i));
                        Assert.Equal(sourceValue, cache.GetValue(i, i, i + k));
                        Assert.Equal(sourceValue, cache.GetValue(i, i, i + k));
                    }
                });
            }
            for (int i = 0; i < threadArray.Length; i++)
                threadArray[i].Start();
            startFlag = true;
            for (int i = 0; i < threadArray.Length; i++)
                threadArray[i].Join();
        }
    }
}
