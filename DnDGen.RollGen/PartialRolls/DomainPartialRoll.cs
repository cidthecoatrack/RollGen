﻿using RollGen.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RollGen.PartialRolls
{
    internal class DomainPartialRoll : PartialRoll
    {
        private readonly Random random;
        private readonly ExpressionEvaluator expressionEvaluator;
        private readonly Regex strictRollRegex;
        private readonly Regex booleanExpressionRegex;

        private DomainPartialRoll(Random random, ExpressionEvaluator expressionEvaluator)
        {
            this.random = random;
            this.expressionEvaluator = expressionEvaluator;

            strictRollRegex = new Regex(RegexConstants.StrictRollPattern);
            booleanExpressionRegex = new Regex(RegexConstants.BooleanExpressionPattern);
        }

        public DomainPartialRoll(int quantity, Random random, ExpressionEvaluator expressionEvaluator)
            : this(random, expressionEvaluator)
        {
            CurrentRollExpression = $"{quantity}";
        }

        public DomainPartialRoll(string quantity, Random random, ExpressionEvaluator expressionEvaluator)
            : this(random, expressionEvaluator)
        {
            CurrentRollExpression = $"({quantity})";
        }

        public override int AsSum()
        {
            var rolls = GetIndividualRolls(CurrentRollExpression);
            return rolls.Sum();
        }

        public override IEnumerable<int> AsIndividualRolls()
        {
            var rolls = GetIndividualRolls(CurrentRollExpression);
            return rolls;
        }

        public override double AsPotentialAverage()
        {
            var average = GetAverageRoll(CurrentRollExpression);
            return average;
        }

        public override int AsPotentialMinimum()
        {
            var minimum = GetMinimumRoll(CurrentRollExpression);
            return minimum;
        }

        public override int AsPotentialMaximum()
        {
            var maximum = GetMaximumRoll(CurrentRollExpression);
            return maximum;
        }

        public override bool AsTrueOrFalse(double threshold = .5)
        {
            if (booleanExpressionRegex.IsMatch(CurrentRollExpression))
                return EvaluateExpressionWithRollsAsTrueOrFalse(CurrentRollExpression);

            var minimum = AsPotentialMinimum();
            var maximum = AsPotentialMaximum();
            var sum = AsSum();
            var difference = minimum - 1;
            var percentage = (sum - difference) / (double)(maximum - difference);

            return percentage <= threshold;
        }

        private bool EvaluateExpressionWithRollsAsTrueOrFalse(string rollExpression)
        {
            var expression = ReplaceRollsInExpression(rollExpression, GetTotalOfRolls);
            var evaluatedExpression = expressionEvaluator.Evaluate<bool>(expression);
            return evaluatedExpression;
        }

        public override PartialRoll d(int die)
        {
            CurrentRollExpression += $"d{die}";
            return this;
        }

        public override PartialRoll d(string die)
        {
            CurrentRollExpression += $"d({die})";
            return this;
        }

        public override PartialRoll Keeping(int amountToKeep)
        {
            CurrentRollExpression += $"k{amountToKeep}";
            return this;
        }

        public override PartialRoll Keeping(string amountToKeep)
        {
            CurrentRollExpression += $"k({amountToKeep})";
            return this;
        }

        public override PartialRoll Explode()
        {
            CurrentRollExpression += "!";
            return this;
        }

        private IEnumerable<int> GetIndividualRolls(string rollExpression)
        {
            //INFO: Not sure how to evaluate individual rolls from genuine expressions (6d5d4k3d2k1), so will compute those as 1 roll
            if (Roll.CanParse(rollExpression) == false)
            {
                var evaluatedExpression = EvaluateExpression(rollExpression, GetTotalOfRolls);
                return new[] { evaluatedExpression };
            }

            var roll = new Roll(rollExpression);
            var rolls = roll.GetRolls(random);

            return rolls;
        }

        private T EvaluateExpression<T>(string rollExpression, Func<string, T> getRoll)
        {
            var expression = ReplaceRollsInExpression(rollExpression, getRoll);
            var evaluatedExpression = expressionEvaluator.Evaluate<T>(expression);
            return evaluatedExpression;
        }

        private string ReplaceRollsInExpression<T>(string rollExpression, Func<string, T> getRoll)
        {
            var expressionWithReplacedRolls = rollExpression;

            //1. Replace paranthetical expressions
            while (expressionWithReplacedRolls.Contains('('))
            {
                var openParanthesisIndex = expressionWithReplacedRolls.IndexOf('(');
                var innerExpressionLength = GetInnerExpressionLength(expressionWithReplacedRolls, openParanthesisIndex);
                var innerExpression = expressionWithReplacedRolls.Substring(openParanthesisIndex + 1, innerExpressionLength);

                var innerExpressionWithReplacedRolls = EvaluateExpression(innerExpression, getRoll);
                var target = $"({innerExpression})";
                var replacement = innerExpressionWithReplacedRolls.ToString();

                if (NeedLeadingMultiplier(expressionWithReplacedRolls))
                {
                    replacement = $"*{replacement}";
                }

                if (NeedFollowingMultiplier(expressionWithReplacedRolls))
                {
                    replacement = $"{replacement}*";
                }

                expressionWithReplacedRolls = ReplaceFirst(expressionWithReplacedRolls, $"({innerExpression})", replacement);
            }

            //2. Replace rolls
            var match = strictRollRegex.Match(expressionWithReplacedRolls);

            while (match.Success)
            {
                var matchValue = match.Value.Trim();
                var replacement = getRoll(matchValue);

                expressionWithReplacedRolls = ReplaceFirst(expressionWithReplacedRolls, matchValue, replacement.ToString());
                match = strictRollRegex.Match(expressionWithReplacedRolls);
            }

            return expressionWithReplacedRolls;
        }

        private bool NeedLeadingMultiplier(string expression)
        {
            var openParanthesisIndex = expression.IndexOf('(');
            if (openParanthesisIndex < 1)
                return false;

            var leadingCharacter = expression[openParanthesisIndex - 1];

            return char.IsDigit(leadingCharacter) || leadingCharacter == ')';
        }

        private bool NeedFollowingMultiplier(string expression)
        {
            var openParanthesisIndex = expression.IndexOf('(');
            if (openParanthesisIndex < 0)
                return false;

            var innerExpressionLength = GetInnerExpressionLength(expression, openParanthesisIndex);
            var followingIndex = openParanthesisIndex + innerExpressionLength + 2;

            if (followingIndex >= expression.Length)
                return false;

            var followingCharacter = expression[followingIndex];

            return char.IsDigit(followingCharacter) || followingCharacter == '(';
        }

        private int GetInnerExpressionLength(string expression, int openingIndex)
        {
            var innerCount = 0;
            var closingIndex = 0;
            for (var i = openingIndex + 1; i < expression.Length; i++)
            {
                if (innerCount == 0 && expression[i] == ')')
                {
                    closingIndex = i;
                    break;
                }

                if (expression[i] == '(')
                {
                    innerCount++;
                }
                else if (expression[i] == ')')
                {
                    innerCount--;
                }
            }

            if (closingIndex == 0)
            {
                throw new InvalidOperationException($"No closing paranthesis found for expression '{expression}'");
            }

            return closingIndex - openingIndex - 1;
        }

        private int GetTotalOfRolls(string rollExpression)
        {
            var rolls = GetIndividualRolls(rollExpression);
            return rolls.Sum();
        }

        private double GetAverageRoll(string rollExpression)
        {
            if (Roll.CanParse(rollExpression) == false)
                return EvaluateExpression(rollExpression, GetAverageRoll);

            var roll = new Roll(rollExpression);
            var average = roll.GetPotentialAverage();

            return average;
        }

        private int GetMinimumRoll(string rollExpression)
        {
            if (Roll.CanParse(rollExpression) == false)
                return EvaluateExpression(rollExpression, GetMinimumRoll);

            var roll = new Roll(rollExpression);
            var minimum = roll.GetPotentialMinimum();

            return minimum;
        }

        private int GetMaximumRoll(string rollExpression)
        {
            if (Roll.CanParse(rollExpression) == false)
                return EvaluateExpression(rollExpression, GetMaximumRoll);

            var roll = new Roll(rollExpression);
            var maximum = roll.GetPotentialMaximum();

            return maximum;
        }

        private string ReplaceFirst(string source, string target, string replacement)
        {
            var index = source.IndexOf(target);
            source = source.Remove(index, target.Length);
            source = source.Insert(index, replacement);
            return source;
        }
    }
}