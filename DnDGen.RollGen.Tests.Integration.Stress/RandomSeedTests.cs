﻿using DnDGen.RollGen.IoC;
using Ninject;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DnDGen.RollGen.Tests.Integration.Stress
{
    [TestFixture]
    public class RandomSeedTests : StressTests
    {
        [Inject]
        public Dice Dice1 { get; set; }
        [Inject]
        public Dice Dice2 { get; set; }

        private List<int> dice1Rolls;
        private List<int> dice2Rolls;

        [SetUp]
        public void Setup()
        {
            dice1Rolls = new List<int>();
            dice2Rolls = new List<int>();
        }

        [Test]
        public void RollsAreDifferentBetweenDice()
        {
            stressor.Stress(() => PopulateRolls(Dice1, Dice2));

            Assert.That(dice1Rolls, Is.Not.EqualTo(dice2Rolls));
            Assert.That(dice1Rolls.Distinct().Count(), Is.InRange(1000, Limits.Die));
            Assert.That(dice2Rolls.Distinct().Count(), Is.InRange(1000, Limits.Die));
        }

        private void PopulateRolls(Dice dice1, Dice dice2)
        {
            var firstRoll = dice1.Roll().d(Limits.Die).AsSum();
            dice1Rolls.Add(firstRoll);

            var secondRoll = dice2.Roll().d(Limits.Die).AsSum();
            dice2Rolls.Add(secondRoll);
        }

        [Test]
        public void RollsAreDifferentBetweenDiceFromFactory()
        {
            var dice1 = DiceFactory.Create();
            var dice2 = DiceFactory.Create();

            stressor.Stress(() => PopulateRolls(dice1, dice2));

            Assert.That(dice1Rolls, Is.Not.EqualTo(dice2Rolls));
            Assert.That(dice1Rolls.Distinct().Count(), Is.InRange(1000, Limits.Die));
            Assert.That(dice2Rolls.Distinct().Count(), Is.InRange(1000, Limits.Die));
        }
    }
}