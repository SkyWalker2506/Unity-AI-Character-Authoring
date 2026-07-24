using System.Collections.Generic;
using NUnit.Framework;

namespace BlackMountains.AICharacterAuthoring.Tests
{
    public sealed class ValueStateAndCanonicalTests
    {
        [Test]
        public void AllValueStatesRemainDistinct()
        {
            var values = new[]
            {
                AuthoringFieldValue.Unspecified(),
                AuthoringFieldValue.Absent(),
                AuthoringFieldValue.Null(),
                AuthoringFieldValue.Unknown(),
                AuthoringFieldValue.Computed(),
                AuthoringFieldValue.Known(CanonicalValue.String("value"))
            };

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (i == j) Assert.That(values[i], Is.EqualTo(values[j]));
                    else Assert.That(values[i], Is.Not.EqualTo(values[j]));
                }
            }
        }

        [Test]
        public void CanonicalMapHashIsInvariantToInputOrder()
        {
            var a = CanonicalValue.Map(new Dictionary<string, CanonicalValue>
            {
                ["b"] = CanonicalValue.Integer(2),
                ["a"] = CanonicalValue.String("one")
            });
            var b = CanonicalValue.Map(new Dictionary<string, CanonicalValue>
            {
                ["a"] = CanonicalValue.String("one"),
                ["b"] = CanonicalValue.Integer(2)
            });

            Assert.That(CanonicalSerialization.Serialize(a), Is.EqualTo(CanonicalSerialization.Serialize(b)));
            Assert.That(CanonicalSerialization.Hash(a), Is.EqualTo(CanonicalSerialization.Hash(b)));
        }

        [Test]
        public void DecimalUsesInvariantCanonicalText()
        {
            var value = AuthoringFieldValue.Known(CanonicalValue.Decimal(12.500m));
            Assert.That(value.ToString(), Does.Contain("\"value\":\"12.5\""));
            Assert.That(value.ToString(), Does.Not.Contain("\"value\":\"12,5\""));
        }
    }
}
