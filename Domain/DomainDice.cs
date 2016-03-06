﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RollGen.Domain
{
    public class DomainDice : Dice
    {
        private Regex rollRegex;
        private Regex lenientRollRegex;
        private Regex expressionRegex;
        private ExpressionEvaluator expressionEvaluator;
        private PartialRollFactory partialRollFactory;

        public DomainDice(ExpressionEvaluator expressionEvaluator, PartialRollFactory partialRollFactory)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.partialRollFactory = partialRollFactory;

            rollRegex = new Regex("(?:(?:\\d* +)|(?:\\d+ *)|^)d *\\d+");
            lenientRollRegex = new Regex("\\d* *d *\\d+");
            expressionRegex = new Regex("(?:[-+]?\\d*\\.?\\d+[%/\\+\\-\\*])+(?:[-+]?\\d*\\.?\\d+)");
        }

        public PartialRoll Roll(int quantity = 1)
        {
            return partialRollFactory.Build(quantity);
        }

        public string ReplaceWrappedExpressions<T>(string str, string openexpr = "{", string closeexpr = "}", char? openexprescape = '\\')
        {
            var regex = new Regex($"{(openexprescape != null ? $"(?:[^{Regex.Escape(openexprescape.ToString())}]|^)" : "")}{Regex.Escape(openexpr)}(.*?){Regex.Escape(closeexpr)}");
            foreach (Match match in regex.Matches(str))
            {
                var m = match.Groups[1].Value;
                T evaluated_match = Evaluate<T>(ReplaceDiceExpression(m, true));
                str = ReplaceFirst(str, openexpr+m+closeexpr, evaluated_match.ToString());
            }
            return openexprescape == null ? str : str.Replace(openexprescape+openexpr, openexpr);
        }

        public object Evaluate(string expression)
        {
            var match = rollRegex.Match(expression);

            if (match.Success)
            {
                var message = string.Format("Cannot evaluate unrolled die roll {0}", match.Value);
                throw new ArgumentException(message);
            }

            return expressionEvaluator.Evaluate(expression);
        }

        public int Roll(string roll)
        {
            var expression = ReplaceExpressionWithTotal(roll, true);
            return Evaluate<int>(expression);
        }

        public T Evaluate<T>(string expression)
        {
            var rawEvaluatedExpression = Evaluate(expression);

            if (rawEvaluatedExpression is T)
                return (T)rawEvaluatedExpression;

            return (T)Convert.ChangeType(rawEvaluatedExpression, typeof(T));
        }

        public string ReplaceRollsWithSum(string expression)
        {
            expression = Replace(expression, rollRegex, s => CreateSumOfRolls(s));

            return expression.Trim();
        }

        private string CreateSumOfRolls(string roll)
        {
            var rolls = GetIndividualRolls(roll);
            var count = rolls.Count();
            if (count <= 1)
                return count == 1 ? rolls.ElementAt(0).ToString() : "0";
            var sumOfRolls = string.Join(" + ", rolls);
            return string.Format("({0})", sumOfRolls);
        }

        private IEnumerable<int> GetIndividualRolls(string roll)
        {
            var sections = roll.Split('d');
            var die = Convert.ToInt32(sections[1]);
            var quantity = 1;

            if (string.IsNullOrEmpty(sections[0]) == false)
                quantity = Convert.ToInt32(sections[0]);

            return Roll(quantity).IndividualRolls(die);
        }


        public bool ContainsRoll(string expression, bool lenient = false) =>
            (lenient ? lenientRollRegex : rollRegex).IsMatch(expression);

        private string ReplaceDiceExpression(string expression, bool lenient = false) =>
            Replace(expression, lenient ? lenientRollRegex : rollRegex, s => CreateTotalOfRolls(s));

        public string ReplaceExpressionWithTotal(string expression, bool lenient = false)
        {
            expression = ReplaceDiceExpression(expression, lenient);
            expression = Replace(expression, expressionRegex, Evaluate);

            return expression;
        }

        private int CreateTotalOfRolls(string roll)
        {
            var rolls = GetIndividualRolls(roll);
            return rolls.Sum();
        }

        private string ReplaceFirst(string input, string to_replace, string replacement)
        {
            var index = input.IndexOf(to_replace);
            input = input.Remove(index, to_replace.Length);
            input = input.Insert(index, replacement);
            return input;
        }

        private string Replace(string expression, Regex regex, Func<string, object> createReplacement)
        {
            var matches = regex.Matches(expression);

            foreach (Match match in matches)
            {
                var matchValue = match.Value.Trim();
                var replacement = createReplacement(matchValue);
                expression = ReplaceFirst(expression, matchValue, replacement.ToString());
            }

            return expression;
        }
    }
}