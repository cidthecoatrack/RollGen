﻿using NUnit.Framework;

namespace DnDGen.RollGen.Tests.Integration.Stress
{
    [TestFixture]
    public class d2Tests : ProvidedDiceTests
    {
        protected override int die
        {
            get { return 2; }
        }

        protected override int GetRoll(int quantity)
        {
            return Dice.Roll(quantity).d2().AsSum();
        }

        [Test]
        public void StressD2()
        {
            stressor.Stress(AssertRoll);
        }
    }
}