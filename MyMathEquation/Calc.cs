using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyMathEquation
{
    public class Calc
    {
        char oper;
        decimal num;
        int precedence; // Assists with print order, not the actual calculation.

        Calc left;
        Calc right;

        /// <summary>
        /// Tests for NULL, that all chars are numbers or operators and that the last
        /// char is a number.
        /// </summary>
        /// <param name="equation">User input equation</param>
        /// <returns>True if valid</returns>
        /// 
        public static bool IsValidEquation(string equation)
        {
            // check null test and last char must be a number
            if (String.IsNullOrWhiteSpace(equation) || !Char.IsNumber(equation[equation.Length - 1]))
            {
                return false;
            }

            // All chars must be a number or operator
            for (int i = 0; i < equation.Length; i++)
            {
                char character = equation[i];

                if (!Char.IsNumber(character))
                {
                    if (character != '*' && character != '/' && character != '+' && character != '-' && character != '.')
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Solves the equation
        /// </summary>
        /// <param name="equation">String of valid numbers and operators</param>
        /// <returns>String[] of the solution in the order of operations</returns>
        public static double Solve(string equation)
        {
            IEnumerable<String> parsedInput = ParseEquation(equation);
            Calc[] logicTree = CreateLogicTree(parsedInput);
            logicTree = ApplyLeftLogic(logicTree);
            logicTree = ApplyRightLogic(logicTree);
            string[] answers = SolveLogicTree(logicTree);
            int found = 0; string equationResult = "";
            foreach (var a in answers)
            {
                if (a.Contains("="))
                {
                    found = a.IndexOf("=");
                    equationResult = a.Substring(found + 1);
                    break;
                }
                             
            }
            double outcome = RoundUpto2DecimalPlacesWithoutExternalLibrary(equationResult);
            return outcome;
        }

        /// <summary>
        /// // 1 + 2.3 / 4 * 5 - 6
        /// Put all characters into array forexample [0] -> 1 , [1] -> +, [2] -> 2.3, [3]-> /, [4] -> 4 , [5] -> * , [6]-> 5, [7] -> - , [8] -> 6
        /// </summary>
        /// <param name="equation">User input equation</param>
        /// <returns>return list of characters</returns>
        static IEnumerable<String> ParseEquation(string equation)
        {
            IList<String> parsedInput = new List<String>();
            StringBuilder sb = new StringBuilder();
            foreach (Char c in equation)
            {
                if (c != '*' && c != '/' && c != '+' && c != '-')
                {
                    sb.Append(c);
                }
                else
                {
                    parsedInput.Add(sb.ToString());
                    parsedInput.Add(c.ToString());
                    sb.Clear();
                }
            }
            parsedInput.Add(sb.ToString());
            return parsedInput;
        }

        /// <summary>
        /// Assigns string[] to eqaul sized Calc[], string[] value is either Calc.oper or Calc.num.
        /// </summary>
        /// <param name="parsedEquation">String[] containing separated numbers and operators</param>
        /// <returns>Calc[] with either .num or .oper assigned to each index</returns>
        public static Calc[] CreateLogicTree(IEnumerable<String> parsedEquation)
        {
            IList<Calc> logicTree = new List<Calc>();

            foreach (String item in parsedEquation)
            {
                if (item == "*" || item == "/" || item == "+" || item == "-")
                {
                    logicTree.Add(new Calc() { oper = item[0] });
                }
                else
                {
                    logicTree.Add(new Calc() { num = Convert.ToDecimal(item, CultureInfo.InvariantCulture) });
                }
            }
            return logicTree.ToArray();
        }

        /// <summary>
        /// Applies the order of operations logic for the .left branches.
        /// </summary>
        /// <param name="logicTree">Each index contains .num or .oper</param>
        /// <returns>logicTree with .left assigned and precedence.</returns>
        static Calc[] ApplyLeftLogic(Calc[] logicTree)
        {
            // .left requirements & assign initial values before .right can be determined for + and -.
            for (int i = 0; i < logicTree.Length; i++)
            {
                if (logicTree[i].oper == '*' || logicTree[i].oper == '/')
                {
                    // Previous oper must be + or - // else .left = previous * or / oper
                    if (i - 2 >= 0) // bounds checking
                    {
                        if (logicTree[i - 2].oper == '+' || logicTree[i - 2].oper == '-')
                            logicTree[i].left = logicTree[i - 1];
                        else // previous operator must be * or /
                            logicTree[i].left = logicTree[i - 2];
                    }
                    else logicTree[i].left = logicTree[i - 1];
                    logicTree[i].right = logicTree[i + 1]; // always
                    logicTree[i].precedence = 1; // Calculate this 1st
                }
                else if (logicTree[i].oper == '+' || logicTree[i].oper == '-')
                {
                    // Previous oper must not exist or link to previous + or -
                    if (i - 2 >= 0) // bounds checking
                    {
                        for (int j = i - 2; j >= 0; j--)
                        {
                            if (logicTree[j].oper == '+' || logicTree[j].oper == '-')
                            {
                                logicTree[i].left = logicTree[j];
                                break;
                            }
                            j--;
                        }
                        if (logicTree[i].left == null) // logicTree[l - 2] must be the last * or / & the correct assignment
                            logicTree[i].left = logicTree[i - 2];
                    }
                    else logicTree[i].left = logicTree[i - 1];
                    // wait to assign .right
                    logicTree[i].precedence = 2; // Calculate this 2nd
                }
            }
            return logicTree;
        }
        /// <summary>
        /// Applies the order of operations logic for the .right branches.
        /// </summary>
        /// <param name="logicTree">Each index contains .num or .oper</param>
        /// <returns>logicTree with .right assigned.</returns>
        static Calc[] ApplyRightLogic(Calc[] logicTree)
        {
            // .right requirements for + and -
            for (int i = 1; i < logicTree.Length; i++)
            {
                if (logicTree[i].oper == '+' || logicTree[i].oper == '-')
                {
                    // if tree.oper + 2 == * or /, check next tree.oper and assign .right to the last consecutive * or /
                    if (i + 2 < logicTree.Length) // bounds checking
                    {
                        if (logicTree[i + 2].oper == '*' || logicTree[i + 2].oper == '/')
                        {
                            int j; // represents last * or /
                            for (j = i + 2; j < logicTree.Length; j++) // assign .right to last consecutive * or /
                            {
                                if (logicTree[j].oper != '*' && logicTree[j].oper != '/')
                                {
                                    logicTree[i].right = logicTree[j - 2];
                                    break;
                                }
                                j++;
                            }
                            if (logicTree[i].right == null) // if not assigned from the for loop, last * or / must be (o - 2)
                                logicTree[i].right = logicTree[j - 2];
                        }
                        else logicTree[i].right = logicTree[i + 1];
                    }
                    else logicTree[i].right = logicTree[i + 1];
                }
                i++;
            }
            return logicTree;
        }

        /// <summary>
        /// Returns decimal value or if performed on an operator returns the solution of
        /// left (operator) right.
        /// </summary>
        /// <returns>num value or solution</returns>
        decimal GetValue()
        {
            if (oper == 0) // tests for operator
                return num;
            else
                switch (oper)
                { // recursively calculates down the "tree", logical order determined in applyLogic()
                    case '+':
                        return left.GetValue() + right.GetValue();
                    case '-':
                        return left.GetValue() - right.GetValue();
                    case '*':
                        return left.GetValue() * right.GetValue();
                    case '/':
                        return left.GetValue() / right.GetValue();
                    default: return 0; // never returns 0
                }
        }
        /// <summary>
        /// Solves logicTree by the order of operations.
        /// </summary>
        /// <param name="logicTree">.left, .right, and precedence are assigned</param>
        /// <returns>string[] of answers in the order of operations</returns>
        static string[] SolveLogicTree(Calc[] logicTree)
        {
            // 1 extra index for final answer
            int answersIndex = 1;
            for (int i = 0; i < logicTree.Length; i++)
            {
                // check if it is not NUll character
                if (logicTree[i].oper != '\0')
                    answersIndex++;
            }
            string[] answers = new string[answersIndex];
            answersIndex = 0;

            int precedence = 1; // starts with * and /
            while (precedence != 3)
            {
                for (int j = 0; j < logicTree.Length; j++)
                {
                    if (logicTree[j].oper != 0 && logicTree[j].precedence == precedence)
                        answers[answersIndex] = "\n" + logicTree[j].left.GetValue() + " " + logicTree[j].oper + " " + logicTree[j].right.GetValue() + " = " + logicTree[j].GetValue();
                }
                precedence++;
            }
            answers[answers.Length - 1] = "" + logicTree[logicTree.Length - 2].GetValue();
            return answers;
        }

        private static double RoundUpto2DecimalPlacesWithoutExternalLibrary(string value)
        {
            double x = Convert.ToDouble(value);
            double mult = x * 100.0;
            double add = mult + 0.5;
            int result = (int)add;
            double outcome = result / 100.0;
            return outcome;
        }



    }
}
